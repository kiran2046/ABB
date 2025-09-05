using Intellinspect.Backend.DTOs;
using Intellinspect.Backend.Models;
using Intellinspect.Backend.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;
using System.Text.Json;

namespace Intellinspect.Backend.Services;

public interface IDatasetService
{
    Task<UploadResponseDto> UploadDatasetAsync(IFormFile file);
    Task<DatasetDto?> GetDatasetAsync(Guid id);
    Task<DateRangeValidationResponseDto> ValidateDateRangesAsync(DateRangeValidationRequestDto request);
}

public class DatasetService : IDatasetService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatasetService> _logger;
    private readonly string _uploadPath;

    public DatasetService(ApplicationDbContext context, ILogger<DatasetService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _uploadPath = configuration["UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<UploadResponseDto> UploadDatasetAsync(IFormFile file)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return new UploadResponseDto { Success = false, Message = "No file provided" };
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return new UploadResponseDto { Success = false, Message = "Only CSV files are supported" };
            }

            // Create dataset entity
            var dataset = new Dataset
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                UploadedAt = DateTime.UtcNow,
                Status = DatasetStatus.Processing
            };

            // Save file
            var filePath = Path.Combine(_uploadPath, $"{dataset.Id}_{file.FileName}");
            dataset.FilePath = filePath;

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Parse and analyze CSV
            var metadata = await AnalyzeCsvAsync(filePath, dataset);
            
            // Save to database
            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync();

            return new UploadResponseDto
            {
                Success = true,
                DatasetId = dataset.Id,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
            return new UploadResponseDto { Success = false, Message = ex.Message };
        }
    }

    private async Task<DatasetMetadataDto> AnalyzeCsvAsync(string filePath, Dataset dataset)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();

        // Check for Response column
        var hasResponseColumn = headers.Any(h => h.Equals("Response", StringComparison.OrdinalIgnoreCase));

        // Read all records to analyze
        var records = new List<Dictionary<string, string>>();
        while (await csv.ReadAsync())
        {
            var record = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header) ?? string.Empty;
            }
            records.Add(record);
        }

        // Calculate statistics
        var totalRecords = records.Count;
        var passCount = 0;
        var hasSyntheticTimestamp = false;
        DateTime earliestTimestamp = DateTime.MaxValue;
        DateTime latestTimestamp = DateTime.MinValue;

        // Check for timestamp column or create synthetic timestamps
        var timestampColumn = headers.FirstOrDefault(h => 
            h.Contains("time", StringComparison.OrdinalIgnoreCase) || 
            h.Contains("date", StringComparison.OrdinalIgnoreCase));

        if (timestampColumn != null)
        {
            // Parse existing timestamps
            foreach (var record in records)
            {
                if (DateTime.TryParse(record[timestampColumn], out var timestamp))
                {
                    if (timestamp < earliestTimestamp) earliestTimestamp = timestamp;
                    if (timestamp > latestTimestamp) latestTimestamp = timestamp;
                }
            }
        }
        else
        {
            // Create synthetic timestamps (1 second apart starting from 2021-01-01)
            hasSyntheticTimestamp = true;
            earliestTimestamp = new DateTime(2021, 1, 1);
            latestTimestamp = earliestTimestamp.AddSeconds(totalRecords - 1);
            
            // Add synthetic timestamp column to CSV
            await AddSyntheticTimestampsAsync(filePath, headers, earliestTimestamp);
            headers.Insert(0, "synthetic_timestamp");
        }

        // Calculate pass rate
        if (hasResponseColumn)
        {
            var responseColumn = headers.First(h => h.Equals("Response", StringComparison.OrdinalIgnoreCase));
            passCount = records.Count(r => r[responseColumn] == "1");
        }

        var passRate = totalRecords > 0 ? (double)passCount / totalRecords * 100 : 0;

        // Update dataset
        dataset.TotalRecords = totalRecords;
        dataset.TotalColumns = headers.Count;
        dataset.PassRate = passRate;
        dataset.EarliestTimestamp = earliestTimestamp;
        dataset.LatestTimestamp = latestTimestamp;
        dataset.ColumnsJson = JsonSerializer.Serialize(headers);
        dataset.HasResponseColumn = hasResponseColumn;
        dataset.HasSyntheticTimestamp = hasSyntheticTimestamp;
        dataset.Status = DatasetStatus.Ready;

        return new DatasetMetadataDto
        {
            RowCount = totalRecords,
            ColumnCount = headers.Count,
            PassRate = passRate,
            FirstTimestamp = earliestTimestamp,
            LastTimestamp = latestTimestamp,
            Columns = headers,
            HasResponseColumn = hasResponseColumn
        };
    }

    private async Task AddSyntheticTimestampsAsync(string filePath, List<string> headers, DateTime startTime)
    {
        var tempPath = filePath + ".temp";
        
        using (var reader = new StreamReader(filePath))
        using (var writer = new StreamWriter(tempPath))
        using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
        using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            // Write new header with synthetic timestamp
            var newHeaders = new List<string> { "synthetic_timestamp" };
            newHeaders.AddRange(headers);
            
            foreach (var header in newHeaders)
            {
                csvWriter.WriteField(header);
            }
            csvWriter.NextRecord();

            // Read original records and add timestamps
            await csvReader.ReadAsync();
            csvReader.ReadHeader();
            
            var currentTime = startTime;
            while (await csvReader.ReadAsync())
            {
                // Write timestamp
                csvWriter.WriteField(currentTime.ToString("yyyy-MM-dd HH:mm:ss"));
                
                // Write original fields
                foreach (var header in headers)
                {
                    csvWriter.WriteField(csvReader.GetField(header));
                }
                csvWriter.NextRecord();
                
                currentTime = currentTime.AddSeconds(1);
            }
        }

        // Replace original file
        File.Delete(filePath);
        File.Move(tempPath, filePath);
    }

    public async Task<DatasetDto?> GetDatasetAsync(Guid id)
    {
        var dataset = await _context.Datasets.FindAsync(id);
        if (dataset == null) return null;

        var columns = JsonSerializer.Deserialize<List<string>>(dataset.ColumnsJson) ?? new List<string>();

        return new DatasetDto
        {
            Id = dataset.Id,
            FileName = dataset.FileName,
            TotalRecords = dataset.TotalRecords,
            TotalColumns = dataset.TotalColumns,
            PassRate = dataset.PassRate,
            DateRange = new DatasetDateRangeDto
            {
                Start = dataset.EarliestTimestamp,
                End = dataset.LatestTimestamp
            },
            UploadedAt = dataset.UploadedAt,
            Status = dataset.Status.ToString(),
            Columns = columns,
            HasResponseColumn = dataset.HasResponseColumn,
            HasSyntheticTimestamp = dataset.HasSyntheticTimestamp
        };
    }

    public async Task<DateRangeValidationResponseDto> ValidateDateRangesAsync(DateRangeValidationRequestDto request)
    {
        var dataset = await _context.Datasets.FindAsync(request.DatasetId);
        if (dataset == null)
        {
            return new DateRangeValidationResponseDto
            {
                IsValid = false,
                Errors = new List<string> { "Dataset not found" }
            };
        }

        var errors = new List<string>();

        // Validate date ranges are sequential and non-overlapping
        if (request.Training.End >= request.Testing.Start)
        {
            errors.Add("Training period must end before testing period starts");
        }

        if (request.Testing.End >= request.Simulation.Start)
        {
            errors.Add("Testing period must end before simulation period starts");
        }

        // Validate dates are within dataset range
        if (request.Training.Start < dataset.EarliestTimestamp)
        {
            errors.Add("Training start date is before dataset earliest timestamp");
        }

        if (request.Simulation.End > dataset.LatestTimestamp)
        {
            errors.Add("Simulation end date is after dataset latest timestamp");
        }

        // Calculate summary
        var trainingDays = (request.Training.End - request.Training.Start).Days;
        var testingDays = (request.Testing.End - request.Testing.Start).Days;
        var simulationDays = (request.Simulation.End - request.Simulation.Start).Days;

        // Estimate record counts (assuming 1 record per second)
        var trainingRecords = (int)(request.Training.End - request.Training.Start).TotalSeconds;
        var testingRecords = (int)(request.Testing.End - request.Testing.Start).TotalSeconds;
        var simulationRecords = (int)(request.Simulation.End - request.Simulation.Start).TotalSeconds;

        return new DateRangeValidationResponseDto
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            TrainingPeriod = new DateRangeDto
            {
                Start = request.Training.Start,
                End = request.Training.End,
                Type = "training",
                Label = "Training Period",
                Color = "#4caf50"
            },
            TestingPeriod = new DateRangeDto
            {
                Start = request.Testing.Start,
                End = request.Testing.End,
                Type = "testing",
                Label = "Testing Period",
                Color = "#ff9800"
            },
            SimulationPeriod = new DateRangeDto
            {
                Start = request.Simulation.Start,
                End = request.Simulation.End,
                Type = "simulation",
                Label = "Simulation Period",
                Color = "#2196f3"
            },
            Summary = new DateRangeSummaryDto
            {
                TrainingDays = trainingDays,
                TestingDays = testingDays,
                SimulationDays = simulationDays,
                TotalRecordsTraining = trainingRecords,
                TotalRecordsTesting = testingRecords,
                TotalRecordsSimulation = simulationRecords
            }
        };
    }
}
