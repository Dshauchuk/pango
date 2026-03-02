using Microsoft.Extensions.DependencyInjection;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

namespace Pango.Desktop.Uwp.Dialogs.Views;

public sealed partial class GeneratePasswordDialog : DialogPage
{
    public GeneratePasswordDialog(GeneratePasswordDialogParameters parameter) : base(parameter)
    {
        InitializeComponent();
        this.SetViewModel(App.Host.Services.GetRequiredService<GeneratePasswordDialogViewModel>());
    }
    public override string Title => ViewResourceLoader.GetString("GeneratePassword_Title");

}

