using Microsoft.Extensions.DependencyInjection;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ChangePasswordDialog : DialogPage
{
    public ChangePasswordDialog(EmptyDialogParameter parameter) : base(parameter)
    {
        this.InitializeComponent();
        this.SetViewModel(App.Host.Services.GetRequiredService<ChangePasswordDialogViewModel>());
    }

    public override string Title => ViewResourceLoader.GetString("ChangePasswordDialogTitle");

    public override string? PrimaryButtonText => ViewResourceLoader.GetString("ChangePassword_Label");
}
