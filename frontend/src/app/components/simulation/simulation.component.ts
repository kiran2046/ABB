import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { SimulationService } from '../../services/simulation.service';
import { TrainingService } from '../../services/training.service';
import { DatasetService } from '../../services/dataset.service';
import { SimulationConfig, SimulationStatus, PredictionResult, QualityAlert } from '../../models/simulation.model';

@Component({
  selector: 'app-simulation',
  templateUrl: './simulation.component.html',
  styleUrls: ['./simulation.component.scss']
})
export class SimulationComponent implements OnInit, OnDestroy {
  simulationForm: FormGroup;
  simulationStatus: SimulationStatus | null = null;
  predictions: PredictionResult[] = [];
  alerts: QualityAlert[] = [];
  
  isRunning = false;
  isPaused = false;
  
  private subscriptions: Subscription[] = [];

  speedOptions = [
    { value: 1, label: '1x (Real-time)' },
    { value: 2, label: '2x' },
    { value: 5, label: '5x' },
    { value: 10, label: '10x' }
  ];

  constructor(
    private fb: FormBuilder,
    private simulationService: SimulationService,
    private trainingService: TrainingService,
    private datasetService: DatasetService,
    private snackBar: MatSnackBar
  ) {
    this.simulationForm = this.createForm();
  }

  ngOnInit(): void {
    this.subscriptions.push(
      this.simulationService.currentSimulationStatus$.subscribe(status => {
        this.simulationStatus = status;
        this.isRunning = status?.status === 'running';
        this.isPaused = status?.status === 'paused';
      })
    );

    this.subscriptions.push(
      this.simulationService.predictions$.subscribe(predictions => {
        this.predictions = predictions.slice(-100); // Keep last 100 predictions
      })
    );

    this.subscriptions.push(
      this.simulationService.alerts$.subscribe(alerts => {
        this.alerts = alerts.slice(-20); // Keep last 20 alerts
      })
    );

    this.initializeForm();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  private createForm(): FormGroup {
    return this.fb.group({
      realTimeSpeed: [1, Validators.required]
    });
  }

  private initializeForm(): void {
    const dateRange = this.datasetService.dateRangeValidation?.value;
    if (dateRange?.simulationPeriod) {
      // Form is automatically initialized with simulation period from date ranges
    }
  }

  startSimulation(): void {
    const dataset = this.datasetService.getCurrentDataset();
    const model = this.trainingService.getCurrentModel();
    const dateRange = this.datasetService.dateRangeValidation?.value;

    if (!dataset || !model || !dateRange) {
      this.snackBar.open('Missing required data for simulation', 'Close', { duration: 3000 });
      return;
    }

    const config: SimulationConfig = {
      modelId: model.id,
      datasetId: dataset.id,
      dateRange: {
        start: dateRange.simulationPeriod.start,
        end: dateRange.simulationPeriod.end
      },
      realTimeSpeed: this.simulationForm.value.realTimeSpeed
    };

    this.simulationService.startSimulation(config).subscribe({
      next: (status) => {
        if (status.status === 'completed') {
          this.snackBar.open('Simulation completed successfully!', 'Close', { duration: 3000 });
        }
      },
      error: (error) => {
        this.snackBar.open('Simulation failed: ' + error.message, 'Close', { duration: 5000 });
      }
    });
  }

  pauseSimulation(): void {
    if (this.simulationStatus?.id) {
      this.simulationService.pauseSimulation(this.simulationStatus.id).subscribe({
        next: () => {
          this.snackBar.open('Simulation paused', 'Close', { duration: 2000 });
        },
        error: (error) => {
          this.snackBar.open('Failed to pause: ' + error.message, 'Close', { duration: 3000 });
        }
      });
    }
  }

  resumeSimulation(): void {
    if (this.simulationStatus?.id) {
      this.simulationService.resumeSimulation(this.simulationStatus.id).subscribe({
        next: () => {
          this.snackBar.open('Simulation resumed', 'Close', { duration: 2000 });
        },
        error: (error) => {
          this.snackBar.open('Failed to resume: ' + error.message, 'Close', { duration: 3000 });
        }
      });
    }
  }

  stopSimulation(): void {
    if (this.simulationStatus?.id) {
      this.simulationService.stopSimulation(this.simulationStatus.id).subscribe({
        next: () => {
          this.snackBar.open('Simulation stopped', 'Close', { duration: 2000 });
        },
        error: (error) => {
          this.snackBar.open('Failed to stop: ' + error.message, 'Close', { duration: 3000 });
        }
      });
    }
  }

  clearAlerts(): void {
    this.simulationService.clearAlerts();
    this.snackBar.open('Alerts cleared', 'Close', { duration: 2000 });
  }

  getStatusDisplayName(status: string): string {
    switch (status) {
      case 'starting': return 'Starting...';
      case 'running': return 'Running';
      case 'paused': return 'Paused';
      case 'completed': return 'Completed';
      case 'error': return 'Error';
      default: return status;
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'starting': return 'hourglass_empty';
      case 'running': return 'play_circle_filled';
      case 'paused': return 'pause_circle_filled';
      case 'completed': return 'check_circle';
      case 'error': return 'error';
      default: return 'help';
    }
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'starting': return '#ff9800';
      case 'running': return '#4caf50';
      case 'paused': return '#2196f3';
      case 'completed': return '#4caf50';
      case 'error': return '#f44336';
      default: return '#666';
    }
  }

  getSeverityColor(severity: string): string {
    switch (severity) {
      case 'high': return '#f44336';
      case 'medium': return '#ff9800';
      case 'low': return '#ffc107';
      default: return '#666';
    }
  }

  getSeverityIcon(severity: string): string {
    switch (severity) {
      case 'high': return 'error';
      case 'medium': return 'warning';
      case 'low': return 'info';
      default: return 'help';
    }
  }

  formatConfidence(confidence: number): string {
    return (confidence * 100).toFixed(1) + '%';
  }

  formatNumber(num: number): string {
    return num?.toLocaleString() || '0';
  }

  getQualityScoreColor(score: number): string {
    if (score >= 90) return '#4caf50';
    if (score >= 70) return '#ff9800';
    return '#f44336';
  }
}
