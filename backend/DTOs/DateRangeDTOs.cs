namespace Intellinspect.Backend.DTOs;

public class DateRangeValidationRequestDto
{
    public Guid DatasetId { get; set; }
    public DatePeriodDto Training { get; set; } = new();
    public DatePeriodDto Testing { get; set; } = new();
    public DatePeriodDto Simulation { get; set; } = new();
}

public class DatePeriodDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class DateRangeValidationResponseDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateRangeDto TrainingPeriod { get; set; } = new();
    public DateRangeDto TestingPeriod { get; set; } = new();
    public DateRangeDto SimulationPeriod { get; set; } = new();
    public DateRangeSummaryDto Summary { get; set; } = new();
}

public class DateRangeDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class DateRangeSummaryDto
{
    public int TrainingDays { get; set; }
    public int TestingDays { get; set; }
    public int SimulationDays { get; set; }
    public int TotalRecordsTraining { get; set; }
    public int TotalRecordsTesting { get; set; }
    public int TotalRecordsSimulation { get; set; }
}
