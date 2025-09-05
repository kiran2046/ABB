import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

// Angular Material Modules
import { MatStepperModule } from '@angular/material/stepper';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBarModule } from '@angular/material/snack-bar';

// Components
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { UploadDatasetComponent } from './components/upload-dataset/upload-dataset.component';
import { DateRangesComponent } from './components/date-ranges/date-ranges.component';
import { ModelTrainingComponent } from './components/model-training/model-training.component';
import { SimulationComponent } from './components/simulation/simulation.component';

// Services
import { DatasetService } from './services/dataset.service';
import { TrainingService } from './services/training.service';
import { SimulationService } from './services/simulation.service';

@NgModule({
  declarations: [
    AppComponent,
    UploadDatasetComponent,
    DateRangesComponent,
    ModelTrainingComponent,
    SimulationComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
    
    // Material Modules
    MatStepperModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatToolbarModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSelectModule,
    MatTableModule,
    MatChipsModule,
    MatSnackBarModule
  ],
  providers: [
    DatasetService,
    TrainingService,
    SimulationService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
