using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Application.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Pango.Desktop.Uwp.Models;

public class SelectionChangedMessage { }

[DebuggerDisplay("{UniqueId}")]
public partial class PangoExplorerItem : ObservableObject
{
    public enum ExplorerItemType { Folder, File };

    #region Fields

    private bool _isExpanded;
    private string _catalogPath = string.Empty;
    private bool _isStar;
    private bool _isVisible = true;
    private bool _isSelected = false;
    private bool _isSettingSelection = false;

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

    public bool IsFolder => Type == ExplorerItemType.Folder;

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

    public bool IsStar
    {
        get => _isStar;
        set => SetProperty(ref _isStar, value);
    }

    public ExplorerItemType Type { get; set; }

    /// <summary>
    /// Parent element. Null for the first level of nesting
    /// </summary>
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
        set
        {
            if (_isSettingSelection) return;

            _isSettingSelection = true;
            try
            {
                if (SetProperty(ref _isSelected, value))
                {
                    if (IsFolder)
                    {
                        foreach (var child in Children)
                        {
                            child.IsSelected = value;
                        }
                    }

                    if (value && Parent != null)
                    {
                        // Check if all siblings are selected to update parent state if needed (optional logic could go here)
                        Parent.IsSelected = true;
                    }

                    WeakReferenceMessenger.Default.Send(new SelectionChangedMessage());
                }
            }
            finally
            {
                _isSettingSelection = false;
            }
        }
    }

    #endregion

    public void AddChild(PangoExplorerItem child)
    {
        // Prevent duplicates: do not add if child with same ID already exists
        if (Children.Any(c => c.Id == child.Id)) return;

        child.Parent = this;
        Children.Add(child);
    }

    public IEnumerable<PangoExplorerItem> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    // Override Equals to ensure items with same ID are treated as one
    public override bool Equals(object? obj)
    {
        if (obj is PangoExplorerItem item)
        {
            return Id == item.Id;
        }
        return false;
    }

    // Override GetHashCode to match Equals logic
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}