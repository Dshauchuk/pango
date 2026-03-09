using Pango.Application.Models;

namespace Pango.Application.Common.Interfaces.Services;

public interface IPasswordGeneratorSettingsService
{
    PasswordGeneratorSettings Load();
    void Save(PasswordGeneratorSettings settings);
}
