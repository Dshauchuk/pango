﻿using Microsoft.UI.Xaml.Controls;
using System;

namespace Pango.Desktop.Uwp.Core.Navigation;

/// <summary>
/// A simple model for tracking sample pages associated with buttons.
/// </summary>
public sealed class NavigationEntry
{
    public NavigationEntry(NavigationViewItem viewItem, Type pageType, string? name = null, string? tags = null)
    {
        Item = viewItem;
        PageType = pageType;
        Name = name;
        Tags = tags;
    }

    /// <summary>
    /// The navigation item for the current entry.
    /// </summary>
    public NavigationViewItem Item { get; }

    /// <summary>
    /// The associated page type for the current entry.
    /// </summary>
    public Type PageType { get; }

    /// <summary>
    /// Gets the name of the current entry.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the tag for the current entry, if any.
    /// </summary>
    public string? Tags { get; }
}
