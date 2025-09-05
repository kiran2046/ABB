namespace Intellinspect.Backend.DTOs;

public class SimulationConfigDto
{
    public Guid ModelId { get; set; }
    public Guid DatasetId { get; set; }
    public DatasetDateRangeDto DateRange { get; set; } = new();
    public int RealTimeSpeed { get; set; }
}

public class SimulationStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime? CurrentTimestamp { get; set; }
    public int PredictionsCount { get; set; }
    public int AlertsCount { get; set; }
    public double QualityScore { get; set; }
    public DateTime StartedAt { get; set; }
}

public class PredictionResultDto
{
    public DateTime Timestamp { get; set; }
    public int Prediction { get; set; }
    public double Confidence { get; set; }
    public int? ActualValue { get; set; }
    public bool IsAlert { get; set; }
    public Dictionary<string, double> SensorValues { get; set; } = new();
}

public class QualityAlertDto
{
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Prediction { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> SensorValues { get; set; } = new();
}
