import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DatasetService } from '../../services/dataset.service';
import { DateRange, DateRangeValidation } from '../../models/date-range.model';
import { Dataset } from '../../models/dataset.model';

@Component({
  selector: 'app-date-ranges',
  templateUrl: './date-ranges.component.html',
  styleUrls: ['./date-ranges.component.scss']
})
export class DateRangesComponent implements OnInit {
  dateForm: FormGroup;
  dataset: Dataset | null = null;
  validation: DateRangeValidation | null = null;
  isValidating = false;

  constructor(
    private fb: FormBuilder,
    private datasetService: DatasetService,
    private snackBar: MatSnackBar
  ) {
    this.dateForm = this.createForm();
  }

  ngOnInit(): void {
    this.datasetService.currentDataset$.subscribe(dataset => {
      this.dataset = dataset;
      if (dataset) {
        this.initializeDateRanges();
      }
    });

    this.datasetService.dateRangeValidation$.subscribe(validation => {
      this.validation = validation;
    });
  }

  private createForm(): FormGroup {
    return this.fb.group({
      trainingStart: ['', Validators.required],
      trainingEnd: ['', Validators.required],
      testingStart: ['', Validators.required],
      testingEnd: ['', Validators.required],
      simulationStart: ['', Validators.required],
      simulationEnd: ['', Validators.required]
    });
  }

  private initializeDateRanges(): void {
    if (!this.dataset) return;

    const earliest = new Date(this.dataset.dateRange.earliest);
    const latest = new Date(this.dataset.dateRange.latest);
    const totalDays = Math.floor((latest.getTime() - earliest.getTime()) / (1000 * 60 * 60 * 24));

    // Automatically split into 60% training, 20% testing, 20% simulation
    const trainingDays = Math.floor(totalDays * 0.6);
    const testingDays = Math.floor(totalDays * 0.2);

    const trainingEnd = new Date(earliest.getTime() + trainingDays * 24 * 60 * 60 * 1000);
    const testingEnd = new Date(trainingEnd.getTime() + testingDays * 24 * 60 * 60 * 1000);

    this.dateForm.patchValue({
      trainingStart: earliest,
      trainingEnd: trainingEnd,
      testingStart: trainingEnd,
      testingEnd: testingEnd,
      simulationStart: testingEnd,
      simulationEnd: latest
    });
  }

  validateDateRanges(): void {
    if (!this.dateForm.valid) {
      this.snackBar.open('Please fill in all date fields', 'Close', { duration: 3000 });
      return;
    }

    this.isValidating = true;
    const formValue = this.dateForm.value;

    this.datasetService.validateDateRanges({
      training: {
        start: formValue.trainingStart,
        end: formValue.trainingEnd
      },
      testing: {
        start: formValue.testingStart,
        end: formValue.testingEnd
      },
      simulation: {
        start: formValue.simulationStart,
        end: formValue.simulationEnd
      }
    }).subscribe({
      next: (validation) => {
        this.isValidating = false;
        if (validation.isValid) {
          this.snackBar.open('Date ranges validated successfully!', 'Close', { duration: 3000 });
        } else {
          this.snackBar.open('Date range validation failed', 'Close', { duration: 3000 });
        }
      },
      error: (error) => {
        this.isValidating = false;
        this.snackBar.open('Validation failed: ' + error.message, 'Close', { duration: 3000 });
      }
    });
  }

  getTimelineBars(): any[] {
    if (!this.validation?.isValid) return [];

    return [
      {
        label: 'Training Period',
        color: '#4caf50',
        width: 33.33
      },
      {
        label: 'Testing Period',
        color: '#ff9800',
        width: 33.33
      },
      {
        label: 'Simulation Period',
        color: '#2196f3',
        width: 33.34
      }
    ];
  }

  formatDuration(days: number): string {
    if (days < 30) {
      return `${days} days`;
    } else if (days < 365) {
      const months = Math.floor(days / 30);
      const remainingDays = days % 30;
      return `${months}m ${remainingDays}d`;
    } else {
      const years = Math.floor(days / 365);
      const remainingDays = days % 365;
      return `${years}y ${remainingDays}d`;
    }
  }

  formatNumber(num: number): string {
    return num?.toLocaleString() || '0';
  }
}
