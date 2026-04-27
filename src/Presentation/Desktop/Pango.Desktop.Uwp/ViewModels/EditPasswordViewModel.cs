using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels.Validators;
using Serilog;
using System.Globalization;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// View model for creating and editing password entries with validation and navigation support.
/// </summary>
[AppView(AppView.EditPassword)]
public partial class EditPasswordViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IDialogService _dialogService;
    private bool _isNew;
    private List<string>? _availableCatalogs;
    private EditPasswordValidator? _passwordValidator;
    private string _title = string.Empty;
    private AppView _sourceView = AppView.PasswordsIndex;
    private bool _isSaving;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="EditPasswordViewModel"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    /// <param name="logger">Logger instance for this view model.</param>
    /// <param name="dialogService">Service for showing modal dialogs.</param>
    public EditPasswordViewModel(
        ISender sender,
        ILogger<EditPasswordViewModel> logger,
        IDialogService dialogService)
        : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
        SavePasswordCommand = new AsyncRelayCommand(OnSavePasswordAsync);
        OpenGeneratePasswordDialogCommand = new RelayCommand(OnOpenGeneratePasswordDialogAsync);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the view title text displayed in the UI.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Gets or sets the password validator instance for form validation.
    /// </summary>
    public EditPasswordValidator? PasswordValidator
    {
        get => _passwordValidator;
        set => SetProperty(ref _passwordValidator, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this is a new password creation or edit operation.
    /// </summary>
    public bool IsNew
    {
        get => _isNew;
        set
        {
            if (SetProperty(ref _isNew, value))
            {
                Title = value ? ViewResourceLoader.GetString("NewPassword") : ViewResourceLoader.GetString("EditPassword");
            }
        }
    }

    /// <summary>
    /// Gets or sets the list of available catalog paths for password organization.
    /// </summary>
    public List<string>? AvailableCatalogs
    {
        get => _availableCatalogs;
        set
        {
            if (SetProperty(ref _availableCatalogs, value))
            {
                OnPropertyChanged(nameof(HasCatalogs));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether any catalogs are available for selection.
    /// </summary>
    public bool HasCatalogs => AvailableCatalogs != null && AvailableCatalogs.Count != 0;

    #endregion

    #region Commands

    /// <summary>
    /// Command for navigating back to the source index view.
    /// </summary>
    public RelayCommand OpenIndexViewCommand { get; }

    /// <summary>
    /// Command for saving the password entry asynchronously.
    /// </summary>
    public IAsyncRelayCommand SavePasswordCommand { get; }

    /// <summary>
    /// Command for opening the password generation dialog.
    /// </summary>
    public RelayCommand OpenGeneratePasswordDialogCommand { get; }

    #endregion

    #region Overrides

    /// <summary>
    /// Called when the view is navigated to: initializes form state based on navigation parameters.
    /// </summary>
    /// <param name="parameter">Optional navigation parameters containing edit context.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        Clear();

        var navParams = parameter as NavigationParameters;
        if (navParams != null)
        {
            _sourceView = navParams.SourceView;
        }

        bool isReturningToExistingEdit = navParams?.SourceView == AppView.GeneratePassword && navParams.Parameter == null;

        if (!isReturningToExistingEdit)
        {
            Clear();

            if (navParams?.Parameter is EditPasswordParameters parameters)
            {
                IsNew = parameters.IsNew;
                AvailableCatalogs = parameters.AvailableCatalogs;

                if (AvailableCatalogs == null || AvailableCatalogs.Count == 0)
                {
                    var queryResult = await _sender.Send(
                        new Application.UseCases.Password.Queries.UserPasswords.UserPasswordsQuery());

                    List<string> catalogs = [string.Empty];

                    if (!queryResult.IsError)
                    {
                        var existingCatalogs = queryResult.Value
                            .Where(p => p.IsCatalog)
                            .Select(p => PasswordPathUtility.BuildCatalogPath(p.CatalogPath, p.Name))
                            .OrderBy(p => p)
                            .ToList();

                        catalogs.AddRange(existingCatalogs);
                    }
                    AvailableCatalogs = catalogs;
                }

                if (!IsNew && parameters.SelectedPasswordId != null)
                {
                    Guid passwordId = parameters.SelectedPasswordId.Value;
                    var passwordResult = await _sender.Send(new FindUserPasswordQuery(passwordId));

                    if (!passwordResult.IsError)
                    {
                        PasswordValidator!.Id = passwordId;
                        PasswordValidator.Login = passwordResult.Value.Login;
                        PasswordValidator.Title = passwordResult.Value.Name;
                        PasswordValidator.Password = passwordResult.Value.Value;
                        PasswordValidator.SelectedCatalog = passwordResult.Value.CatalogPath;
                        PasswordValidator.Star = passwordResult.Value.Star;

                        if (passwordResult.Value.Properties.TryGetValue(PasswordProperties.Notes, out string? notes))
                        {
                            PasswordValidator.Notes = notes;
                        }

                        if (passwordResult.Value.Properties.TryGetValue(PasswordProperties.ExpirationDate, out string? expDateStr) &&
                            DateTimeOffset.TryParse(expDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expDate))
                        {
                            PasswordValidator.ExpirationDate = expDate;
                            PasswordValidator.HasExpirationDate = true;
                        }
                    }
                }
                else
                {
                    PasswordValidator!.SelectedCatalog = parameters.Catalog ?? string.Empty;

                    if (!string.IsNullOrEmpty(parameters.GeneratedPassword))
                    {
                        PasswordValidator.Password = parameters.GeneratedPassword;
                    }
                }
            }
            else
            {
                IsNew = true;
            }
        }
    }

    public override async Task OnNavigatedFromAsync(object? parameter)
    {
        await base.OnNavigatedFromAsync(parameter);
    }

    /// <summary>
    /// Registers message subscriptions for password generation events.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<PasswordGeneratedForEditMessage>(this, OnPasswordGeneratedForEdit);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Resets the password validator to initial state or creates new instance.
    /// </summary>
    private void Clear()
    {
        PasswordValidator?.Dispose();
        PasswordValidator = new EditPasswordValidator();
        OnPropertyChanged(nameof(PasswordValidator));
    }

    /// <summary>
    /// Validates and saves the password entry asynchronously via MediatR command.
    /// </summary>
    private async Task OnSavePasswordAsync()
    {
        if (_isSaving) return;
        _isSaving = true;

        try
        {
            Log.Logger?.Information("OnSavePasswordAsync: validating and saving password");

            PasswordValidator!.Validate();

            if (!PasswordValidator.HasErrors)
            {
                var props = new Dictionary<string, string> { { PasswordProperties.Notes, PasswordValidator.Notes } };

                if (PasswordValidator.HasExpirationDate && PasswordValidator.ExpirationDate.HasValue)
                {
                    props.Add(
                        PasswordProperties.ExpirationDate,
                        PasswordValidator.ExpirationDate.Value.ToString("O", CultureInfo.InvariantCulture));
                }

                ErrorOr.ErrorOr<PangoPasswordDto> result;

                if (IsNew)
                {
                    result = await _sender.Send(
                        new NewPasswordCommand(
                            PasswordValidator.Title,
                            PasswordValidator.Login,
                            PasswordValidator.Password,
                            props)
                        {
                            CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                        });
                }
                else
                {
                    result = await _sender.Send(
                        new UpdatePasswordCommand(
                        PasswordValidator.Id!.Value,
                        PasswordValidator.Title,
                        PasswordValidator.Login,
                        PasswordValidator.Password,
                        PasswordValidator.Star,
                        props)
                        {
                            CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                        });
                }

                string message = result.IsError
                    ? ViewResourceLoader.GetString("CannotSavePassword")
                    : IsNew
                        ? string.Format(ViewResourceLoader.GetString("PasswordCreated"), PasswordValidator.Title)
                        : string.Format(ViewResourceLoader.GetString("PasswordModified"), PasswordValidator.Title);

                WeakReferenceMessenger.Default.Send(
                    new InAppNotificationMessage(
                        message,
                    result.IsError ? AppNotificationType.Error : AppNotificationType.Success));

                if (!result.IsError)
                {
                    Log.Logger?.Information("Password {Action} successfully: {Title}", IsNew ? "created" : "updated", PasswordValidator.Title);
                    var entity = result.Value.Adapt<PangoPasswordListItemDto>();

                    if (IsNew) WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
                    else WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(entity));

                    App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(
                        Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                        () =>
                        {
                            Clear();
                            WeakReferenceMessenger.Default.Send(new SwitchPasswordTabMessage(0));
                            WeakReferenceMessenger.Default.Send(new NavigationRequestedMessage(
                                new NavigationParameters(AppView.PasswordsIndex, AppView.EditPassword)));
                        });
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("ValidationError_Required") ?? "This field is required.", AppNotificationType.Warning));
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    /// <summary>
    /// Navigates back to the source index view via messenger.
    /// </summary>
    private void OnOpenIndexView()
    {
        Clear();

        if (_sourceView == AppView.GeneratePassword)
        {
            WeakReferenceMessenger.Default.Send(
                new NavigationRequestedMessage(
                    new NavigationParameters(AppView.GeneratePassword, AppView.EditPassword)));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new SwitchPasswordTabMessage(0));
        }
    }

    /// <summary>
    /// Opens the password generation dialog asynchronously.
    /// </summary>
    private async void OnOpenGeneratePasswordDialogAsync()
    {
        var parameters = new GeneratePasswordDialogParameters(
            password: PasswordValidator?.Password ?? string.Empty);

        await _dialogService.ShowGeneratePasswordDialogAsync(parameters);
    }

    /// <summary>
    /// Handles password generation message: inserts generated password into form.
    /// </summary>
    /// <param name="recipient">Message recipient instance.</param>
    /// <param name="message">Message containing the generated password value.</param>
    private void OnPasswordGeneratedForEdit(object recipient, PasswordGeneratedForEditMessage message)
    {
        if (PasswordValidator is null) return;

        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            PasswordValidator.Password = message.Value;
        });
    }

    #endregion
}
