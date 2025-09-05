export interface DateRange {
  start: Date;
  end: Date;
  type: 'training' | 'testing' | 'simulation';
  label: string;
  color: string;
}

export interface DateRangeValidation {
  isValid: boolean;
  errors: string[];
  trainingPeriod: DateRange;
  testingPeriod: DateRange;
  simulationPeriod: DateRange;
  summary: {
    trainingDays: number;
    testingDays: number;
    simulationDays: number;
    totalRecordsTraining: number;
    totalRecordsTesting: number;
    totalRecordsSimulation: number;
  };
}
