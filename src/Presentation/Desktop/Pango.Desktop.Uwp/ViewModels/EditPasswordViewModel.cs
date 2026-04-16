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

namespace Pango.Desktop.Uwp.ViewModels;

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

    #endregion

    public EditPasswordViewModel(ISender sender, ILogger<EditPasswordViewModel> logger, IDialogService dialogService) : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
        SavePasswordComand = new AsyncRelayCommand(OnSavePassword);
        OpenGeneratePasswordDialogCommand = new RelayCommand(OnOpenGeneratePasswordDialog);
    }

    #region Properties

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public EditPasswordValidator? PasswordValidator
    {
        get => _passwordValidator;
        set => SetProperty(ref _passwordValidator, value);
    }

    public bool IsNew
    {
        get => _isNew;
        set
        {
            SetProperty(ref _isNew, value);
            Title = value ? ViewResourceLoader.GetString("NewPassword") : ViewResourceLoader.GetString("EditPassword");
        }
    }

    public List<string>? AvailableCatalogs
    {
        get => _availableCatalogs;
        set
        {
            SetProperty(ref _availableCatalogs, value);
            OnPropertyChanged(nameof(HasCatalogs));
        }
    }

    public bool HasCatalogs => AvailableCatalogs != null && AvailableCatalogs.Count != 0;

    #endregion

    #region Commands

    public RelayCommand OpenIndexViewCommand { get; }
    public IAsyncRelayCommand SavePasswordComand { get; }
    public RelayCommand OpenGeneratePasswordDialogCommand { get; }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        NavigationParameters? navParams = parameter as NavigationParameters;
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
                    var queryResult = await _sender.Send(new Application.UseCases.Password.Queries.UserPasswords.UserPasswordsQuery());
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
                        PasswordValidator!.Login = passwordResult.Value.Login;
                        PasswordValidator!.Title = passwordResult.Value.Name;
                        PasswordValidator!.Password = passwordResult.Value.Value;
                        PasswordValidator!.SelectedCatalog = passwordResult.Value.CatalogPath;

                        if (passwordResult.Value.Properties.TryGetValue(PasswordProperties.ExpirationDate, out string? expDateStr))
                        {
                            if (DateTimeOffset.TryParse(expDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var expDate))
                            {
                                PasswordValidator.ExpirationDate = expDate;
                                PasswordValidator.HasExpirationDate = true;
                            }
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

    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<PasswordGeneratedForEditMessage>(this, OnPasswordGeneratedForEdit);
    }

    protected override void UnregisterMessages()
    {
        base.UnregisterMessages();
    }

    #endregion

    #region Private Methods

    private void Clear()
    {
        if (PasswordValidator == null)
        {
            PasswordValidator = new EditPasswordValidator();
        }
        else
        {
            PasswordValidator.Reset();
        }
    }

    private async Task OnSavePassword()
    {
        PasswordValidator!.Validate();

        if (!PasswordValidator.HasErrors)
        {
            var props = new Dictionary<string, string>() { { PasswordProperties.Notes, PasswordValidator.Notes } };

            if (PasswordValidator.HasExpirationDate && PasswordValidator.ExpirationDate.HasValue)
            {
                props.Add(PasswordProperties.ExpirationDate, PasswordValidator.ExpirationDate.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            }

            ErrorOr.ErrorOr<PangoPasswordDto> result;

            if (IsNew)
            {
                result = await _sender.Send(
                    new NewPasswordCommand(PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password, props)
                    {
                        CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                    });
            }
            else
            {
                result = await _sender.Send(
                    new UpdatePasswordCommand(PasswordValidator.Id!.Value, PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password, PasswordValidator.Star, props)
                    {
                        CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                    });
            }

            string message = result.IsError ? ViewResourceLoader.GetString("CannotSavePassword")
                : IsNew ? string.Format(ViewResourceLoader.GetString("PasswordCreated"), PasswordValidator.Title)
                        : string.Format(ViewResourceLoader.GetString("PasswordModified"), PasswordValidator.Title);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message, result.IsError ? AppNotificationType.Error : AppNotificationType.Success));

            if (!result.IsError)
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                if (IsNew)
                    WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
                else
                    WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(entity));

                OnOpenIndexView();
            }
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("ValidationError_Required") ?? "This field is required.",
                AppNotificationType.Warning));
        }
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new NavigationParameters(AppView.PasswordsIndex, AppView.EditPassword)));
    }

    private async void OnOpenGeneratePasswordDialog()
    {
        var parameters = new GeneratePasswordDialogParameters(password: PasswordValidator?.Password ?? string.Empty);
        await _dialogService.ShowGeneratePasswordDialogAsync(parameters);
    }

    /// <summary>
    /// retrieves the generated password from the dialog
    /// </summary>
    /// <param name="recipient"></param>
    /// <param name="message"></param>
    private void OnPasswordGeneratedForEdit(object recipient, PasswordGeneratedForEditMessage message)
    {
        if (PasswordValidator is null) return;
        PasswordValidator.Password = message.Value;
    }

    #endregion
}
