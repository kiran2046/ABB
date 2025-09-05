namespace Intellinspect.Backend.DTOs;

public class TrainingConfigDto
{
    public Guid DatasetId { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
    public double ValidationSplit { get; set; }
}

public class TrainingStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int? CurrentEpoch { get; set; }
    public int? TotalEpochs { get; set; }
    public TrainingMetricsDto? Metrics { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public class TrainingMetricsDto
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

public class ModelInfoDto
{
    public Guid Id { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public DateTime TrainedAt { get; set; }
    public TrainingMetricsDto Metrics { get; set; } = new();
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
