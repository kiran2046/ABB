import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TrainingService } from '../../services/training.service';
import { DatasetService } from '../../services/dataset.service';
import { TrainingConfig, TrainingStatus, TrainingMetrics } from '../../models/training.model';

@Component({
  selector: 'app-model-training',
  templateUrl: './model-training.component.html',
  styleUrls: ['./model-training.component.scss']
})
export class ModelTrainingComponent implements OnInit {
  trainingForm: FormGroup;
  trainingStatus: TrainingStatus | null = null;
  isTraining = false;
  algorithms: string[] = [];

  constructor(
    private fb: FormBuilder,
    private trainingService: TrainingService,
    private datasetService: DatasetService,
    private snackBar: MatSnackBar
  ) {
    this.trainingForm = this.createForm();
  }

  ngOnInit(): void {
    this.algorithms = this.trainingService.getAvailableAlgorithms();
    this.initializeForm();

    this.trainingService.currentTrainingStatus$.subscribe(status => {
      this.trainingStatus = status;
      this.isTraining = status?.status === 'training' || status?.status === 'queued';
    });
  }

  private createForm(): FormGroup {
    return this.fb.group({
      algorithm: ['xgboost', Validators.required],
      validationSplit: [0.2, [Validators.required, Validators.min(0.1), Validators.max(0.5)]],
      hyperparameters: this.fb.group({})
    });
  }

  private initializeForm(): void {
    this.onAlgorithmChange('xgboost');
  }

  onAlgorithmChange(algorithm: string): void {
    const hyperparams = this.trainingService.getDefaultHyperparameters(algorithm);
    const hyperparamsGroup = this.fb.group({});

    Object.keys(hyperparams).forEach(key => {
      hyperparamsGroup.addControl(key, this.fb.control(hyperparams[key], Validators.required));
    });

    this.trainingForm.setControl('hyperparameters', hyperparamsGroup);
  }

  startTraining(): void {
    if (!this.trainingForm.valid) {
      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
      return;
    }

    const dataset = this.datasetService.getCurrentDataset();
    if (!dataset) {
      this.snackBar.open('No dataset available for training', 'Close', { duration: 3000 });
      return;
    }

    const config: TrainingConfig = {
      datasetId: dataset.id,
      algorithm: this.trainingForm.value.algorithm,
      validationSplit: this.trainingForm.value.validationSplit,
      hyperparameters: this.trainingForm.value.hyperparameters
    };

    this.isTraining = true;
    this.trainingService.startTraining(config).subscribe({
      next: (status) => {
        if (status.status === 'completed') {
          this.snackBar.open('Model training completed successfully!', 'Close', { duration: 3000 });
          this.isTraining = false;
        } else if (status.status === 'failed') {
          this.snackBar.open('Model training failed: ' + status.error, 'Close', { duration: 5000 });
          this.isTraining = false;
        }
      },
      error: (error) => {
        this.snackBar.open('Training failed: ' + error.message, 'Close', { duration: 5000 });
        this.isTraining = false;
      }
    });
  }

  getAlgorithmDisplayName(algorithm: string): string {
    switch (algorithm) {
      case 'xgboost': return 'XGBoost';
      case 'lightgbm': return 'LightGBM';
      case 'scikit-learn': return 'Random Forest (scikit-learn)';
      default: return algorithm;
    }
  }

  getStatusDisplayName(status: string): string {
    switch (status) {
      case 'queued': return 'Queued';
      case 'training': return 'Training...';
      case 'completed': return 'Completed';
      case 'failed': return 'Failed';
      default: return status;
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'queued': return 'schedule';
      case 'training': return 'hourglass_empty';
      case 'completed': return 'check_circle';
      case 'failed': return 'error';
      default: return 'help';
    }
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'queued': return '#ff9800';
      case 'training': return '#2196f3';
      case 'completed': return '#4caf50';
      case 'failed': return '#f44336';
      default: return '#666';
    }
  }

  formatMetric(value: number): string {
    return (value * 100).toFixed(2) + '%';
  }

  formatDuration(startTime: string, endTime?: string): string {
    const start = new Date(startTime);
    const end = endTime ? new Date(endTime) : new Date();
    const duration = end.getTime() - start.getTime();
    
    const seconds = Math.floor(duration / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) {
      return `${hours}h ${minutes % 60}m`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    } else {
      return `${seconds}s`;
    }
  }

  isTrainingComplete(): boolean {
    return this.trainingService.isTrainingComplete();
  }

  getHyperparameterControls() {
    const hyperparamGroup = this.trainingForm.get('hyperparameters') as FormGroup;
    return hyperparamGroup ? hyperparamGroup.controls : {};
  }

  getInputType(control: any): string {
    const value = control?.value;
    return typeof value === 'number' ? 'number' : 'text';
  }
}
