using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UpdateManager.Models;
using UpdateManager.Services;

namespace UpdateManager.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class UpdatesController : ControllerBase
{
    private readonly UpdatesService _updatesService;

    public UpdatesController(UpdatesService updatesService)
    {
        _updatesService = updatesService;
    }

    [HttpPost]
    public async Task<IActionResult> SetUpdatePreferences(UpdatePreferenceModel preferenceModel)
    {
        try
        {
            await _updatesService.SetUpdatePreferences(preferenceModel);
            return Ok("Update Preferences Set.");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUpdatePreferences()
    {
        try
        {
            string preferences = await _updatesService.GetUpdatePreferences();
            return Ok(JsonSerializer.Deserialize<UpdatePreferenceModel>(preferences));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBuilds()
    {
        try
        {
            return Ok(await _updatesService.GetBuilds());
        } catch (Exception e)
        {
            return BadRequest(e.Message);
        }

    }

    [HttpGet]
    public async Task<IActionResult> GetChangelog(string? buildNumber)
    {
        try
        {
            if(buildNumber is null) return Ok(await _updatesService.GetChangelog());
            return Ok(await _updatesService.GetChangelog(buildNumber));
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> IsUpdateAvailable()
    {
        try
        {
            return Ok(await _updatesService.IsUpdateAvailable());
        } catch (Exception e)
        {
            return BadRequest(e.Message);
        }

    }
    
    [HttpPost]
    public async Task<IActionResult> StartUpdate(string buildNumber)
    {
        try
        {
            await _updatesService.StartUpdate(buildNumber);
            return Ok("Update Started.");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public IActionResult GetUpdateStatus()
    {
        return Ok(_updatesService.GetStatus());
    }
}