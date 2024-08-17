using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditPasswordCatalogDialog : DialogPage
{
    public EditPasswordCatalogDialog(EditCatalogParameters editCatalogParameters) 
        : base(editCatalogParameters)
    {
        this.InitializeComponent();
        this.SetViewModel(App.Host.Services.GetRequiredService<EditPasswordCatalogDialogViewModel>());

        ((EditPasswordCatalogDialogViewModel)ViewModel!).Initialize(editCatalogParameters);
    }

    public override void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        NewCatalogNameTextBlock.Focus(FocusState.Programmatic);
    }

    public override string Title => ((EditPasswordCatalogDialogViewModel)DataContext).IsNew ? ViewResourceLoader.GetString("NewCatalogDialogTitle") : ViewResourceLoader.GetString("EditCatalogDialogTitle");
}
