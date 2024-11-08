using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UpdateManager.Database.Context;
using UpdateManager.Database.Entities;
using UpdateManager.Models;

namespace UpdateManager.Services;

public class UpdatesService
{
    private readonly ManagerContext _context;

    public UpdatesService(ManagerContext context)
    {
        _context = context;
    }

    public async Task<string> GetUpdatePreferences()
    {
        string? updatePreferences = await _context.SystemConfigurations.Where(x => x.Key == "UpdatePreferences").Select(x => x.Value).FirstOrDefaultAsync();
        if(updatePreferences is null) throw new Exception("Update Preferences Not Found.");
        return updatePreferences;
    }

    public async Task SetUpdatePreferences(UpdatePreferenceModel preferenceModel)
    {
        var configuration = await _context.SystemConfigurations.FirstOrDefaultAsync(x => x.Key == "UpdatePreferences");

        if (configuration is null)
        {
            configuration = new SystemConfiguration()
            {
                Key = "UpdatePreferences",
                Value = JsonSerializer.Serialize(preferenceModel)
            };
            await _context.SystemConfigurations.AddAsync(configuration);
        }
        else
        {
            configuration.Value = JsonSerializer.Serialize(preferenceModel);
        }

        await _context.SaveChangesAsync();
    }
}