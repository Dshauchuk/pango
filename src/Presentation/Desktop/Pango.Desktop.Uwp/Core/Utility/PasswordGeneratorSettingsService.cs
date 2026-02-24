using System.Text.Json;
using Windows.Storage;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;

namespace Pango.Infrastructure.Services;

public class PasswordGeneratorSettingsService : IPasswordGeneratorSettingsService
{
    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    private readonly IUserContextProvider _userContextProvider;
    public PasswordGeneratorSettingsService(IUserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
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

