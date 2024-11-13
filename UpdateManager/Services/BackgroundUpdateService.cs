using System.Text.Json;
using UpdateManager.Database.Context;
using UpdateManager.Models;

namespace UpdateManager.Services;

public class BackgroundUpdateService : BackgroundService
{
    private readonly ILogger<BackgroundUpdateService> _logger;
    private readonly UpdatesService _updatesService;

    public BackgroundUpdateService(ILogger<BackgroundUpdateService> logger, UpdatesService updatesService)
    {
        _logger = logger;
        _updatesService = updatesService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UpdatePreferenceValue? preferenceModel = JsonSerializer.Deserialize<UpdatePreferenceValue>(await _updatesService.GetUpdatePreferences());
                if(preferenceModel is null) throw new Exception("Update Preferences Not Found.");

                if (!preferenceModel.AutoUpdate && await _updatesService.IsUpdateAvailable()) await _updatesService.StartUpdate();

                Thread.Sleep(5 * 60 * 1000); //5 minutes
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating in background task");
            }
        }
    }
}