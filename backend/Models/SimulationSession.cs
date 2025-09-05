using System.ComponentModel.DataAnnotations;

namespace Intellinspect.Backend.Models;

public class SimulationSession
{
    public Guid Id { get; set; }
    
    public Guid ModelId { get; set; }
    public TrainingJob Model { get; set; } = null!;
    
    public Guid DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public int RealTimeSpeed { get; set; }
    
    public SimulationStatus Status { get; set; } = SimulationStatus.Starting;
    
    public int Progress { get; set; }
    
    public DateTime? CurrentTimestamp { get; set; }
    
    public int PredictionsCount { get; set; }
    
    public int AlertsCount { get; set; }
    
    public double QualityScore { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public List<PredictionResult> Predictions { get; set; } = new();
    public List<QualityAlert> Alerts { get; set; } = new();
}

public enum SimulationStatus
{
    Starting,
    Running,
    Paused,
    Completed,
    Error
}

public class PredictionResult
{
    public Guid Id { get; set; }
    
    public Guid SimulationSessionId { get; set; }
    public SimulationSession SimulationSession { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    
    public int Prediction { get; set; }
    
    public double Confidence { get; set; }
    
    public int? ActualValue { get; set; }
    
    public bool IsAlert { get; set; }
    
    public string SensorValuesJson { get; set; } = "{}";
}

public class QualityAlert
{
    public Guid Id { get; set; }
    
    public Guid SimulationSessionId { get; set; }
    public SimulationSession SimulationSession { get; set; } = null!;
    
    public DateTime Timestamp { get; set; }
    
    public AlertSeverity Severity { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public int Prediction { get; set; }
    
    public double Confidence { get; set; }
    
    public string SensorValuesJson { get; set; } = "{}";
}

public enum AlertSeverity
{
    Low,
    Medium,
    High
}
