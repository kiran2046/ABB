import os
import uuid
import pandas as pd
import numpy as np
import joblib
from datetime import datetime
from typing import Dict, Any, List, Optional
import asyncio
import logging
from concurrent.futures import ThreadPoolExecutor
import threading
import json

from app.models.prediction_models import (
    PredictionRequest, PredictionResponse, BatchPredictionRequest,
    BatchPredictionResponse, PredictionJob
)

logger = logging.getLogger(__name__)

class PredictionService:
    def __init__(self):
        self.models_cache: Dict[str, Any] = {}
        self.prediction_jobs: Dict[str, PredictionJob] = {}
        self.models_dir = "models"
        self.data_dir = "data"
        self.outputs_dir = "outputs"
        self.executor = ThreadPoolExecutor(max_workers=2)
        self.job_lock = threading.Lock()
        self.cache_lock = threading.Lock()
        
        # Create directories if they don't exist
        os.makedirs(self.models_dir, exist_ok=True)
        os.makedirs(self.data_dir, exist_ok=True)
        os.makedirs(self.outputs_dir, exist_ok=True)

    async def predict(self, request: PredictionRequest) -> PredictionResponse:
        """Make real-time predictions"""
        start_time = datetime.now()
        
        try:
            # Load model
            model = await self._load_model(request.model_id)
            
            # Prepare data
            df = pd.DataFrame(request.data)
            
            # Make predictions
            predictions = model.predict(df)
            predictions_list = [float(pred) for pred in predictions]
            
            # Calculate confidence scores if requested
            confidence_scores = None
            if request.include_confidence:
                confidence_scores = self._calculate_confidence_scores(model, df, predictions)
            
            # Generate explanations if requested
            explanations = None
            if request.include_explanation:
                explanations = self._generate_explanations(model, df, predictions)
            
            processing_duration = (datetime.now() - start_time).total_seconds()
            
            return PredictionResponse(
                predictions=predictions_list,
                confidence_scores=confidence_scores,
                explanations=explanations,
                model_id=request.model_id,
                prediction_time=datetime.now(),
                processing_duration=processing_duration
            )
            
        except Exception as e:
            logger.error(f"Prediction failed for model {request.model_id}: {str(e)}")
            raise

    async def start_batch_prediction(self, request: BatchPredictionRequest) -> BatchPredictionResponse:
        """Start batch prediction job"""
        job_id = str(uuid.uuid4())
        
        # Create initial job status
        job = PredictionJob(
            job_id=job_id,
            model_id=request.model_id,
            dataset_id=request.dataset_id,
            status="queued",
            progress=0.0,
            total_records=0,
            processed_records=0,
            started_at=datetime.now()
        )
        
        with self.job_lock:
            self.prediction_jobs[job_id] = job
        
        # Submit batch prediction task to thread pool
        self.executor.submit(self._run_batch_prediction, job_id, request)
        
        return BatchPredictionResponse(
            job_id=job_id,
            status="queued",
            message="Batch prediction job has been queued",
            created_at=datetime.now(),
            estimated_duration=60  # 1 minute estimate
        )

    def _run_batch_prediction(self, job_id: str, request: BatchPredictionRequest):
        """Execute batch prediction"""
        try:
            # Update status
            self._update_job_status(job_id, "running", 5.0)
            
            # Load dataset
            dataset_path = os.path.join(self.data_dir, f"{request.dataset_id}.csv")
            if not os.path.exists(dataset_path):
                raise FileNotFoundError(f"Dataset {request.dataset_id} not found")
            
            df = pd.read_csv(dataset_path)
            total_records = len(df)
            
            with self.job_lock:
                self.prediction_jobs[job_id].total_records = total_records
            
            self._update_job_status(job_id, "running", 10.0)
            
            # Load model
            model_path = os.path.join(self.models_dir, f"{request.model_id}.pkl")
            if not os.path.exists(model_path):
                raise FileNotFoundError(f"Model {request.model_id} not found")
            
            model = joblib.load(model_path)
            self._update_job_status(job_id, "running", 20.0)
            
            # Make predictions in batches
            batch_size = 1000
            predictions = []
            confidence_scores = [] if request.include_confidence else None
            
            for i in range(0, total_records, batch_size):
                batch_df = df.iloc[i:i+batch_size]
                batch_predictions = model.predict(batch_df)
                predictions.extend([float(pred) for pred in batch_predictions])
                
                if request.include_confidence:
                    batch_confidence = self._calculate_confidence_scores(model, batch_df, batch_predictions)
                    if batch_confidence:
                        confidence_scores.extend(batch_confidence)
                
                processed_records = min(i + batch_size, total_records)
                progress = 20.0 + (processed_records / total_records) * 60.0
                
                with self.job_lock:
                    self.prediction_jobs[job_id].processed_records = processed_records
                
                self._update_job_status(job_id, "running", progress)
            
            # Save results
            output_data = {
                "predictions": predictions,
                "model_id": request.model_id,
                "dataset_id": request.dataset_id,
                "created_at": datetime.now().isoformat()
            }
            
            if confidence_scores:
                output_data["confidence_scores"] = confidence_scores
            
            # Save to file
            if request.output_format == "json":
                output_filename = f"{job_id}_predictions.json"
                output_path = os.path.join(self.outputs_dir, output_filename)
                with open(output_path, 'w') as f:
                    json.dump(output_data, f, indent=2)
            else:
                # CSV format
                output_filename = f"{job_id}_predictions.csv"
                output_path = os.path.join(self.outputs_dir, output_filename)
                result_df = df.copy()
                result_df['prediction'] = predictions
                if confidence_scores:
                    result_df['confidence'] = confidence_scores
                result_df.to_csv(output_path, index=False)
            
            # Complete job
            with self.job_lock:
                job = self.prediction_jobs[job_id]
                job.status = "completed"
                job.progress = 100.0
                job.output_path = output_path
                job.completed_at = datetime.now()
            
        except Exception as e:
            logger.error(f"Batch prediction job {job_id} failed: {str(e)}")
            self._fail_job(job_id, str(e))

    async def _load_model(self, model_id: str):
        """Load model with caching"""
        with self.cache_lock:
            if model_id in self.models_cache:
                return self.models_cache[model_id]
        
        model_path = os.path.join(self.models_dir, f"{model_id}.pkl")
        if not os.path.exists(model_path):
            raise FileNotFoundError(f"Model {model_id} not found")
        
        model = joblib.load(model_path)
        
        with self.cache_lock:
            self.models_cache[model_id] = model
        
        return model

    def _calculate_confidence_scores(self, model, df: pd.DataFrame, predictions: np.ndarray) -> Optional[List[float]]:
        """Calculate confidence scores for predictions"""
        try:
            # For ensemble models, use prediction variance
            if hasattr(model, 'estimators_'):
                # Random Forest or Gradient Boosting
                all_predictions = np.array([tree.predict(df) for tree in model.estimators_])
                variance = np.var(all_predictions, axis=0)
                # Convert variance to confidence (lower variance = higher confidence)
                confidence = 1.0 / (1.0 + variance)
                return [float(conf) for conf in confidence]
            
            # For other models, use a simple heuristic based on prediction magnitude
            abs_predictions = np.abs(predictions)
            max_pred = np.max(abs_predictions) if len(abs_predictions) > 0 else 1.0
            confidence = abs_predictions / max_pred if max_pred > 0 else np.ones_like(predictions)
            return [float(conf) for conf in confidence]
            
        except Exception as e:
            logger.warning(f"Could not calculate confidence scores: {str(e)}")
            return None

    def _generate_explanations(self, model, df: pd.DataFrame, predictions: np.ndarray) -> Optional[List[Dict[str, Any]]]:
        """Generate explanations for predictions"""
        try:
            explanations = []
            
            # Simple feature importance based explanation
            if hasattr(model, 'feature_importances_'):
                feature_names = df.columns.tolist()
                feature_importance = model.feature_importances_
                
                for i, (_, row) in enumerate(df.iterrows()):
                    explanation = {
                        "prediction": float(predictions[i]),
                        "top_features": []
                    }
                    
                    # Get top 3 most important features for this prediction
                    feature_contributions = []
                    for j, (feature, importance, value) in enumerate(zip(feature_names, feature_importance, row.values)):
                        contribution = importance * abs(float(value))
                        feature_contributions.append({
                            "feature": feature,
                            "value": float(value),
                            "importance": float(importance),
                            "contribution": contribution
                        })
                    
                    # Sort by contribution and take top 3
                    feature_contributions.sort(key=lambda x: x["contribution"], reverse=True)
                    explanation["top_features"] = feature_contributions[:3]
                    explanations.append(explanation)
            
            return explanations if explanations else None
            
        except Exception as e:
            logger.warning(f"Could not generate explanations: {str(e)}")
            return None

    def _update_job_status(self, job_id: str, status: str, progress: float, error_message: Optional[str] = None):
        """Update prediction job status"""
        with self.job_lock:
            if job_id in self.prediction_jobs:
                job = self.prediction_jobs[job_id]
                job.status = status
                job.progress = progress
                if error_message:
                    job.error_message = error_message

    def _fail_job(self, job_id: str, error_message: str):
        """Mark prediction job as failed"""
        with self.job_lock:
            if job_id in self.prediction_jobs:
                job = self.prediction_jobs[job_id]
                job.status = "failed"
                job.error_message = error_message
                job.completed_at = datetime.now()

    async def get_prediction_job_status(self, job_id: str) -> Optional[PredictionJob]:
        """Get prediction job status"""
        with self.job_lock:
            return self.prediction_jobs.get(job_id)

    async def list_prediction_jobs(self) -> List[PredictionJob]:
        """List all prediction jobs"""
        with self.job_lock:
            return list(self.prediction_jobs.values())

    async def cancel_prediction_job(self, job_id: str) -> bool:
        """Cancel a prediction job"""
        with self.job_lock:
            if job_id in self.prediction_jobs:
                job = self.prediction_jobs[job_id]
                if job.status in ["queued", "running"]:
                    job.status = "cancelled"
                    job.completed_at = datetime.now()
                    return True
        return False

    async def clear_model_cache(self):
        """Clear the model cache"""
        with self.cache_lock:
            self.models_cache.clear()

# Global instance
prediction_service = PredictionService()
