using System.ComponentModel.DataAnnotations;

namespace Intellinspect.Backend.Models;

public class TrainingJob
{
    public Guid Id { get; set; }
    
    public Guid DatasetId { get; set; }
    public Dataset Dataset { get; set; } = null!;
    
    [Required]
    public string Algorithm { get; set; } = string.Empty;
    
    public string HyperparametersJson { get; set; } = "{}";
    
    public double ValidationSplit { get; set; }
    
    public TrainingStatus Status { get; set; } = TrainingStatus.Queued;
    
    public int Progress { get; set; }
    
    public int? CurrentEpoch { get; set; }
    
    public int? TotalEpochs { get; set; }
    
    public string? MetricsJson { get; set; }
    
    public DateTime StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public string? ModelPath { get; set; }
    
    // Navigation properties
    public List<SimulationSession> SimulationSessions { get; set; } = new();
}

public enum TrainingStatus
{
    Queued,
    Training,
    Completed,
    Failed
}

public class TrainingMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double Auc { get; set; }
    public int[][] ConfusionMatrix { get; set; } = Array.Empty<int[]>();
    public double[] TrainingLoss { get; set; } = Array.Empty<double>();
    public double[] ValidationLoss { get; set; } = Array.Empty<double>();
}
