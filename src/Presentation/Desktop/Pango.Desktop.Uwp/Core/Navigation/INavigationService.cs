using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Application.Common.Interfaces.Services;

public interface INavigationService
{
    void Navigate(AppView view, object parameter = null);
    bool CanGoBack { get; }
    void GoBack();
}