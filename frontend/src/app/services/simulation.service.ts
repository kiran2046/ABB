import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { SimulationConfig, SimulationStatus, PredictionResult, QualityAlert } from '../models/simulation.model';

@Injectable({
  providedIn: 'root'
})
export class SimulationService {
  private apiUrl = environment.apiUrl;
  private currentSimulationStatus = new BehaviorSubject<SimulationStatus | null>(null);
  private predictions = new BehaviorSubject<PredictionResult[]>([]);
  private alerts = new BehaviorSubject<QualityAlert[]>([]);

  currentSimulationStatus$ = this.currentSimulationStatus.asObservable();
  predictions$ = this.predictions.asObservable();
  alerts$ = this.alerts.asObservable();

  constructor(private http: HttpClient) {}

  startSimulation(config: SimulationConfig): Observable<SimulationStatus> {
    return this.http.post<{simulationId: string}>(`${this.apiUrl}/simulation/start`, config)
      .pipe(
        switchMap(response => {
          return this.pollSimulationStatus(response.simulationId);
        })
      );
  }

  private pollSimulationStatus(simulationId: string): Observable<SimulationStatus> {
    return interval(1000).pipe(
      switchMap(() => this.getSimulationStatus(simulationId)),
      takeWhile((status: SimulationStatus) => {
        this.currentSimulationStatus.next(status);
        this.fetchLatestPredictions(simulationId);
        return status.status === 'running';
      }, true)
    );
  }

  getSimulationStatus(simulationId: string): Observable<SimulationStatus> {
    return this.http.get<SimulationStatus>(`${this.apiUrl}/simulation/status/${simulationId}`);
  }

  pauseSimulation(simulationId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/simulation/pause/${simulationId}`, {});
  }

  resumeSimulation(simulationId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/simulation/resume/${simulationId}`, {});
  }

  stopSimulation(simulationId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/simulation/stop/${simulationId}`, {});
  }

  private fetchLatestPredictions(simulationId: string): void {
    this.http.get<PredictionResult[]>(`${this.apiUrl}/simulation/predictions/${simulationId}`)
      .subscribe(predictions => {
        this.predictions.next(predictions);
        
        // Extract alerts from predictions
        const newAlerts = predictions
          .filter(p => p.isAlert)
          .map(p => this.createAlert(p));
        
        this.alerts.next([...this.alerts.value, ...newAlerts]);
      });
  }

  private createAlert(prediction: PredictionResult): QualityAlert {
    let severity: 'low' | 'medium' | 'high' = 'low';
    
    if (prediction.confidence > 0.9) {
      severity = 'high';
    } else if (prediction.confidence > 0.7) {
      severity = 'medium';
    }

    return {
      timestamp: prediction.timestamp,
      severity,
      message: `Quality issue detected with ${(prediction.confidence * 100).toFixed(1)}% confidence`,
      prediction: prediction.prediction,
      confidence: prediction.confidence,
      sensorValues: prediction.sensorValues
    };
  }

  getPredictions(): PredictionResult[] {
    return this.predictions.value;
  }

  getAlerts(): QualityAlert[] {
    return this.alerts.value;
  }

  clearAlerts(): void {
    this.alerts.next([]);
  }

  getCurrentSimulationStatus(): SimulationStatus | null {
    return this.currentSimulationStatus.value;
  }
}
