import os
import uuid
import pandas as pd
import numpy as np
import joblib
from datetime import datetime
from typing import Dict, Any, List, Optional
from sklearn.model_selection import cross_val_score, StratifiedKFold, KFold
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report, mean_squared_error,
    mean_absolute_error, r2_score
)
import asyncio
import logging
from concurrent.futures import ThreadPoolExecutor
import threading
import json

from app.models.validation_models import (
    ValidationRequest, ValidationResponse, ValidationResult,
    ModelComparison, ValidationJob
)

logger = logging.getLogger(__name__)

class ValidationService:
    def __init__(self):
        self.validation_jobs: Dict[str, ValidationJob] = {}
        self.models_dir = "models"
        self.data_dir = "data"
        self.executor = ThreadPoolExecutor(max_workers=2)
        self.job_lock = threading.Lock()
        
        # Create directories if they don't exist
        os.makedirs(self.models_dir, exist_ok=True)
        os.makedirs(self.data_dir, exist_ok=True)

    async def start_validation(self, request: ValidationRequest) -> ValidationResponse:
        """Start a new validation job"""
        job_id = str(uuid.uuid4())
        
        # Create initial job status
        job = ValidationJob(
            job_id=job_id,
            model_id=request.model_id,
            dataset_id=request.dataset_id,
            status="queued",
            progress=0.0,
            started_at=datetime.now()
        )
        
        with self.job_lock:
            self.validation_jobs[job_id] = job
        
        # Submit validation task to thread pool
        self.executor.submit(self._run_validation, job_id, request)
        
        return ValidationResponse(
            job_id=job_id,
            status="queued",
            message="Validation job has been queued",
            created_at=datetime.now(),
            estimated_duration=120  # 2 minutes estimate
        )

    def _run_validation(self, job_id: str, request: ValidationRequest):
        """Execute model validation"""
        try:
            # Update status
            self._update_job_status(job_id, "running", 10.0)
            
            # Load dataset
            dataset_path = os.path.join(self.data_dir, f"{request.dataset_id}.csv")
            if not os.path.exists(dataset_path):
                raise FileNotFoundError(f"Dataset {request.dataset_id} not found")
            
            df = pd.read_csv(dataset_path)
            self._update_job_status(job_id, "running", 20.0)
            
            # Load model
            model_path = os.path.join(self.models_dir, f"{request.model_id}.pkl")
            if not os.path.exists(model_path):
                raise FileNotFoundError(f"Model {request.model_id} not found")
            
            model = joblib.load(model_path)
            self._update_job_status(job_id, "running", 30.0)
            
            # Determine target column (assume last column if not specified)
            target_column = df.columns[-1]
            feature_columns = df.columns[:-1].tolist()
            
            X = df[feature_columns]
            y = df[target_column]
            
            # Determine if this is a regression or classification problem
            is_regression = self._is_regression_problem(y)
            
            # Perform validation based on type
            if request.validation_type == "cross_validation":
                validation_result = self._perform_cross_validation(
                    model, X, y, request.cv_folds, request.metrics, is_regression
                )
            else:
                # Hold-out validation
                validation_result = self._perform_holdout_validation(
                    model, X, y, request.metrics, is_regression
                )
            
            self._update_job_status(job_id, "running", 80.0)
            
            # Create validation result
            result = ValidationResult(
                job_id=job_id,
                model_id=request.model_id,
                dataset_id=request.dataset_id,
                validation_type=request.validation_type,
                metrics=validation_result["metrics"],
                cross_validation_scores=validation_result.get("cv_scores"),
                confusion_matrix=validation_result.get("confusion_matrix"),
                classification_report=validation_result.get("classification_report"),
                validation_duration=validation_result["duration"],
                created_at=datetime.now()
            )
            
            # Complete job
            with self.job_lock:
                job = self.validation_jobs[job_id]
                job.status = "completed"
                job.progress = 100.0
                job.completed_at = datetime.now()
                job.result = result
            
        except Exception as e:
            logger.error(f"Validation job {job_id} failed: {str(e)}")
            self._fail_job(job_id, str(e))

    def _is_regression_problem(self, y: pd.Series) -> bool:
        """Determine if this is a regression or classification problem"""
        # Simple heuristic: if target has more than 10 unique values and is numeric, assume regression
        unique_values = y.nunique()
        is_numeric = pd.api.types.is_numeric_dtype(y)
        return is_numeric and unique_values > 10

    def _perform_cross_validation(self, model, X: pd.DataFrame, y: pd.Series, cv_folds: int, 
                                 metrics: List[str], is_regression: bool) -> Dict[str, Any]:
        """Perform cross-validation"""
        start_time = datetime.now()
        
        # Choose appropriate cross-validation strategy
        if is_regression:
            cv = KFold(n_splits=cv_folds, shuffle=True, random_state=42)
        else:
            cv = StratifiedKFold(n_splits=cv_folds, shuffle=True, random_state=42)
        
        results = {
            "metrics": {},
            "cv_scores": {},
            "duration": 0.0
        }
        
        # Perform cross-validation for each metric
        for metric in metrics:
            if is_regression:
                scores = self._cross_validate_regression(model, X, y, cv, metric)
            else:
                scores = self._cross_validate_classification(model, X, y, cv, metric)
            
            if scores is not None:
                results["metrics"][metric] = float(np.mean(scores))
                results["cv_scores"][metric] = [float(score) for score in scores]
        
        # Add additional evaluation on full dataset
        model.fit(X, y)
        y_pred = model.predict(X)
        
        if is_regression:
            additional_metrics = self._calculate_regression_metrics(y, y_pred)
        else:
            additional_metrics = self._calculate_classification_metrics(y, y_pred)
            results["confusion_matrix"] = confusion_matrix(y, y_pred).tolist()
            results["classification_report"] = classification_report(y, y_pred, output_dict=True)
        
        results["metrics"].update(additional_metrics)
        results["duration"] = (datetime.now() - start_time).total_seconds()
        
        return results

    def _perform_holdout_validation(self, model, X: pd.DataFrame, y: pd.Series, 
                                   metrics: List[str], is_regression: bool) -> Dict[str, Any]:
        """Perform hold-out validation"""
        start_time = datetime.now()
        
        from sklearn.model_selection import train_test_split
        
        # Split data
        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=0.2, random_state=42, 
            stratify=None if is_regression else y
        )
        
        # Train and predict
        model.fit(X_train, y_train)
        y_pred = model.predict(X_test)
        
        results = {
            "metrics": {},
            "duration": (datetime.now() - start_time).total_seconds()
        }
        
        if is_regression:
            results["metrics"] = self._calculate_regression_metrics(y_test, y_pred)
        else:
            results["metrics"] = self._calculate_classification_metrics(y_test, y_pred)
            results["confusion_matrix"] = confusion_matrix(y_test, y_pred).tolist()
            results["classification_report"] = classification_report(y_test, y_pred, output_dict=True)
        
        return results

    def _cross_validate_regression(self, model, X: pd.DataFrame, y: pd.Series, cv, metric: str):
        """Cross-validate for regression metrics"""
        try:
            if metric == "r2_score":
                return cross_val_score(model, X, y, cv=cv, scoring="r2")
            elif metric == "mse":
                return -cross_val_score(model, X, y, cv=cv, scoring="neg_mean_squared_error")
            elif metric == "mae":
                return -cross_val_score(model, X, y, cv=cv, scoring="neg_mean_absolute_error")
            elif metric == "rmse":
                mse_scores = -cross_val_score(model, X, y, cv=cv, scoring="neg_mean_squared_error")
                return np.sqrt(mse_scores)
        except Exception as e:
            logger.warning(f"Could not calculate {metric}: {str(e)}")
        return None

    def _cross_validate_classification(self, model, X: pd.DataFrame, y: pd.Series, cv, metric: str):
        """Cross-validate for classification metrics"""
        try:
            if metric == "accuracy":
                return cross_val_score(model, X, y, cv=cv, scoring="accuracy")
            elif metric == "precision":
                return cross_val_score(model, X, y, cv=cv, scoring="precision_weighted")
            elif metric == "recall":
                return cross_val_score(model, X, y, cv=cv, scoring="recall_weighted")
            elif metric == "f1_score":
                return cross_val_score(model, X, y, cv=cv, scoring="f1_weighted")
        except Exception as e:
            logger.warning(f"Could not calculate {metric}: {str(e)}")
        return None

    def _calculate_regression_metrics(self, y_true, y_pred) -> Dict[str, float]:
        """Calculate regression metrics"""
        return {
            "mse": float(mean_squared_error(y_true, y_pred)),
            "rmse": float(np.sqrt(mean_squared_error(y_true, y_pred))),
            "mae": float(mean_absolute_error(y_true, y_pred)),
            "r2_score": float(r2_score(y_true, y_pred))
        }

    def _calculate_classification_metrics(self, y_true, y_pred) -> Dict[str, float]:
        """Calculate classification metrics"""
        return {
            "accuracy": float(accuracy_score(y_true, y_pred)),
            "precision": float(precision_score(y_true, y_pred, average="weighted", zero_division=0)),
            "recall": float(recall_score(y_true, y_pred, average="weighted", zero_division=0)),
            "f1_score": float(f1_score(y_true, y_pred, average="weighted", zero_division=0))
        }

    async def compare_models(self, model_ids: List[str], dataset_id: str, 
                           comparison_criteria: str = "accuracy") -> ModelComparison:
        """Compare multiple models on the same dataset"""
        # Load dataset
        dataset_path = os.path.join(self.data_dir, f"{dataset_id}.csv")
        if not os.path.exists(dataset_path):
            raise FileNotFoundError(f"Dataset {dataset_id} not found")
        
        df = pd.read_csv(dataset_path)
        target_column = df.columns[-1]
        feature_columns = df.columns[:-1].tolist()
        
        X = df[feature_columns]
        y = df[target_column]
        
        # Split data for fair comparison
        from sklearn.model_selection import train_test_split
        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=0.2, random_state=42
        )
        
        results = {}
        is_regression = self._is_regression_problem(y)
        
        for model_id in model_ids:
            try:
                # Load and evaluate model
                model_path = os.path.join(self.models_dir, f"{model_id}.pkl")
                if not os.path.exists(model_path):
                    logger.warning(f"Model {model_id} not found, skipping")
                    continue
                
                model = joblib.load(model_path)
                model.fit(X_train, y_train)
                y_pred = model.predict(X_test)
                
                if is_regression:
                    metrics = self._calculate_regression_metrics(y_test, y_pred)
                else:
                    metrics = self._calculate_classification_metrics(y_test, y_pred)
                
                results[model_id] = metrics
                
            except Exception as e:
                logger.error(f"Error evaluating model {model_id}: {str(e)}")
                continue
        
        # Determine best model
        best_model = None
        best_score = float('-inf') if comparison_criteria in ['accuracy', 'precision', 'recall', 'f1_score', 'r2_score'] else float('inf')
        
        for model_id, metrics in results.items():
            if comparison_criteria in metrics:
                score = metrics[comparison_criteria]
                if comparison_criteria in ['accuracy', 'precision', 'recall', 'f1_score', 'r2_score']:
                    if score > best_score:
                        best_score = score
                        best_model = model_id
                else:  # Lower is better for mse, mae, rmse
                    if score < best_score:
                        best_score = score
                        best_model = model_id
        
        return ModelComparison(
            models=model_ids,
            dataset_id=dataset_id,
            metrics=results,
            best_model=best_model or "none",
            comparison_criteria=comparison_criteria,
            created_at=datetime.now()
        )

    def _update_job_status(self, job_id: str, status: str, progress: float, error_message: Optional[str] = None):
        """Update validation job status"""
        with self.job_lock:
            if job_id in self.validation_jobs:
                job = self.validation_jobs[job_id]
                job.status = status
                job.progress = progress
                if error_message:
                    job.error_message = error_message

    def _fail_job(self, job_id: str, error_message: str):
        """Mark validation job as failed"""
        with self.job_lock:
            if job_id in self.validation_jobs:
                job = self.validation_jobs[job_id]
                job.status = "failed"
                job.error_message = error_message
                job.completed_at = datetime.now()

    async def get_validation_job_status(self, job_id: str) -> Optional[ValidationJob]:
        """Get validation job status"""
        with self.job_lock:
            return self.validation_jobs.get(job_id)

    async def get_validation_result(self, job_id: str) -> Optional[ValidationResult]:
        """Get validation result"""
        job = await self.get_validation_job_status(job_id)
        return job.result if job and job.result else None

    async def list_validation_jobs(self) -> List[ValidationJob]:
        """List all validation jobs"""
        with self.job_lock:
            return list(self.validation_jobs.values())

    async def cancel_validation_job(self, job_id: str) -> bool:
        """Cancel a validation job"""
        with self.job_lock:
            if job_id in self.validation_jobs:
                job = self.validation_jobs[job_id]
                if job.status in ["queued", "running"]:
                    job.status = "cancelled"
                    job.completed_at = datetime.now()
                    return True
        return False

# Global instance
validation_service = ValidationService()
