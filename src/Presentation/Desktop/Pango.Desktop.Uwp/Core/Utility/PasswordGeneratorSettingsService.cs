using System.Text.Json;
using Windows.Storage;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Microsoft.Extensions.Logging;

namespace Pango.Infrastructure.Services;

public class PasswordGeneratorSettingsService : IPasswordGeneratorSettingsService
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    private readonly IUserContextProvider _userContextProvider;
    private readonly ILogger _logger;
    public PasswordGeneratorSettingsService(IUserContextProvider userContextProvider, ILogger logger)
    {
        _userContextProvider = userContextProvider;
        _logger = logger;
    }

    private string BuildKey()
    {
        string userId = _userContextProvider.GetUserName();
        return $"PasswordGeneratorSettings_{userId}";
    }

    public PasswordGeneratorSettings Load()
    {
        string key = BuildKey();
        if (_localSettings.Values[key] is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<PasswordGeneratorSettings>(json) ?? new PasswordGeneratorSettings();
            }
            catch
            {
                _logger.LogError("An error occurred while loading password generator settings.");
            }
        }
        return new PasswordGeneratorSettings();
    }

    public void Save(PasswordGeneratorSettings settings)
    {
        string key = BuildKey() ;
        string json = JsonSerializer.Serialize(settings);

        _localSettings.Values[key] = json;
    }
}

