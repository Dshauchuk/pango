using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace Pango.Desktop.Uwp.Models;

public class PasswordExplorerItem : ObservableObject
{
    public enum ExplorerItemType { Folder, File };

    #region Fields

    private bool _isExpanded;

    private ObservableCollection<PasswordExplorerItem> _children;

    #endregion

    public PasswordExplorerItem()
    {
        Children = [];
    }

    public PasswordExplorerItem(Guid id, string name, ExplorerItemType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    #region Properties

    public Guid Id { get; set; }

    public string Name { get; set; }

    public string CatalogPath { get; set; }

    public ExplorerItemType Type { get; set; }

    public ObservableCollection<PasswordExplorerItem> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    #endregion
}
