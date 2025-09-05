from pydantic import BaseModel
from typing import List, Dict, Any, Optional
from datetime import datetime

class PredictionRequest(BaseModel):
    model_id: str
    data: List[Dict[str, Any]]
    include_confidence: bool = False
    include_explanation: bool = False

class PredictionResponse(BaseModel):
    predictions: List[float]
    confidence_scores: Optional[List[float]] = None
    explanations: Optional[List[Dict[str, Any]]] = None
    model_id: str
    prediction_time: datetime
    processing_duration: float

class BatchPredictionRequest(BaseModel):
    model_id: str
    dataset_id: str
    output_format: str = "json"
    include_confidence: bool = False

class BatchPredictionResponse(BaseModel):
    job_id: str
    status: str
    message: str
    created_at: datetime
    estimated_duration: Optional[int] = None

class PredictionJob(BaseModel):
    job_id: str
    model_id: str
    dataset_id: str
    status: str
    progress: float
    total_records: int
    processed_records: int
    output_path: Optional[str] = None
    error_message: Optional[str] = None
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
