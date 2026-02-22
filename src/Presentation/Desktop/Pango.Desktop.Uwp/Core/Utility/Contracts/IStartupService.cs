using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Core.Utility.Contracts;

/// <summary>
/// Interface for managing the application startup state
/// </summary>
public interface IStartupService
{
    Task<bool> IsStartupEnabledAsync();

    Task<bool> EnableStartupAsync();

    Task DisableStartupAsync();
}