import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { TrainingConfig, TrainingStatus, TrainingMetrics, ModelInfo } from '../models/training.model';

@Injectable({
  providedIn: 'root'
})
export class TrainingService {
  private apiUrl = environment.apiUrl;
  private currentTrainingStatus = new BehaviorSubject<TrainingStatus | null>(null);
  private currentModel = new BehaviorSubject<ModelInfo | null>(null);

  currentTrainingStatus$ = this.currentTrainingStatus.asObservable();
  currentModel$ = this.currentModel.asObservable();

  constructor(private http: HttpClient) {}

  startTraining(config: TrainingConfig): Observable<TrainingStatus> {
    return this.http.post<{trainingId: string}>(`${this.apiUrl}/training/start`, config)
      .pipe(
        switchMap(response => {
          return this.pollTrainingStatus(response.trainingId);
        })
      );
  }

  private pollTrainingStatus(trainingId: string): Observable<TrainingStatus> {
    return interval(2000).pipe(
      switchMap(() => this.getTrainingStatus(trainingId)),
      takeWhile((status: TrainingStatus) => {
        this.currentTrainingStatus.next(status);
        return status.status === 'training' || status.status === 'queued';
      }, true)
    );
  }

  getTrainingStatus(trainingId: string): Observable<TrainingStatus> {
    return this.http.get<TrainingStatus>(`${this.apiUrl}/training/status/${trainingId}`);
  }

  getModel(modelId: string): Observable<ModelInfo> {
    return this.http.get<ModelInfo>(`${this.apiUrl}/models/${modelId}`);
  }

  getAvailableAlgorithms(): string[] {
    return ['xgboost', 'lightgbm', 'scikit-learn'];
  }

  getCurrentModel(): ModelInfo | null {
    return this.currentModel.value;
  }

  setCurrentModel(model: ModelInfo): void {
    this.currentModel.next(model);
  }

  isTrainingComplete(): boolean {
    const status = this.currentTrainingStatus.value;
    return status?.status === 'completed';
  }

  getDefaultHyperparameters(algorithm: string): any {
    switch (algorithm) {
      case 'xgboost':
        return {
          max_depth: 6,
          learning_rate: 0.1,
          n_estimators: 100,
          subsample: 0.8,
          colsample_bytree: 0.8
        };
      case 'lightgbm':
        return {
          num_leaves: 31,
          learning_rate: 0.1,
          n_estimators: 100,
          subsample: 0.8,
          colsample_bytree: 0.8
        };
      case 'scikit-learn':
        return {
          n_estimators: 100,
          max_depth: 10,
          min_samples_split: 2,
          min_samples_leaf: 1
        };
      default:
        return {};
    }
  }
}
