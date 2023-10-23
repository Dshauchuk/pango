using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
	public ViewModelBase()
	{
        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
    }

    private async void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if(message.Value.NavigatedView == View)
        {
            await OnNavigatedToAsync(message.Value);
        }
    }

    protected AppView View => this.GetType().GetCustomAttribute<AppViewAttribute>().View;

    public bool NavigationProcessed => throw new System.NotImplementedException();

    public virtual Task OnNavigatedToAsync(object parameter)
    {
        Debug.WriteLine($"Navigated to {this.GetType().Name}");

        return Task.CompletedTask;
    }
}
