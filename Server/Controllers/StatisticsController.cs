using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Server.Services;
using SharedModels.QueryParameters.Statistics;

namespace Server.Controllers;

[Route("api/[controller]")] 
[ApiController] 
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }
    
    [HttpGet("routes")]
    public async Task<IActionResult> GetPopularRoutes([FromQuery] PopularRoutesParameters parameters)
    {
        var result = await _statisticsService.GetPopularRoutes(parameters);

        if (!result.IsSucceed)
        {
            return BadRequest(result.actionResult);
        }
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(result.pagingMetadata));

        return Ok(result.route);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetEngagedUsers([FromQuery] EngagedUserParameters parameters)

    {
        var result = await _statisticsService.GetEngagedUsers(parameters);

        if (!result.IsSucceed)
        {
            return BadRequest(result.actionResult);
        }
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(result.pagingMetadata));

        return Ok(result.users);
    }
    
    [HttpGet("companies")]
    public async Task<IActionResult> GetPopularCompanies([FromQuery] PopularCompanyParameters parameters)
    {
        var result = await _statisticsService.GetPopularCompanies(parameters);

        if (!result.IsSucceed)
        {
            return BadRequest(result.actionResult);
        }
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(result.pagingMetadata));

        return Ok(result.companies);
    }
    
    [HttpGet("stations")]
    public async Task<IActionResult> GetPopularStations([FromQuery] PopularAddressesParameters parameters)
    {
        var result = await _statisticsService.GetPopularStations(parameters);

        if (!result.IsSucceed)
        {
            return BadRequest(result.actionResult);
        }
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(result.pagingMetadata));

        return Ok(result.stations);
    }
}

