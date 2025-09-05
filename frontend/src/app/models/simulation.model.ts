export interface SimulationConfig {
  modelId: string;
  datasetId: string;
  dateRange: {
    start: Date;
    end: Date;
  };
  realTimeSpeed: number; // 1x, 2x, 5x, 10x
}

export interface SimulationStatus {
  id: string;
  status: 'starting' | 'running' | 'paused' | 'completed' | 'error';
  progress: number;
  currentTimestamp: string;
  predictionsCount: number;
  alertsCount: number;
  qualityScore: number;
  startedAt: string;
}

export interface PredictionResult {
  timestamp: string;
  prediction: number;
  confidence: number;
  actualValue?: number;
  isAlert: boolean;
  sensorValues: { [key: string]: number };
}

export interface QualityAlert {
  timestamp: string;
  severity: 'low' | 'medium' | 'high';
  message: string;
  prediction: number;
  confidence: number;
  sensorValues: { [key: string]: number };
}
