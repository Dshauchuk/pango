using CommunityToolkit.Mvvm.ComponentModel;
using Pango.Application.Common;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Pango.Desktop.Uwp.Models;

[DebuggerDisplay("{UniqueId}")]
public class PangoExplorerItem : ObservableObject
{
    public enum ExplorerItemType { Folder, File };

    #region Fields

    private bool _isExpanded;
    private string _catalogPath = string.Empty;
    private bool _isVisible = true;
    private bool _isSelected = false;

    private ObservableCollection<PangoExplorerItem> _children = [];

    #endregion

    public PangoExplorerItem()
    {
        Children = [];
    }

    public PangoExplorerItem(Guid id, string name, ExplorerItemType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    #region Properties

    public Guid UniqueId { get; } = Guid.NewGuid();

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int NestingLevel { get; private set; } 

    public string CatalogPath
    {
        get => _catalogPath;
        set
        {
            SetProperty(ref _catalogPath, value);
            NestingLevel = string.IsNullOrEmpty(CatalogPath) ? 0 : CatalogPath.Count((c) => c == AppConstants.CatalogDelimeter) + 1;
        }
    }

    public ExplorerItemType Type { get; set; }

    public virtual PangoExplorerItem? Parent { get; set; }

    public virtual ObservableCollection<PangoExplorerItem> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsVisible 
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    #endregion
}
