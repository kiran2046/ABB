import { Injectable } from '@angular/core';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { Dataset, DatasetMetadata, UploadResponse } from '../models/dataset.model';
import { DateRangeValidation } from '../models/date-range.model';

@Injectable({
  providedIn: 'root'
})
export class DatasetService {
  private apiUrl = environment.apiUrl;
  private currentDataset = new BehaviorSubject<Dataset | null>(null);
  public dateRangeValidation = new BehaviorSubject<DateRangeValidation | null>(null);

  currentDataset$ = this.currentDataset.asObservable();
  dateRangeValidation$ = this.dateRangeValidation.asObservable();

  constructor(private http: HttpClient) {}

  uploadDataset(file: File): Observable<{ progress?: number; response?: UploadResponse | null }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<UploadResponse>(`${this.apiUrl}/upload`, formData, {
      reportProgress: true,
      observe: 'events'
    }).pipe(
      map(event => {
        switch (event.type) {
          case HttpEventType.UploadProgress:
            const progress = Math.round(100 * event.loaded / (event.total || 1));
            return { progress };
          case HttpEventType.Response:
            if (event.body?.success) {
              const dataset: Dataset = {
                id: event.body.datasetId,
                filename: file.name,
                totalRecords: event.body.metadata.rowCount,
                totalColumns: event.body.metadata.columnCount,
                passRate: event.body.metadata.passRate,
                dateRange: {
                  earliest: event.body.metadata.firstTimestamp,
                  latest: event.body.metadata.lastTimestamp
                },
                uploadedAt: new Date().toISOString(),
                status: 'ready',
                columns: event.body.metadata.columns,
                hasResponseColumn: event.body.metadata.hasResponseColumn,
                hasSyntheticTimestamp: !event.body.metadata.firstTimestamp
              };
              this.currentDataset.next(dataset);
            }
            return { response: event.body };
          default:
            return {};
        }
      })
    );
  }

  validateDateRanges(ranges: any): Observable<DateRangeValidation> {
    return this.http.post<DateRangeValidation>(`${this.apiUrl}/dateranges/validate`, {
      datasetId: this.currentDataset.value?.id,
      ...ranges
    }).pipe(
      map(validation => {
        this.dateRangeValidation.next(validation);
        return validation;
      })
    );
  }

  getDataset(id: string): Observable<Dataset> {
    return this.http.get<Dataset>(`${this.apiUrl}/datasets/${id}`);
  }

  isDatasetUploaded(): boolean {
    return this.currentDataset.value?.status === 'ready';
  }

  areDateRangesValid(): boolean {
    return this.dateRangeValidation.value?.isValid === true;
  }

  isModelTrained(): boolean {
    // This will be updated by TrainingService
    return false;
  }

  getCurrentDataset(): Dataset | null {
    return this.currentDataset.value;
  }

  setCurrentDataset(dataset: Dataset): void {
    this.currentDataset.next(dataset);
  }
}
