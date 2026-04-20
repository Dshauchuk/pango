using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Application.Common;
using Pango.Desktop.Uwp.Core.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pango.Desktop.Uwp.Models;

/// <summary>
/// Message sent when selection state changes in the tree view.
/// </summary>
public class SelectionChangedMessage;

/// <summary>
/// Represents a node in the password/catalog tree view with support for hierarchical data, selection, and expiration tracking.
/// </summary>
[DebuggerDisplay("{UniqueId}")]
public partial class PangoExplorerItem : ObservableObject
{
    /// <summary>
    /// Defines the type of explorer item: folder or file.
    /// </summary>
    public enum ExplorerItemType { Folder, File }

    #region Fields

    private bool _isExpanded;
    private string _catalogPath = string.Empty;
    private bool _isStar;
    private bool _isVisible = true;
    private bool _isSelected;
    private bool _isSettingSelection;
    private ExplorerItemType _type;
    private IList<PangoExplorerItem> _children = [];
    private PasswordExpirationStatus _expirationStatus = PasswordExpirationStatus.Valid;
    private string? _expirationTooltip;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PangoExplorerItem"/> class with default values.
    /// </summary>
    public PangoExplorerItem()
    {
        Children = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PangoExplorerItem"/> class with specified properties.
    /// </summary>
    /// <param name="id">Unique identifier for the item.</param>
    /// <param name="name">Display name of the item.</param>
    /// <param name="type">Type of the item (folder or file).</param>
    public PangoExplorerItem(Guid id, string name, ExplorerItemType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a unique runtime identifier for this instance (distinct from business Id).
    /// </summary>
    public Guid UniqueId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the business identifier of the item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this item is a folder.
    /// </summary>
    public bool IsFolder => Type == ExplorerItemType.Folder;

    /// <summary>
    /// Gets the nesting level of this item in the tree hierarchy.
    /// </summary>
    public int NestingLevel { get; private set; }

    /// <summary>
    /// Gets or sets additional properties associated with this item.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the catalog path string for this item.
    /// </summary>
    public string CatalogPath
    {
        get => _catalogPath;
        set
        {
            if (SetProperty(ref _catalogPath, value))
            {
                NestingLevel = string.IsNullOrEmpty(CatalogPath)
                    ? 0
                    : CatalogPath.Count(c => c == AppConstants.CatalogDelimeter) + 1;
                Log.Logger?.Debug("CatalogPath updated for {ItemName}: level {Level}", Name, NestingLevel);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is starred/favorited.
    /// </summary>
    public bool IsStar
    {
        get => _isStar;
        set
        {
            if (SetProperty(ref _isStar, value))
            {
                Log.Logger?.Debug("Star status changed for {ItemName}: {IsStar}", Name, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the type of this explorer item.
    /// </summary>
    public ExplorerItemType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
            {
                OnPropertyChanged(nameof(IsFolder));
                Log.Logger?.Debug("Type changed for {ItemName}: {Type}", Name, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the parent item in the tree hierarchy. Null for root-level items.
    /// </summary>
    public virtual PangoExplorerItem? Parent { get; set; }

    /// <summary>
    /// Gets or sets the collection of child items for folder-type nodes.
    /// </summary>
    public virtual IList<PangoExplorerItem> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this folder is expanded in the UI.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                Log.Logger?.Debug("Expanded state changed for {ItemName}: {IsExpanded}", Name, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is visible after filtering.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is selected. 
    /// Setting this value propagates selection to all children for folders.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSettingSelection || _isSelected == value) return;

            _isSettingSelection = true;
            try
            {
                Log.Logger?.Debug("Setting IsSelected={Value} for {ItemName}", value, Name);
                SetProperty(ref _isSelected, value);

                if (IsFolder)
                {
                    foreach (var child in Children)
                    {
                        child.SetSelectedWithoutNotify(value);
                    }
                    Log.Logger?.Debug("Propagated selection to {Count} children of {ItemName}", Children.Count, Name);
                }

                WeakReferenceMessenger.Default.Send(new SelectionChangedMessage());
            }
            finally
            {
                _isSettingSelection = false;
            }
        }
    }

    /// <summary>
    /// Sets selection state internally without broadcasting a global messenger event.
    /// Used for recursive propagation from parent to children.
    /// </summary>
    /// <param name="value">The selection state to apply.</param>
    public void SetSelectedWithoutNotify(bool value)
    {
        if (_isSelected != value)
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));

            if (IsFolder)
            {
                foreach (var child in Children)
                {
                    child.SetSelectedWithoutNotify(value);
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the expiration status of this password item.
    /// </summary>
    public PasswordExpirationStatus ExpirationStatus
    {
        get => _expirationStatus;
        set => SetProperty(ref _expirationStatus, value);
    }

    /// <summary>
    /// Gets or sets the expiration date for this password item.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the tooltip text describing expiration status.
    /// </summary>
    public string? ExpirationTooltip
    {
        get => _expirationTooltip;
        set => SetProperty(ref _expirationTooltip, value);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a child item to this folder, preventing duplicates by ID.
    /// </summary>
    /// <param name="child">The child item to add.</param>
    public void AddChild(PangoExplorerItem child)
    {
        if (Children.Any(c => c.Id == child.Id))
        {
            Log.Logger?.Debug("Skipped adding duplicate child {ChildId} to {ParentName}", child.Id, Name);
            return;
        }

        child.Parent = this;
        Children.Add(child);
        Log.Logger?.Debug("Added child {ChildName} to {ParentName}", child.Name, Name);
    }

    /// <summary>
    /// Recursively yields all descendant items in depth-first order.
    /// </summary>
    /// <returns>Enumerable of all descendant items.</returns>
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

    /// <summary>
    /// Recalculates the folder's expiration status based on its children and propagates upward.
    /// </summary>
    public void RecalculateExpirationStatus()
    {
        if (!IsFolder || !Children.Any()) return;

        Log.Logger?.Debug("Recalculating expiration status for folder {FolderName}", Name);

        if (Children.Any(c => c.ExpirationStatus == PasswordExpirationStatus.Expired))
        {
            ExpirationStatus = PasswordExpirationStatus.Expired;
            Log.Logger?.Debug("Folder {FolderName} marked as Expired", Name);
        }
        else if (Children.Any(c => c.ExpirationStatus == PasswordExpirationStatus.ExpiringSoon))
        {
            ExpirationStatus = PasswordExpirationStatus.ExpiringSoon;
            Log.Logger?.Debug("Folder {FolderName} marked as ExpiringSoon", Name);
        }
        else
        {
            ExpirationStatus = PasswordExpirationStatus.Valid;
        }

        Parent?.RecalculateExpirationStatus();
    }

    /// <summary>
    /// Releases references to child items and properties to assist with memory cleanup.
    /// </summary>
    public void ReleaseReferences()
    {
        Log.Logger?.Debug("Releasing references for {ItemName}", Name);

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.ReleaseReferences();
            }
            Children.Clear();
        }

        Properties?.Clear();
        Parent = null;
    }

    /// <summary>
    /// Recalculates the catalog path and nesting level for this item and all descendants.
    /// </summary>
    public void RecalculateCatalogPath()
    {
        Log.Logger?.Debug("Recalculating catalog path for {ItemName}", Name);

        if (Parent != null && !string.IsNullOrEmpty(Parent.CatalogPath))
        {
            CatalogPath = $"{Parent.CatalogPath}{AppConstants.CatalogDelimeter}{Parent.Name}";
        }
        else if (Parent != null)
        {
            CatalogPath = Parent.Name;
        }
        else
        {
            CatalogPath = string.Empty;
        }

        foreach (var child in Children)
        {
            child.RecalculateCatalogPath();
        }
    }

    #endregion
}
