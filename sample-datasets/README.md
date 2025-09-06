# Sample Datasets for Intellinspect Application

This folder contains sample manufacturing datasets designed for testing the Intellinspect ML-powered quality control application.

## Available Datasets

### 1. bosch_production_line_dataset.csv
**Size:** ~0.94 MB (14,690 records)  
**Date Range:** August 1, 2015 - December 31, 2015 (5 months)  
**Frequency:** Every 15 minutes  
**Defect Rate:** ~12-15% (realistic for manufacturing)

**Features:**
- **Timestamp:** Date and time of measurement
- **Temperature:** Process temperature (°C) - Range: ~71-84°C
- **Pressure:** System pressure - Range: ~12-16
- **Vibration:** Equipment vibration level - Range: ~0.1-4.0
- **Speed:** Motor/equipment speed (RPM) - Range: ~1100-1300
- **Current:** Electrical current (A) - Range: ~3-5A
- **Voltage:** Supply voltage (V) - Range: ~200-230V
- **Flow_Rate:** Material flow rate - Range: ~10-13
- **Humidity:** Environmental humidity (%) - Range: ~30-55%
- **Material_Thickness:** Product thickness measurement - Range: ~1.8-2.3
- **Tool_Wear:** Tool wear indicator (0-1) - Range: ~0.1-0.8
- **Response:** Quality outcome (0=Good, 1=Defect)

**Realistic Patterns:**
- Daily temperature cycles following actual manufacturing patterns
- Gradual equipment wear over time affecting multiple sensors
- Correlated sensor readings (e.g., higher temperature affects pressure)
- Night shift quality issues (slightly higher defect rates 22:00-06:00)
- Realistic defect triggers:
  - Temperature > 80°C increases defect probability by 30%
  - High vibration (>1.5) increases defect probability by 25%
  - High tool wear (>0.4) increases defect probability by 20%
  - Low pressure (<13) increases defect probability by 15%
  - Thin material (<1.8) increases defect probability by 10%

### 2. production_line_quality_dataset.csv
**Size:** ~0.007 MB (100 records)  
**Date Range:** August 1-2, 2015 (2 days)  
**Frequency:** Hourly  
**Purpose:** Small test dataset for quick validation

## Dataset Quality Features

✅ **Realistic Manufacturing Data:**
- Sensor correlations match real industrial equipment
- Time-based patterns (daily cycles, equipment degradation)
- Appropriate noise levels and measurement ranges

✅ **ML-Ready Format:**
- Clean timestamps in YYYY-MM-DD HH:MM:SS format
- Binary response variable for classification
- No missing values
- Proper numeric ranges for all sensors

✅ **Application Compatible:**
- Covers full date range for testing training/testing/simulation splits
- Response column for supervised learning
- Reasonable file sizes (under 100MB limit)
- Standard CSV format with headers

## Usage Instructions

1. **Upload Dataset:** Use the File Upload feature in Intellinspect
2. **Select Date Ranges:**
   - Training: Aug 1 - Oct 15, 2015 (40% of data)
   - Testing: Oct 16 - Nov 15, 2015 (20% of data)  
   - Simulation: Nov 16 - Dec 31, 2015 (remaining data)
3. **Configure Analysis:** Select relevant sensors for quality prediction
4. **Run Analysis:** Execute ML pipeline for quality control insights

## Expected Results

With this dataset, you should expect:
- **Model Accuracy:** 85-92% (typical for manufacturing quality control)
- **Key Predictors:** Temperature, Vibration, Tool_Wear likely most important
- **Temporal Patterns:** Equipment degradation over time, daily cycles
- **Actionable Insights:** Temperature and vibration thresholds for quality control

The dataset is designed to provide realistic ML training scenarios while being clean enough to avoid data preprocessing issues.
