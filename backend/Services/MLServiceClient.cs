using System.Text.Json;
using System.Text;

namespace Intellinspect.Backend.Services;

public interface IMLServiceClient
{
    Task<TrainingJobResponse> StartTrainingAsync(MLTrainingRequest request);
    Task<TrainingStatusResponse> GetTrainingStatusAsync(string jobId);
    Task<PredictionResponse> PredictAsync(PredictionRequest request);
    Task<ValidationResponse> ValidateDatasetAsync(ValidationRequest request);
}

public class MLServiceClient : IMLServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLServiceClient> _logger;

    public MLServiceClient(HttpClient httpClient, ILogger<MLServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TrainingJobResponse> StartTrainingAsync(MLTrainingRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/train", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TrainingJobResponse>(responseJson) ?? new TrainingJobResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting ML training");
            throw;
        }
    }

    public async Task<TrainingStatusResponse> GetTrainingStatusAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/training/status/{jobId}");
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TrainingStatusResponse>(responseJson) ?? new TrainingStatusResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training status for job {JobId}", jobId);
            throw;
        }
    }

    public async Task<PredictionResponse> PredictAsync(PredictionRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/predict", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PredictionResponse>(responseJson) ?? new PredictionResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making prediction");
            throw;
        }
    }

    public async Task<ValidationResponse> ValidateDatasetAsync(ValidationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/validate", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ValidationResponse>(responseJson) ?? new ValidationResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating dataset");
            throw;
        }
    }
}

// ML Service DTOs
public class MLTrainingRequest
{
    public string DatasetPath { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public Dictionary<string, object> Hyperparameters { get; set; } = new();
    public double ValidationSplit { get; set; }
    public DateTime TrainingStart { get; set; }
    public DateTime TrainingEnd { get; set; }
}

public class TrainingJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class TrainingStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int? CurrentEpoch { get; set; }
    public int? TotalEpochs { get; set; }
    public TrainingMetricsResponse? Metrics { get; set; }
    public string? Error { get; set; }
}

public class TrainingMetricsResponse
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

public class PredictionRequest
{
    public string ModelId { get; set; } = string.Empty;
    public Dictionary<string, double> Features { get; set; } = new();
}

public class PredictionResponse
{
    public int Prediction { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
}

public class ValidationRequest
{
    public string DatasetPath { get; set; } = string.Empty;
}

public class ValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public List<string> Columns { get; set; } = new();
}
