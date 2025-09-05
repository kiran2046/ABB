import os
import uuid
import pandas as pd
import numpy as np
import joblib
from datetime import datetime
from typing import Dict, Any, List, Optional, Tuple
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.ensemble import RandomForestRegressor, GradientBoostingRegressor
from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_squared_error, mean_absolute_error, r2_score
import xgboost as xgb
import lightgbm as lgb
import asyncio
import logging
from concurrent.futures import ThreadPoolExecutor
import threading

from app.models.training_models import (
    TrainingRequest, TrainingResponse, TrainingStatus, 
    TrainingResult, TrainingAlgorithm
)

logger = logging.getLogger(__name__)

class TrainingService:
    def __init__(self):
        self.training_jobs: Dict[str, TrainingStatus] = {}
        self.models_dir = "models"
        self.data_dir = "data"
        self.executor = ThreadPoolExecutor(max_workers=2)
        self.job_lock = threading.Lock()
        
        # Create directories if they don't exist
        os.makedirs(self.models_dir, exist_ok=True)
        os.makedirs(self.data_dir, exist_ok=True)

    async def start_training(self, request: TrainingRequest) -> TrainingResponse:
        """Start a new training job"""
        job_id = str(uuid.uuid4())
        
        # Create initial job status
        job_status = TrainingStatus(
            job_id=job_id,
            status="queued",
            progress=0.0,
            started_at=datetime.now()
        )
        
        with self.job_lock:
            self.training_jobs[job_id] = job_status
        
        # Submit training task to thread pool
        self.executor.submit(self._train_model, job_id, request)
        
        return TrainingResponse(
            job_id=job_id,
            status="queued",
            message="Training job has been queued",
            created_at=datetime.now(),
            estimated_duration=300  # 5 minutes estimate
        )

    def _train_model(self, job_id: str, request: TrainingRequest):
        """Execute the actual model training"""
        try:
            # Update status to training
            self._update_job_status(job_id, "training", 10.0)
            
            # Load dataset
            dataset_path = os.path.join(self.data_dir, f"{request.dataset_id}.csv")
            if not os.path.exists(dataset_path):
                raise FileNotFoundError(f"Dataset {request.dataset_id} not found")
            
            df = pd.read_csv(dataset_path)
            self._update_job_status(job_id, "training", 20.0)
            
            # Prepare features and target
            X = df[request.feature_columns]
            y = df[request.target_column]
            
            # Split data
            X_train, X_test, y_train, y_test = train_test_split(
                X, y, test_size=request.test_size, random_state=request.random_state
            )
            self._update_job_status(job_id, "training", 30.0)
            
            # Train model based on algorithm
            model = self._create_model(request.algorithm, request.hyperparameters)
            model.fit(X_train, y_train)
            self._update_job_status(job_id, "training", 70.0)
            
            # Evaluate model
            y_pred = model.predict(X_test)
            metrics = self._calculate_metrics(y_test, y_pred)
            self._update_job_status(job_id, "training", 85.0)
            
            # Save model
            model_id = str(uuid.uuid4())
            model_path = os.path.join(self.models_dir, f"{model_id}.pkl")
            joblib.dump(model, model_path)
            
            # Get feature importance if available
            feature_importance = self._get_feature_importance(model, request.feature_columns)
            
            # Complete job
            self._complete_job(job_id, model_id, model_path, metrics, feature_importance)
            
        except Exception as e:
            logger.error(f"Training job {job_id} failed: {str(e)}")
            self._fail_job(job_id, str(e))

    def _create_model(self, algorithm: TrainingAlgorithm, hyperparameters: Optional[Dict[str, Any]]):
        """Create model instance based on algorithm"""
        params = hyperparameters or {}
        
        if algorithm == TrainingAlgorithm.XGBOOST:
            return xgb.XGBRegressor(**params)
        elif algorithm == TrainingAlgorithm.LIGHTGBM:
            return lgb.LGBMRegressor(**params)
        elif algorithm == TrainingAlgorithm.RANDOM_FOREST:
            default_params = {"n_estimators": 100, "random_state": 42}
            default_params.update(params)
            return RandomForestRegressor(**default_params)
        elif algorithm == TrainingAlgorithm.GRADIENT_BOOSTING:
            default_params = {"n_estimators": 100, "random_state": 42}
            default_params.update(params)
            return GradientBoostingRegressor(**default_params)
        elif algorithm == TrainingAlgorithm.LINEAR_REGRESSION:
            return LinearRegression(**params)
        else:
            raise ValueError(f"Unsupported algorithm: {algorithm}")

    def _calculate_metrics(self, y_true: np.ndarray, y_pred: np.ndarray) -> Dict[str, float]:
        """Calculate evaluation metrics"""
        return {
            "mse": float(mean_squared_error(y_true, y_pred)),
            "rmse": float(np.sqrt(mean_squared_error(y_true, y_pred))),
            "mae": float(mean_absolute_error(y_true, y_pred)),
            "r2_score": float(r2_score(y_true, y_pred))
        }

    def _get_feature_importance(self, model, feature_columns: List[str]) -> Optional[Dict[str, float]]:
        """Get feature importance if model supports it"""
        try:
            if hasattr(model, 'feature_importances_'):
                importances = model.feature_importances_
                return {col: float(imp) for col, imp in zip(feature_columns, importances)}
            elif hasattr(model, 'coef_'):
                coefficients = model.coef_
                return {col: float(coef) for col, coef in zip(feature_columns, coefficients)}
        except Exception as e:
            logger.warning(f"Could not extract feature importance: {str(e)}")
        return None

    def _update_job_status(self, job_id: str, status: str, progress: float, error_message: Optional[str] = None):
        """Update job status"""
        with self.job_lock:
            if job_id in self.training_jobs:
                job = self.training_jobs[job_id]
                job.status = status
                job.progress = progress
                if error_message:
                    job.error_message = error_message

    def _complete_job(self, job_id: str, model_id: str, model_path: str, metrics: Dict[str, float], feature_importance: Optional[Dict[str, float]]):
        """Mark job as completed"""
        with self.job_lock:
            if job_id in self.training_jobs:
                job = self.training_jobs[job_id]
                job.status = "completed"
                job.progress = 100.0
                job.completed_at = datetime.now()
                job.model_path = model_path
                job.metrics = metrics

    def _fail_job(self, job_id: str, error_message: str):
        """Mark job as failed"""
        with self.job_lock:
            if job_id in self.training_jobs:
                job = self.training_jobs[job_id]
                job.status = "failed"
                job.error_message = error_message
                job.completed_at = datetime.now()

    async def get_training_status(self, job_id: str) -> Optional[TrainingStatus]:
        """Get training job status"""
        with self.job_lock:
            return self.training_jobs.get(job_id)

    async def get_training_result(self, job_id: str) -> Optional[TrainingResult]:
        """Get training job result"""
        job_status = await self.get_training_status(job_id)
        
        if not job_status or job_status.status != "completed":
            return None
        
        return TrainingResult(
            job_id=job_id,
            model_id=os.path.basename(job_status.model_path).replace('.pkl', '') if job_status.model_path else "",
            algorithm="unknown",  # Would need to store this in job status
            metrics=job_status.metrics or {},
            feature_importance=None,  # Would need to store this in job status
            training_duration=0.0,  # Would need to calculate this
            model_size=0,  # Would need to calculate this
            created_at=job_status.completed_at or datetime.now()
        )

    async def list_training_jobs(self) -> List[TrainingStatus]:
        """List all training jobs"""
        with self.job_lock:
            return list(self.training_jobs.values())

    async def cancel_training_job(self, job_id: str) -> bool:
        """Cancel a training job"""
        with self.job_lock:
            if job_id in self.training_jobs:
                job = self.training_jobs[job_id]
                if job.status in ["queued", "training"]:
                    job.status = "cancelled"
                    job.completed_at = datetime.now()
                    return True
        return False

# Global instance
training_service = TrainingService()
