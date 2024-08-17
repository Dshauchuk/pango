using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs.Views;

public abstract class DialogPage: Page, IContentDialog 
{
    public DialogPage(IDialogParameter parameter)
    {
        DialogParameter = parameter;

        ViewResourceLoader = new ResourceLoader();

        PrimaryButtonText = ViewResourceLoader.GetString("Ok");
        CancelButtonText = ViewResourceLoader.GetString("Cancel");
    }

    protected ResourceLoader ViewResourceLoader { get; }

    protected IDialogParameter? DialogParameter { get; set; }

    /// <summary>
    /// The dialog window title
    /// </summary>
    public abstract string Title { get; }
    
    /// <summary>
    /// The dialog window view model
    /// </summary>
    public virtual IDialogViewModel? ViewModel { get; private set; }
    
    /// <summary>
    /// The primary button text content
    /// </summary>
    public virtual string? PrimaryButtonText { get; }
    
    /// <summary>
    /// The cancel button text content
    /// </summary>
    public virtual string? CancelButtonText { get; }

    /// <summary>
    /// Returns the value of the parameter passed into the dialog
    /// </summary>
    /// <returns></returns>
    public virtual object? GetDialogParameter()
    {
        return DialogParameter;
    }

    protected void SetViewModel(IDialogViewModel viewModel)
    {
        DataContext = viewModel;
        ViewModel = viewModel;
    }

    public virtual void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {

    }
}
