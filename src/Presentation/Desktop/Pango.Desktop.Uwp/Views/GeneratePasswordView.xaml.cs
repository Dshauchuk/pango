using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [AppView(Core.Enums.AppView.GeneratePassword)]
    public sealed partial class GeneratePasswordView : PageBase
    {
        public GeneratePasswordView()
            : base(App.Host.Services.GetRequiredService<ILogger<GeneratePasswordView>>())
        {
            InitializeComponent();

            // Temporarily - just take the VM from DI
            DataContext = App.Host.Services.GetRequiredService<GeneratePasswordViewModel>();
        }
    }
}
