from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Dict, List, Optional, Any
import pandas as pd
import numpy as np
from datetime import datetime
import asyncio
import os
import logging

from .models.training_models import TrainingRequest, TrainingResponse, TrainingStatus, TrainingStatusResponse
from .models.prediction_models import PredictionRequest, PredictionResponse
from .models.validation_models import ValidationRequest, ValidationResponse
from .services.training_service import TrainingService
from .services.prediction_service import PredictionService
from .services.validation_service import ValidationService

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Create FastAPI app
app = FastAPI(
    title="Intellinspect ML Service",
    description="Machine Learning service for quality prediction using production line sensor data",
    version="1.0.0"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize services
training_service = TrainingService()
prediction_service = PredictionService()
validation_service = ValidationService()

# Create necessary directories
os.makedirs("models", exist_ok=True)
os.makedirs("data", exist_ok=True)
os.makedirs("logs", exist_ok=True)

@app.get("/")
async def root():
    return {
        "message": "Intellinspect ML Service",
        "version": "1.0.0",
        "status": "running"
    }

@app.get("/health")
async def health_check():
    return {"status": "healthy", "timestamp": datetime.now()}

@app.post("/train", response_model=TrainingResponse)
async def start_training(request: TrainingRequest, background_tasks: BackgroundTasks):
    """Start model training"""
    try:
        logger.info(f"Starting training with algorithm: {request.algorithm}")
        
        # Validate request
        if not os.path.exists(request.dataset_path):
            raise HTTPException(status_code=400, detail="Dataset file not found")
        
        # Start training in background
        job_id = await training_service.start_training(request, background_tasks)
        
        return TrainingResponse(
            job_id=job_id,
            status="queued",
            message="Training job started successfully"
        )
    
    except Exception as e:
        logger.error(f"Error starting training: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/training/status/{job_id}", response_model=TrainingStatusResponse)
async def get_training_status(job_id: str):
    """Get training job status"""
    try:
        status = training_service.get_training_status(job_id)
        
        if status is None:
            raise HTTPException(status_code=404, detail="Training job not found")
        
        return status
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error getting training status: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    """Make prediction using trained model"""
    try:
        logger.info(f"Making prediction with model: {request.model_id}")
        
        result = await prediction_service.predict(request)
        return result
    
    except Exception as e:
        logger.error(f"Error making prediction: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/validate", response_model=ValidationResponse)
async def validate_dataset(request: ValidationRequest):
    """Validate dataset format and contents"""
    try:
        logger.info(f"Validating dataset: {request.dataset_path}")
        
        result = await validation_service.validate_dataset(request)
        return result
    
    except Exception as e:
        logger.error(f"Error validating dataset: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/models")
async def list_models():
    """List available trained models"""
    try:
        models = prediction_service.list_models()
        return {"models": models}
    
    except Exception as e:
        logger.error(f"Error listing models: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.delete("/models/{model_id}")
async def delete_model(model_id: str):
    """Delete a trained model"""
    try:
        success = prediction_service.delete_model(model_id)
        
        if not success:
            raise HTTPException(status_code=404, detail="Model not found")
        
        return {"message": "Model deleted successfully"}
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error deleting model: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
