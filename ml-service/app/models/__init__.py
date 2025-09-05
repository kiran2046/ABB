from pydantic import BaseModel
from typing import List, Dict, Any, Optional
from datetime import datetime
from enum import Enum

class ModelStatus(str, Enum):
    TRAINING = "training"
    READY = "ready"
    ERROR = "error"
    DEPRECATED = "deprecated"

class ModelInfo(BaseModel):
    model_id: str
    name: str
    algorithm: str
    version: str
    status: ModelStatus
    accuracy: Optional[float] = None
    created_at: datetime
    updated_at: datetime
    size_mb: float
    feature_columns: List[str]
    target_column: str
    training_dataset_id: str
    hyperparameters: Dict[str, Any]
    metrics: Optional[Dict[str, float]] = None

class ModelListResponse(BaseModel):
    models: List[ModelInfo]
    total_count: int
    page: int
    page_size: int

class ModelCreateRequest(BaseModel):
    name: str
    algorithm: str
    dataset_id: str
    target_column: str
    feature_columns: List[str]
    hyperparameters: Optional[Dict[str, Any]] = None

class ModelUpdateRequest(BaseModel):
    name: Optional[str] = None
    status: Optional[ModelStatus] = None
    hyperparameters: Optional[Dict[str, Any]] = None

class ModelVersionInfo(BaseModel):
    version: str
    created_at: datetime
    metrics: Dict[str, float]
    is_active: bool
    notes: Optional[str] = None

class ModelMetrics(BaseModel):
    model_id: str
    accuracy: float
    precision: float
    recall: float
    f1_score: float
    mse: Optional[float] = None
    rmse: Optional[float] = None
    mae: Optional[float] = None
    r2_score: Optional[float] = None
    custom_metrics: Optional[Dict[str, float]] = None
