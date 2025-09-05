using Microsoft.AspNetCore.Mvc;
using Intellinspect.Backend.Services;
using Intellinspect.Backend.DTOs;

namespace Intellinspect.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IDatasetService _datasetService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IDatasetService datasetService, ILogger<UploadController> logger)
    {
        _datasetService = datasetService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<UploadResponseDto>> UploadDataset(IFormFile file)
    {
        try
        {
            var result = await _datasetService.UploadDatasetAsync(file);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
            return StatusCode(500, new UploadResponseDto 
            { 
                Success = false, 
                Message = "Internal server error" 
            });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class DatasetsController : ControllerBase
{
    private readonly IDatasetService _datasetService;
    private readonly ILogger<DatasetsController> _logger;

    public DatasetsController(IDatasetService datasetService, ILogger<DatasetsController> logger)
    {
        _datasetService = datasetService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DatasetDto>> GetDataset(Guid id)
    {
        try
        {
            var dataset = await _datasetService.GetDatasetAsync(id);
            
            if (dataset == null)
            {
                return NotFound();
            }

            return Ok(dataset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dataset {DatasetId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class DaterangesController : ControllerBase
{
    private readonly IDatasetService _datasetService;
    private readonly ILogger<DaterangesController> _logger;

    public DaterangesController(IDatasetService datasetService, ILogger<DaterangesController> logger)
    {
        _datasetService = datasetService;
        _logger = logger;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<DateRangeValidationResponseDto>> ValidateDateRanges(DateRangeValidationRequestDto request)
    {
        try
        {
            var result = await _datasetService.ValidateDateRangesAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating date ranges");
            return StatusCode(500, "Internal server error");
        }
    }
}
