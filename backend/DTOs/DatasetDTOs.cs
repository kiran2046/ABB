namespace Intellinspect.Backend.DTOs;

public class UploadResponseDto
{
    public bool Success { get; set; }
    public Guid DatasetId { get; set; }
    public DatasetMetadataDto Metadata { get; set; } = new();
    public string? Message { get; set; }
}

public class DatasetMetadataDto
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public double PassRate { get; set; }
    public DateTime FirstTimestamp { get; set; }
    public DateTime LastTimestamp { get; set; }
    public List<string> Columns { get; set; } = new();
    public bool HasResponseColumn { get; set; }
}

public class DatasetDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int TotalColumns { get; set; }
    public double PassRate { get; set; }
    public DatasetDateRangeDto DateRange { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool HasResponseColumn { get; set; }
    public bool HasSyntheticTimestamp { get; set; }
}

public class DatasetDateRangeDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
