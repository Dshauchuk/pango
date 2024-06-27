using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.ViewModels;

public abstract class ViewModelBase(ILogger logger) : ObservableObject, IViewModel
{
    private bool _isBusy;

    #region Properties

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    protected ResourceLoader ViewResourceLoader { get; } = new();// new ResourceLoader();

    protected AppView? View => this.GetType().GetCustomAttribute<AppViewAttribute>()?.View;

    protected ILogger Logger { get; } = logger;
    #endregion

    #region Methods

    protected async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
    {
        ContentDialog subscribeDialog = new()
        {
            Title = confirmationTitle,
            Content = confirmationText,
            CloseButtonText = ViewResourceLoader.GetString("Cancel"),
            PrimaryButtonText = ViewResourceLoader.GetString("Yes"),
            DefaultButton = ContentDialogButton.Primary
        };

        ContentDialogResult result = await subscribeDialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    protected async Task DoAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            // TODO: handle the error
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual Task OnNavigatedToAsync(object? parameter)
    {     
        RegisterMessages();

        return Task.CompletedTask;
    }

    public virtual Task OnNavigatedFromAsync(object? parameter)
    {
        UnregisterMessages();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans all subscriptions and then adds new registrations. Makes sure this method is called before adding new registrations
    /// </summary>
    protected virtual void RegisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    protected virtual void UnregisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    #endregion
}
