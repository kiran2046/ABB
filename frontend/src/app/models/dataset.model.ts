export interface Dataset {
  id: string;
  filename: string;
  totalRecords: number;
  totalColumns: number;
  passRate: number;
  dateRange: {
    earliest: string;
    latest: string;
  };
  uploadedAt: string;
  status: 'uploading' | 'processing' | 'ready' | 'error';
  columns: string[];
  hasResponseColumn: boolean;
  hasSyntheticTimestamp: boolean;
}

export interface DatasetMetadata {
  rowCount: number;
  columnCount: number;
  passRate: number;
  firstTimestamp: string;
  lastTimestamp: string;
  columns: string[];
  hasResponseColumn: boolean;
}

export interface UploadResponse {
  success: boolean;
  datasetId: string;
  metadata: DatasetMetadata;
  message?: string;
}
