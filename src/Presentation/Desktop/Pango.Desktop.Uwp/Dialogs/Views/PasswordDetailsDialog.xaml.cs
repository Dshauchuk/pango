using Microsoft.Extensions.DependencyInjection;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PasswordDetailsDialog : DialogPage
{
    public PasswordDetailsDialog(PasswordDetailsParameters parameters) : base(parameters)
    {
        this.InitializeComponent();

        this.SetViewModel(App.Host.Services.GetRequiredService<PasswordDetailsDialogViewModel>());
    }

    public override string Title => ViewResourceLoader.GetString("PasswordDetails");
    public override string? PrimaryButtonText => ViewResourceLoader.GetString("EditPassword");
    public override string? CancelButtonText => ViewResourceLoader.GetString("Ok");
}
