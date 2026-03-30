using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using System;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// Represents the view model for the Home view.
/// </summary>
[AppView(AppView.Home)]
public sealed partial class HomeViewModel : ObservableRecipient, IViewModel
{
    /// <summary>
    /// Gets the command used to navigate to different application views.
    /// </summary>
    public RelayCommand<string> NavigateCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    public HomeViewModel()
    {
        NavigateCommand = new RelayCommand<string>(ExecuteNavigation);
    }

    /// <summary>
    /// Executes the navigation based on the provided view name parameter.
    /// </summary>
    /// <param name="viewName">The name of the view to navigate to (must match <see cref="AppView"/> enum).</param>
    private void ExecuteNavigation(string? viewName)
    {
        if (Enum.TryParse<AppView>(viewName, out var targetView))
        {
            WeakReferenceMessenger.Default.Send(
                new NavigationRequstedMessage(
                    new NavigationParameters(targetView, AppView.Home)));
        }
    }

    /// <summary>
    /// Called when the view is navigated to.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>A completed task.</returns>
    public Task OnNavigatedToAsync(object? parameter)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the view is navigated away from.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    /// <returns>A completed task.</returns>
    public Task OnNavigatedFromAsync(object? parameter)
    {
        return Task.CompletedTask;
    }
}
