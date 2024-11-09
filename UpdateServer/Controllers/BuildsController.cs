using Microsoft.AspNetCore.Mvc;
using UpdateServer.Services;

namespace UpdateServer.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class BuildsController : ControllerBase
{
    private readonly BuildsService _buildsService;

    public BuildsController(BuildsService buildsService)
    {
        _buildsService = buildsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetChangelog(string buildNumber)
    {
        try
        {
            return Ok(await _buildsService.GetChangelog(buildNumber));
        }
        catch(Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public IActionResult GetBuilds()
    {
        return Ok(_buildsService.GetBuilds());
    }

    [HttpGet]
    public async Task<IActionResult> GetBuild(string buildNumber)
    {
        try
        {
            return File(await _buildsService.GetBuild(buildNumber), "application/octet-stream");
        }
        catch(Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public IActionResult GetLatestBuildNumber()
    {
        try
        {
            return Ok(_buildsService.GetLatestBuildNumber());
        }
        catch(Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}