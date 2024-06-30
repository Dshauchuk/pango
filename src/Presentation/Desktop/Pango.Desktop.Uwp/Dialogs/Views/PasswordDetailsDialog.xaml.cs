using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Dialogs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PasswordDetailsDialog : Page, IContentDialog
    {
        private readonly ResourceLoader _viewResourceLoader;
        private PasswordDetailsParameters _parameters;

        public PasswordDetailsDialog(PasswordDetailsParameters parameters)
        {
            this.InitializeComponent();
            DataContext = App.Host.Services.GetRequiredService<PasswordDetailsDialogViewModel>();

            _parameters = parameters;
            _viewResourceLoader = new ResourceLoader();

            Title = _viewResourceLoader.GetString("PasswordDetails");
            PrimaryButtonText = _viewResourceLoader.GetString("EditPassword");
            CancelButtonText = _viewResourceLoader.GetString("Ok");
        }

        public string PrimaryButtonText { get; private set; }
        public string CancelButtonText { get; private set; }
        public string Title { get; private set; }

        public IDialogViewModel? ViewModel => DataContext as PasswordDetailsDialogViewModel;

        public void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {

        }

        public object? GetDialogParameter() => _parameters;
    }
}
