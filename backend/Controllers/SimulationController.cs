using Microsoft.AspNetCore.Mvc;
using Intellinspect.Backend.Services;
using Intellinspect.Backend.DTOs;

namespace Intellinspect.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ISimulationService _simulationService;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(ISimulationService simulationService, ILogger<SimulationController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<ActionResult> StartSimulation(SimulationConfigDto config)
    {
        try
        {
            var simulationId = await _simulationService.StartSimulationAsync(config);
            return Ok(new { simulationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting simulation");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status/{simulationId}")]
    public async Task<ActionResult<SimulationStatusDto>> GetSimulationStatus(Guid simulationId)
    {
        try
        {
            var status = await _simulationService.GetSimulationStatusAsync(simulationId);
            
            if (status == null)
            {
                return NotFound();
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting simulation status for {SimulationId}", simulationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("predictions/{simulationId}")]
    public async Task<ActionResult<List<PredictionResultDto>>> GetPredictions(Guid simulationId, int limit = 100)
    {
        try
        {
            var predictions = await _simulationService.GetPredictionsAsync(simulationId, limit);
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for simulation {SimulationId}", simulationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("pause/{simulationId}")]
    public async Task<ActionResult> PauseSimulation(Guid simulationId)
    {
        try
        {
            await _simulationService.PauseSimulationAsync(simulationId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing simulation {SimulationId}", simulationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("resume/{simulationId}")]
    public async Task<ActionResult> ResumeSimulation(Guid simulationId)
    {
        try
        {
            await _simulationService.ResumeSimulationAsync(simulationId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming simulation {SimulationId}", simulationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("stop/{simulationId}")]
    public async Task<ActionResult> StopSimulation(Guid simulationId)
    {
        try
        {
            await _simulationService.StopSimulationAsync(simulationId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping simulation {SimulationId}", simulationId);
            return StatusCode(500, "Internal server error");
        }
    }
}
