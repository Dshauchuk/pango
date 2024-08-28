using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Application.UseCases.Data.Commands.Import;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Models;
using Pango.Persistence.File;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private int _selectedOption;
    private string _titleText;

    #endregion

    public ExportImportViewModel(ILogger<ExportImportViewModel> logger, ISender sender, IUserContextProvider userContextProvider) : base(logger)
    {
        ExportDataCommand = new RelayCommand(OnExportDataAsync);
        ImportDataCommand = new RelayCommand(OnImportDataAsync);
        NavigateToOptionCommand = new RelayCommand<int>(OnNavigateToOption);

        _sender = sender;
        _userContextProvider = userContextProvider;

        Passwords = [];
        TitleText = ViewResourceLoader.GetString("ExportAndImportTooltip");
    }


    #region Commands

    public RelayCommand ExportDataCommand { get; }
    public RelayCommand ImportDataCommand { get; }
    public RelayCommand<int> NavigateToOptionCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoExplorerItem> Passwords { get; private set; }

    public string TitleText
    {
        get => _titleText;
        set => SetProperty(ref _titleText, value);
    }

    public int SelectedOption
    {
        get => _selectedOption;
        set 
        {
            SetProperty(ref _selectedOption, value);

            TitleText = value switch
            {
                1 => ViewResourceLoader.GetString("ExportTitle"),
                2 => ViewResourceLoader.GetString("ImportTitle"),
                _ => ViewResourceLoader.GetString("ExportAndImportTooltip")
            };
        }
    }

    #endregion

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        await ResetViewAsync();
    }

    #region Private Methods

    private async Task ResetViewAsync()
    {
        IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);
    }

    private async void OnImportDataAsync()
    {
        

    }

    private async void OnExportDataAsync()
    {
        var t = await _userContextProvider.GetEncodingOptionsAsync();

        var a = Convert.FromBase64String(t.Key);
        var b = a[..16];

        string tmpUser = "tmp";
        var encoding = new EncodingOptions(t.Key, Convert.ToBase64String(b));

        ErrorOr<ExportResult> result = 
            await _sender.Send(new ExportDataCommand(PrepareContent(), new ExportOptions(tmpUser, encoding)));

        var t2 = await _sender.Send(new ImportDataCommand(result.Value.Path, new ImportOptions(encoding)));
    }

    private List<ExportItem> PrepareContent()
    {
        var selected = FindAll([.. Passwords], p => p.IsSelected || p.Children.Any(c => c.IsSelected));
        if(selected.Count == 0)
        {
            return [];
        }

        List<ExportItem> items = selected.Select(s => new ExportItem(Domain.Enums.ContentType.Passwords, s.Id)).ToList();

        return items;
    }

    private List<PangoExplorerItem> FindAll(List<PangoExplorerItem> sourceList, Func<PangoExplorerItem, bool> predicate)
    {
        List<PangoExplorerItem> result = [];

        if (sourceList is null || sourceList.Count == 0)
        {
            return result;
        }

        foreach (var item in sourceList)
        {
            if (predicate(item))
            {
                result.Add(item);
            }

            var children = FindAll([.. item.Children], predicate);

            if(children.Count > 0)
            {
                result.AddRange(children);
            }
        }

        return result;
    }

    private void OnNavigateToOption(int option)
    {
        SelectedOption = option switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            _ => throw new InvalidOperationException($"Option {option} cannot be navigated")
        };
    }

    /// <summary>
    /// Loads passwords and returns a list of that
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<PangoExplorerItem>> LoadPasswordsAsync()
    {
        Logger.LogDebug($"Loading passwords...");
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        if (queryResult.IsError)
        {
            Logger.LogError("Loaded passwords failed: {FirstError}", queryResult.FirstError);
            return [];
        }
        else
        {
            Logger.LogDebug("Loaded {count} passwords", queryResult.Value.Count(p => !p.IsCatalog));
            return queryResult.Value.Adapt<IEnumerable<PangoExplorerItem>>().OrderBy(i => i.NestingLevel);
        }
    }

    /// <summary>
    /// Displays <paramref name="passwords"/> in a tree view
    /// </summary>
    /// <param name="passwords"></param>
    private void DisplayPasswordsInTree(IEnumerable<PangoExplorerItem> passwords)
    {
        Passwords.Clear();
        foreach (var pwd in passwords)
        {
            AddPassword(Passwords, pwd, pwd.CatalogPath.ParseCatalogPath());
        }
    }

    /// <summary>
    /// Adds a <paramref name="password"/> to the tree collection of <paramref name="passwords"/>
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    /// <param name="catalogs"></param>
    /// <returns></returns>
    private bool AddPassword(ObservableCollection<PangoExplorerItem> passwords, PangoExplorerItem password, Queue<string>? catalogs)
    {
        if (catalogs is null || catalogs.Count == 0)
        {
            Insert(passwords, password);
            return true;
        }

        string catalogName = catalogs.Dequeue();
        PangoExplorerItem? catalog = passwords.FirstOrDefault(p => p.Type == PangoExplorerItem.ExplorerItemType.Folder && p.Name == catalogName);

        if (catalog is null)
        {
            return false;
        }

        if (catalogs.Any())
        {
            return AddPassword(catalog.Children, password, catalogs);
        }
        else
        {
            password.Parent = catalog;
            Insert(catalog.Children, password);
            return true;
        }
    }

    /// <summary>
    /// Inserts <paramref name="passwordToInsert"/> into sorted <paramref name="sortedPasswordsList"/>
    /// </summary>
    /// <param name="sortedPasswordsList">already sorted collection of passwords</param>
    /// <param name="passwordToInsert">a password to add into the <paramref name="sortedPasswordsList"/></param>
    private static void Insert(ObservableCollection<PangoExplorerItem> sortedPasswordsList, PangoExplorerItem passwordToInsert)
    {
        if (!sortedPasswordsList.Any())
        {
            sortedPasswordsList.Add(passwordToInsert);
            return;
        }

        int index = 0;

        if (passwordToInsert.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            for (; index < sortedPasswordsList.Count; index++)
            {
                if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File)
                {
                    break;
                }
            }
        }

        for (; index < sortedPasswordsList.Count; index++)
        {
            if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File && passwordToInsert.Type == PangoExplorerItem.ExplorerItemType.Folder)
            {
                break;
            }

            if (sortedPasswordsList[index].Name.CompareTo(passwordToInsert.Name) < 0)
            {
                continue;
            }
            else
            {
                break;
            }
        }

        sortedPasswordsList.Insert(index, passwordToInsert);
    }

    #endregion

}
