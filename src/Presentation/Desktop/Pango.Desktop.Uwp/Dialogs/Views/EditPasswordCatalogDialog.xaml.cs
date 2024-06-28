using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using Windows.ApplicationModel.Resources;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditPasswordCatalogDialog : Page, IContentDialog
{
    private readonly ResourceLoader _viewResourceLoader;

    public EditPasswordCatalogDialog(EditCatalogParameters editCatalogParameters)
    {
        this.InitializeComponent();

        _viewResourceLoader = new ResourceLoader();
        DataContext = App.Host.Services.GetRequiredService<EditPasswordCatalogDialogViewModel>();

        ((EditPasswordCatalogDialogViewModel)DataContext).Initialize(editCatalogParameters);

        Title = ((EditPasswordCatalogDialogViewModel)DataContext).IsNew ? 
            _viewResourceLoader.GetString("NewCatalogDialogTitle") : 
            _viewResourceLoader.GetString("EditCatalogDialogTitle");
    }

    public void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        NewCatalogNameTextBlock.Focus(FocusState.Programmatic);
    }

    public string Title { get; private set; }

    public IDialogViewModel? ViewModel => DataContext as EditPasswordCatalogDialogViewModel;
}
