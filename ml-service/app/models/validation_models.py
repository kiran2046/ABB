from pydantic import BaseModel
from typing import List, Dict, Any, Optional
from datetime import datetime

class ValidationRequest(BaseModel):
    model_id: str
    dataset_id: str
    validation_type: str = "cross_validation"
    cv_folds: int = 5
    metrics: List[str] = ["accuracy", "precision", "recall", "f1_score"]

class ValidationResponse(BaseModel):
    job_id: str
    status: str
    message: str
    created_at: datetime
    estimated_duration: Optional[int] = None

class ValidationResult(BaseModel):
    job_id: str
    model_id: str
    dataset_id: str
    validation_type: str
    metrics: Dict[str, float]
    cross_validation_scores: Optional[Dict[str, List[float]]] = None
    confusion_matrix: Optional[List[List[int]]] = None
    classification_report: Optional[Dict[str, Any]] = None
    validation_duration: float
    created_at: datetime

class ModelComparison(BaseModel):
    models: List[str]
    dataset_id: str
    metrics: Dict[str, Dict[str, float]]
    best_model: str
    comparison_criteria: str
    created_at: datetime

class ValidationJob(BaseModel):
    job_id: str
    model_id: str
    dataset_id: str
    status: str
    progress: float
    error_message: Optional[str] = None
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
    result: Optional[ValidationResult] = None
