export interface TrainingConfig {
  datasetId: string;
  algorithm: 'xgboost' | 'lightgbm' | 'scikit-learn';
  hyperparameters: any;
  validationSplit: number;
}

export interface TrainingStatus {
  id: string;
  status: 'queued' | 'training' | 'completed' | 'failed';
  progress: number;
  currentEpoch?: number;
  totalEpochs?: number;
  metrics?: TrainingMetrics;
  startedAt: string;
  completedAt?: string;
  error?: string;
}

export interface TrainingMetrics {
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  auc: number;
  confusionMatrix: number[][];
  trainingLoss: number[];
  validationLoss: number[];
}

export interface ModelInfo {
  id: string;
  algorithm: string;
  trainedAt: string;
  metrics: TrainingMetrics;
  hyperparameters: any;
  status: 'ready' | 'training' | 'error';
}
