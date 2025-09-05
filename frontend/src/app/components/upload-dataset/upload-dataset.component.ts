import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DatasetService } from '../../services/dataset.service';
import { Dataset } from '../../models/dataset.model';

@Component({
  selector: 'app-upload-dataset',
  templateUrl: './upload-dataset.component.html',
  styleUrls: ['./upload-dataset.component.scss']
})
export class UploadDatasetComponent implements OnInit {
  isDragOver = false;
  isUploading = false;
  uploadProgress = 0;
  dataset: Dataset | null = null;

  constructor(
    private datasetService: DatasetService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.datasetService.currentDataset$.subscribe(dataset => {
      this.dataset = dataset;
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
    
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.handleFile(file);
    }
  }

  private handleFile(file: File): void {
    // Validate file type
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.snackBar.open('Please select a CSV file', 'Close', { duration: 3000 });
      return;
    }

    // Validate file size (max 100MB)
    if (file.size > 100 * 1024 * 1024) {
      this.snackBar.open('File size too large. Maximum 100MB allowed.', 'Close', { duration: 3000 });
      return;
    }

    this.uploadFile(file);
  }

  private uploadFile(file: File): void {
    this.isUploading = true;
    this.uploadProgress = 0;

    this.datasetService.uploadDataset(file).subscribe({
      next: (event) => {
        if (event.progress !== undefined) {
          this.uploadProgress = event.progress;
        }
        if (event.response) {
          this.isUploading = false;
          if (event.response.success) {
            this.snackBar.open('Dataset uploaded successfully!', 'Close', { duration: 3000 });
          } else {
            this.snackBar.open(event.response.message || 'Upload failed', 'Close', { duration: 3000 });
          }
        }
      },
      error: (error) => {
        this.isUploading = false;
        this.snackBar.open('Upload failed: ' + error.message, 'Close', { duration: 3000 });
      }
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  formatNumber(num: number): string {
    return num.toLocaleString();
  }
}
