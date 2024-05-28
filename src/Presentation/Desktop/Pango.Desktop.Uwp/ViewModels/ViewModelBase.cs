using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.ViewModels;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    private bool _isBusy;

	public ViewModelBase(ILogger logger)
	{
        this.Logger = logger;
        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
        ViewResourceLoader = ResourceLoader.GetForCurrentView();
    }


    #region Properties

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    protected ResourceLoader ViewResourceLoader { get; }

    protected AppView? View => this.GetType().GetCustomAttribute<AppViewAttribute>()?.View;

    protected ILogger Logger { get; }   
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

    private async void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if (message.Value.NavigatedView == View)
        {
            await OnNavigatedToAsync(message.Value);
        }
    }

    public virtual Task OnNavigatedToAsync(object parameter)
    {
        Debug.WriteLine($"Navigated to {this.GetType().Name}");

        return Task.CompletedTask;
    }

    #endregion
}
