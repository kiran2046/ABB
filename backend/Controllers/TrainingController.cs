using Microsoft.AspNetCore.Mvc;
using Intellinspect.Backend.Services;
using Intellinspect.Backend.DTOs;

namespace Intellinspect.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(ITrainingService trainingService, ILogger<TrainingController> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<ActionResult> StartTraining(TrainingConfigDto config)
    {
        try
        {
            var trainingId = await _trainingService.StartTrainingAsync(config);
            return Ok(new { trainingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status/{trainingId}")]
    public async Task<ActionResult<TrainingStatusDto>> GetTrainingStatus(Guid trainingId)
    {
        try
        {
            var status = await _trainingService.GetTrainingStatusAsync(trainingId);
            
            if (status == null)
            {
                return NotFound();
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training status for {TrainingId}", trainingId);
            return StatusCode(500, "Internal server error");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(ITrainingService trainingService, ILogger<ModelsController> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    [HttpGet("{modelId}")]
    public async Task<ActionResult<ModelInfoDto>> GetModel(Guid modelId)
    {
        try
        {
            var model = await _trainingService.GetModelAsync(modelId);
            
            if (model == null)
            {
                return NotFound();
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model {ModelId}", modelId);
            return StatusCode(500, "Internal server error");
        }
    }
}
