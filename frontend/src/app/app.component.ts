import { Component, OnInit } from '@angular/core';
import { DatasetService } from './services/dataset.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'Intellinspect - Predictive Quality Control';
  currentStep = 0;
  isLinear = true;

  constructor(private datasetService: DatasetService) {}

  ngOnInit() {
    // Initialize application state
  }

  onStepChange(event: any) {
    this.currentStep = event.selectedIndex;
  }

  canProceedToNext(step: number): boolean {
    switch(step) {
      case 0: return this.datasetService.isDatasetUploaded();
      case 1: return this.datasetService.areDateRangesValid();
      case 2: return this.datasetService.isModelTrained();
      default: return false;
    }
  }
}
