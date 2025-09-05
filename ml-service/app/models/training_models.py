from pydantic import BaseModel
from typing import List, Optional, Dict, Any
from datetime import datetime
from enum import Enum

class TrainingAlgorithm(str, Enum):
    XGBOOST = "xgboost"
    LIGHTGBM = "lightgbm"
    RANDOM_FOREST = "random_forest"
    GRADIENT_BOOSTING = "gradient_boosting"
    LINEAR_REGRESSION = "linear_regression"

class TrainingRequest(BaseModel):
    dataset_id: str
    algorithm: TrainingAlgorithm
    target_column: str
    feature_columns: List[str]
    test_size: float = 0.2
    random_state: int = 42
    hyperparameters: Optional[Dict[str, Any]] = None

class TrainingResponse(BaseModel):
    job_id: str
    status: str
    message: str
    created_at: datetime
    estimated_duration: Optional[int] = None

class TrainingStatus(BaseModel):
    job_id: str
    status: str
    progress: float
    metrics: Optional[Dict[str, float]] = None
    error_message: Optional[str] = None
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None
    model_path: Optional[str] = None

class TrainingResult(BaseModel):
    job_id: str
    model_id: str
    algorithm: str
    metrics: Dict[str, float]
    feature_importance: Optional[Dict[str, float]] = None
    training_duration: float
    model_size: int
    created_at: datetime

class TrainingStatusResponse(BaseModel):
    status: str
    data: Optional[TrainingStatus] = None
    message: Optional[str] = None
