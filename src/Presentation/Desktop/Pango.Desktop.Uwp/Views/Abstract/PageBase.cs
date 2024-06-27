using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.ViewModels;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class PageBase : Page
{
    public PageBase(ILogger logger)
    {
        Logger = logger;
    }

    public IViewModel ViewModel => (IViewModel)DataContext;

    protected ILogger Logger { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        RegisterMessages();

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        UnregisterMessages();

        base.OnNavigatedFrom(e);
    }

    protected virtual void RegisterMessages()
    {

    }

    protected virtual void UnregisterMessages()
    {

    }

    ~PageBase() 
    { 
        
    }
}
