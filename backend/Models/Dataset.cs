using System.ComponentModel.DataAnnotations;

namespace Intellinspect.Backend.Models;

public class Dataset
{
    public Guid Id { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    public string FilePath { get; set; } = string.Empty;
    
    public int TotalRecords { get; set; }
    
    public int TotalColumns { get; set; }
    
    public double PassRate { get; set; }
    
    public DateTime EarliestTimestamp { get; set; }
    
    public DateTime LatestTimestamp { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    public DatasetStatus Status { get; set; } = DatasetStatus.Processing;
    
    public string ColumnsJson { get; set; } = "[]";
    
    public bool HasResponseColumn { get; set; }
    
    public bool HasSyntheticTimestamp { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public List<TrainingJob> TrainingJobs { get; set; } = new();
    public List<SimulationSession> SimulationSessions { get; set; } = new();
}

public enum DatasetStatus
{
    Processing,
    Ready,
    Error
}
