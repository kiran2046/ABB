using Intellinspect.Backend.DTOs;
using Intellinspect.Backend.Models;
using Intellinspect.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CsvHelper;
using System.Globalization;

namespace Intellinspect.Backend.Services;

public interface ISimulationService
{
    Task<Guid> StartSimulationAsync(SimulationConfigDto config);
    Task<SimulationStatusDto?> GetSimulationStatusAsync(Guid simulationId);
    Task<List<PredictionResultDto>> GetPredictionsAsync(Guid simulationId, int limit = 100);
    Task PauseSimulationAsync(Guid simulationId);
    Task ResumeSimulationAsync(Guid simulationId);
    Task StopSimulationAsync(Guid simulationId);
}

public class SimulationService : ISimulationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMLServiceClient _mlServiceClient;
    private readonly ILogger<SimulationService> _logger;
    private readonly Dictionary<Guid, CancellationTokenSource> _runningSimulations = new();

    public SimulationService(
        ApplicationDbContext context,
        IMLServiceClient mlServiceClient,
        ILogger<SimulationService> logger)
    {
        _context = context;
        _mlServiceClient = mlServiceClient;
        _logger = logger;
    }

    public async Task<Guid> StartSimulationAsync(SimulationConfigDto config)
    {
        try
        {
            // Validate model and dataset exist
            var model = await _context.TrainingJobs.FindAsync(config.ModelId);
            var dataset = await _context.Datasets.FindAsync(config.DatasetId);

            if (model == null || dataset == null)
            {
                throw new ArgumentException("Model or dataset not found");
            }

            if (model.Status != TrainingStatus.Completed)
            {
                throw new ArgumentException("Model is not trained");
            }

            // Create simulation session
            var simulation = new SimulationSession
            {
                Id = Guid.NewGuid(),
                ModelId = config.ModelId,
                DatasetId = config.DatasetId,
                StartDate = config.DateRange.Start,
                EndDate = config.DateRange.End,
                RealTimeSpeed = config.RealTimeSpeed,
                Status = SimulationStatus.Starting,
                Progress = 0,
                StartedAt = DateTime.UtcNow
            };

            _context.SimulationSessions.Add(simulation);
            await _context.SaveChangesAsync();

            // Start simulation in background
            var cancellationTokenSource = new CancellationTokenSource();
            _runningSimulations[simulation.Id] = cancellationTokenSource;

            _ = Task.Run(async () => await RunSimulationAsync(simulation, dataset, model, cancellationTokenSource.Token));

            return simulation.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting simulation");
            throw;
        }
    }

    private async Task RunSimulationAsync(SimulationSession simulation, Dataset dataset, TrainingJob model, CancellationToken cancellationToken)
    {
        try
        {
            // Update status to running
            simulation.Status = SimulationStatus.Running;
            await _context.SaveChangesAsync();

            // Read simulation data from CSV
            var simulationData = await ReadSimulationDataAsync(dataset.FilePath, simulation.StartDate, simulation.EndDate);
            var totalRecords = simulationData.Count;

            if (totalRecords == 0)
            {
                simulation.Status = SimulationStatus.Error;
                simulation.ErrorMessage = "No data found for simulation period";
                await _context.SaveChangesAsync();
                return;
            }

            var currentIndex = 0;
            var alertsCount = 0;
            var correctPredictions = 0;
            var delayBetweenRecords = 1000 / simulation.RealTimeSpeed; // milliseconds

            foreach (var record in simulationData)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    simulation.Status = SimulationStatus.Paused;
                    await _context.SaveChangesAsync();
                    break;
                }

                try
                {
                    // Make prediction
                    var predictionRequest = new PredictionRequest
                    {
                        ModelId = model.Id.ToString(),
                        Features = record.SensorValues
                    };

                    var predictionResponse = await _mlServiceClient.PredictAsync(predictionRequest);

                    // Determine if this is an alert
                    var isAlert = predictionResponse.Prediction == 0 && predictionResponse.Confidence > 0.7;

                    // Create prediction result
                    var predictionResult = new PredictionResult
                    {
                        Id = Guid.NewGuid(),
                        SimulationSessionId = simulation.Id,
                        Timestamp = record.Timestamp,
                        Prediction = predictionResponse.Prediction,
                        Confidence = predictionResponse.Confidence,
                        ActualValue = record.ActualValue,
                        IsAlert = isAlert,
                        SensorValuesJson = JsonSerializer.Serialize(record.SensorValues)
                    };

                    _context.PredictionResults.Add(predictionResult);

                    // Create alert if needed
                    if (isAlert)
                    {
                        alertsCount++;
                        var severity = predictionResponse.Confidence > 0.9 ? AlertSeverity.High :
                                     predictionResponse.Confidence > 0.8 ? AlertSeverity.Medium : AlertSeverity.Low;

                        var alert = new QualityAlert
                        {
                            Id = Guid.NewGuid(),
                            SimulationSessionId = simulation.Id,
                            Timestamp = record.Timestamp,
                            Severity = severity,
                            Message = $"Quality issue detected with {(predictionResponse.Confidence * 100):F1}% confidence",
                            Prediction = predictionResponse.Prediction,
                            Confidence = predictionResponse.Confidence,
                            SensorValuesJson = JsonSerializer.Serialize(record.SensorValues)
                        };

                        _context.QualityAlerts.Add(alert);
                    }

                    // Track accuracy
                    if (record.ActualValue.HasValue && predictionResponse.Prediction == record.ActualValue.Value)
                    {
                        correctPredictions++;
                    }

                    currentIndex++;

                    // Update simulation status
                    simulation.Progress = (int)((double)currentIndex / totalRecords * 100);
                    simulation.CurrentTimestamp = record.Timestamp;
                    simulation.PredictionsCount = currentIndex;
                    simulation.AlertsCount = alertsCount;
                    simulation.QualityScore = currentIndex > 0 ? (double)correctPredictions / currentIndex * 100 : 100;

                    // Save changes periodically
                    if (currentIndex % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }

                    // Wait based on simulation speed
                    await Task.Delay(delayBetweenRecords, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing prediction for simulation {SimulationId}", simulation.Id);
                    continue;
                }
            }

            // Mark as completed if not cancelled
            if (!cancellationToken.IsCancellationRequested)
            {
                simulation.Status = SimulationStatus.Completed;
                simulation.Progress = 100;
                simulation.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running simulation {SimulationId}", simulation.Id);
            
            simulation.Status = SimulationStatus.Error;
            simulation.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
        }
        finally
        {
            _runningSimulations.Remove(simulation.Id);
        }
    }

    private async Task<List<SimulationRecord>> ReadSimulationDataAsync(string filePath, DateTime startDate, DateTime endDate)
    {
        var records = new List<SimulationRecord>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();

        var timestampColumn = headers.FirstOrDefault(h => 
            h.Contains("timestamp", StringComparison.OrdinalIgnoreCase) ||
            h.Contains("time", StringComparison.OrdinalIgnoreCase) ||
            h.Contains("date", StringComparison.OrdinalIgnoreCase)) ?? headers[0];

        var responseColumn = headers.FirstOrDefault(h => h.Equals("Response", StringComparison.OrdinalIgnoreCase));

        while (await csv.ReadAsync())
        {
            var timestampValue = csv.GetField(timestampColumn);
            if (!DateTime.TryParse(timestampValue, out var timestamp))
                continue;

            if (timestamp < startDate || timestamp > endDate)
                continue;

            var record = new SimulationRecord
            {
                Timestamp = timestamp,
                SensorValues = new Dictionary<string, double>()
            };

            // Parse response value if available
            if (responseColumn != null)
            {
                var responseValue = csv.GetField(responseColumn);
                if (int.TryParse(responseValue, out var response))
                {
                    record.ActualValue = response;
                }
            }

            // Parse sensor values (all numeric columns except timestamp and response)
            foreach (var header in headers)
            {
                if (header == timestampColumn || header == responseColumn)
                    continue;

                var value = csv.GetField(header);
                if (double.TryParse(value, out var numericValue))
                {
                    record.SensorValues[header] = numericValue;
                }
            }

            records.Add(record);
        }

        return records.OrderBy(r => r.Timestamp).ToList();
    }

    public async Task<SimulationStatusDto?> GetSimulationStatusAsync(Guid simulationId)
    {
        var simulation = await _context.SimulationSessions.FindAsync(simulationId);
        if (simulation == null) return null;

        return new SimulationStatusDto
        {
            Id = simulation.Id,
            Status = simulation.Status.ToString().ToLower(),
            Progress = simulation.Progress,
            CurrentTimestamp = simulation.CurrentTimestamp,
            PredictionsCount = simulation.PredictionsCount,
            AlertsCount = simulation.AlertsCount,
            QualityScore = simulation.QualityScore,
            StartedAt = simulation.StartedAt
        };
    }

    public async Task<List<PredictionResultDto>> GetPredictionsAsync(Guid simulationId, int limit = 100)
    {
        var predictions = await _context.PredictionResults
            .Where(p => p.SimulationSessionId == simulationId)
            .OrderByDescending(p => p.Timestamp)
            .Take(limit)
            .ToListAsync();

        return predictions.Select(p => new PredictionResultDto
        {
            Timestamp = p.Timestamp,
            Prediction = p.Prediction,
            Confidence = p.Confidence,
            ActualValue = p.ActualValue,
            IsAlert = p.IsAlert,
            SensorValues = JsonSerializer.Deserialize<Dictionary<string, double>>(p.SensorValuesJson) ?? new()
        }).ToList();
    }

    public async Task PauseSimulationAsync(Guid simulationId)
    {
        if (_runningSimulations.TryGetValue(simulationId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }

        var simulation = await _context.SimulationSessions.FindAsync(simulationId);
        if (simulation != null)
        {
            simulation.Status = SimulationStatus.Paused;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ResumeSimulationAsync(Guid simulationId)
    {
        var simulation = await _context.SimulationSessions
            .Include(s => s.Dataset)
            .Include(s => s.Model)
            .FirstOrDefaultAsync(s => s.Id == simulationId);

        if (simulation != null && simulation.Status == SimulationStatus.Paused)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _runningSimulations[simulation.Id] = cancellationTokenSource;

            simulation.Status = SimulationStatus.Running;
            await _context.SaveChangesAsync();

            _ = Task.Run(async () => await RunSimulationAsync(simulation, simulation.Dataset, simulation.Model, cancellationTokenSource.Token));
        }
    }

    public async Task StopSimulationAsync(Guid simulationId)
    {
        if (_runningSimulations.TryGetValue(simulationId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            _runningSimulations.Remove(simulationId);
        }

        var simulation = await _context.SimulationSessions.FindAsync(simulationId);
        if (simulation != null)
        {
            simulation.Status = SimulationStatus.Completed;
            simulation.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

public class SimulationRecord
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> SensorValues { get; set; } = new();
    public int? ActualValue { get; set; }
}
