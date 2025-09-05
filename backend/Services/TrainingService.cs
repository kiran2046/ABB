using Intellinspect.Backend.DTOs;
using Intellinspect.Backend.Models;
using Intellinspect.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Intellinspect.Backend.Services;

public interface ITrainingService
{
    Task<Guid> StartTrainingAsync(TrainingConfigDto config);
    Task<TrainingStatusDto?> GetTrainingStatusAsync(Guid trainingId);
    Task<ModelInfoDto?> GetModelAsync(Guid modelId);
    Task UpdateTrainingStatusAsync(Guid trainingId, string status, int progress, TrainingMetricsDto? metrics = null);
}

public class TrainingService : ITrainingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMLServiceClient _mlServiceClient;
    private readonly IDatasetService _datasetService;
    private readonly ILogger<TrainingService> _logger;

    public TrainingService(
        ApplicationDbContext context,
        IMLServiceClient mlServiceClient,
        IDatasetService datasetService,
        ILogger<TrainingService> logger)
    {
        _context = context;
        _mlServiceClient = mlServiceClient;
        _datasetService = datasetService;
        _logger = logger;
    }

    public async Task<Guid> StartTrainingAsync(TrainingConfigDto config)
    {
        try
        {
            // Get dataset
            var dataset = await _context.Datasets.FindAsync(config.DatasetId);
            if (dataset == null)
            {
                throw new ArgumentException("Dataset not found");
            }

            // Create training job
            var trainingJob = new TrainingJob
            {
                Id = Guid.NewGuid(),
                DatasetId = config.DatasetId,
                Algorithm = config.Algorithm,
                HyperparametersJson = JsonSerializer.Serialize(config.Hyperparameters),
                ValidationSplit = config.ValidationSplit,
                Status = TrainingStatus.Queued,
                Progress = 0,
                StartedAt = DateTime.UtcNow
            };

            _context.TrainingJobs.Add(trainingJob);
            await _context.SaveChangesAsync();

            // Start training in background
            _ = Task.Run(async () => await RunTrainingAsync(trainingJob, dataset));

            return trainingJob.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training");
            throw;
        }
    }

    private async Task RunTrainingAsync(TrainingJob trainingJob, Dataset dataset)
    {
        try
        {
            // Update status to training
            trainingJob.Status = TrainingStatus.Training;
            trainingJob.Progress = 10;
            await _context.SaveChangesAsync();

            // Prepare ML service request
            var mlRequest = new MLTrainingRequest
            {
                DatasetPath = dataset.FilePath,
                Algorithm = trainingJob.Algorithm,
                Hyperparameters = JsonSerializer.Deserialize<Dictionary<string, object>>(trainingJob.HyperparametersJson) ?? new(),
                ValidationSplit = trainingJob.ValidationSplit,
                TrainingStart = dataset.EarliestTimestamp,
                TrainingEnd = dataset.LatestTimestamp
            };

            // Start training in ML service
            var mlResponse = await _mlServiceClient.StartTrainingAsync(mlRequest);

            // Poll for completion
            while (true)
            {
                await Task.Delay(5000); // Poll every 5 seconds

                var statusResponse = await _mlServiceClient.GetTrainingStatusAsync(mlResponse.JobId);

                // Update progress
                trainingJob.Progress = statusResponse.Progress;
                trainingJob.CurrentEpoch = statusResponse.CurrentEpoch;
                trainingJob.TotalEpochs = statusResponse.TotalEpochs;

                if (statusResponse.Metrics != null)
                {
                    var metrics = new TrainingMetrics
                    {
                        Accuracy = statusResponse.Metrics.Accuracy,
                        Precision = statusResponse.Metrics.Precision,
                        Recall = statusResponse.Metrics.Recall,
                        F1Score = statusResponse.Metrics.F1Score,
                        Auc = statusResponse.Metrics.Auc,
                        ConfusionMatrix = statusResponse.Metrics.ConfusionMatrix,
                        TrainingLoss = statusResponse.Metrics.TrainingLoss,
                        ValidationLoss = statusResponse.Metrics.ValidationLoss
                    };
                    trainingJob.MetricsJson = JsonSerializer.Serialize(metrics);
                }

                if (statusResponse.Status == "completed")
                {
                    trainingJob.Status = TrainingStatus.Completed;
                    trainingJob.Progress = 100;
                    trainingJob.CompletedAt = DateTime.UtcNow;
                    trainingJob.ModelPath = $"models/{trainingJob.Id}";
                    break;
                }
                else if (statusResponse.Status == "failed")
                {
                    trainingJob.Status = TrainingStatus.Failed;
                    trainingJob.ErrorMessage = statusResponse.Error;
                    break;
                }

                await _context.SaveChangesAsync();
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running training for job {JobId}", trainingJob.Id);
            
            trainingJob.Status = TrainingStatus.Failed;
            trainingJob.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<TrainingStatusDto?> GetTrainingStatusAsync(Guid trainingId)
    {
        var trainingJob = await _context.TrainingJobs.FindAsync(trainingId);
        if (trainingJob == null) return null;

        TrainingMetricsDto? metrics = null;
        if (!string.IsNullOrEmpty(trainingJob.MetricsJson))
        {
            var metricsModel = JsonSerializer.Deserialize<TrainingMetrics>(trainingJob.MetricsJson);
            if (metricsModel != null)
            {
                metrics = new TrainingMetricsDto
                {
                    Accuracy = metricsModel.Accuracy,
                    Precision = metricsModel.Precision,
                    Recall = metricsModel.Recall,
                    F1Score = metricsModel.F1Score,
                    Auc = metricsModel.Auc,
                    ConfusionMatrix = metricsModel.ConfusionMatrix,
                    TrainingLoss = metricsModel.TrainingLoss,
                    ValidationLoss = metricsModel.ValidationLoss
                };
            }
        }

        return new TrainingStatusDto
        {
            Id = trainingJob.Id,
            Status = trainingJob.Status.ToString().ToLower(),
            Progress = trainingJob.Progress,
            CurrentEpoch = trainingJob.CurrentEpoch,
            TotalEpochs = trainingJob.TotalEpochs,
            Metrics = metrics,
            StartedAt = trainingJob.StartedAt,
            CompletedAt = trainingJob.CompletedAt,
            Error = trainingJob.ErrorMessage
        };
    }

    public async Task<ModelInfoDto?> GetModelAsync(Guid modelId)
    {
        var trainingJob = await _context.TrainingJobs.FindAsync(modelId);
        if (trainingJob == null || trainingJob.Status != TrainingStatus.Completed) return null;

        TrainingMetricsDto metrics = new();
        if (!string.IsNullOrEmpty(trainingJob.MetricsJson))
        {
            var metricsModel = JsonSerializer.Deserialize<TrainingMetrics>(trainingJob.MetricsJson);
            if (metricsModel != null)
            {
                metrics = new TrainingMetricsDto
                {
                    Accuracy = metricsModel.Accuracy,
                    Precision = metricsModel.Precision,
                    Recall = metricsModel.Recall,
                    F1Score = metricsModel.F1Score,
                    Auc = metricsModel.Auc,
                    ConfusionMatrix = metricsModel.ConfusionMatrix,
                    TrainingLoss = metricsModel.TrainingLoss,
                    ValidationLoss = metricsModel.ValidationLoss
                };
            }
        }

        var hyperparameters = JsonSerializer.Deserialize<Dictionary<string, object>>(trainingJob.HyperparametersJson) ?? new();

        return new ModelInfoDto
        {
            Id = trainingJob.Id,
            Algorithm = trainingJob.Algorithm,
            TrainedAt = trainingJob.CompletedAt ?? trainingJob.StartedAt,
            Metrics = metrics,
            Hyperparameters = hyperparameters,
            Status = "ready"
        };
    }

    public async Task UpdateTrainingStatusAsync(Guid trainingId, string status, int progress, TrainingMetricsDto? metrics = null)
    {
        var trainingJob = await _context.TrainingJobs.FindAsync(trainingId);
        if (trainingJob == null) return;

        if (Enum.TryParse<TrainingStatus>(status, true, out var statusEnum))
        {
            trainingJob.Status = statusEnum;
        }

        trainingJob.Progress = progress;

        if (metrics != null)
        {
            var metricsModel = new TrainingMetrics
            {
                Accuracy = metrics.Accuracy,
                Precision = metrics.Precision,
                Recall = metrics.Recall,
                F1Score = metrics.F1Score,
                Auc = metrics.Auc,
                ConfusionMatrix = metrics.ConfusionMatrix,
                TrainingLoss = metrics.TrainingLoss,
                ValidationLoss = metrics.ValidationLoss
            };
            trainingJob.MetricsJson = JsonSerializer.Serialize(metricsModel);
        }

        if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            trainingJob.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
