using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NewPasswordCatalogDialog : Page, IContentDialog
{
    private readonly ResourceLoader _viewResourceLoader;

    public NewPasswordCatalogDialog(List<string> availableCatalogs, string defaultCatalog)
    {
        this.InitializeComponent();

        _viewResourceLoader = ResourceLoader.GetForCurrentView();
        DataContext = Ioc.Default.GetRequiredService<NewPasswordCatalogDialogViewModel>();

        ((NewPasswordCatalogDialogViewModel)DataContext).Initialize(availableCatalogs, defaultCatalog);
    }

    public string Title => _viewResourceLoader.GetString("NewCatalogDialogTitle");

    public IDialogViewModel ViewModel => DataContext as NewPasswordCatalogDialogViewModel;
}
