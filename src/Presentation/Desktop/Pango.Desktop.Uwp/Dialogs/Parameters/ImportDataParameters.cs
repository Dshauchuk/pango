using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Dialogs.Parameters;

// Defines parameters for data import dialog
public class ImportDataParameters(string filePath, IEnumerable<PangoPassword>? preLoadedContent = null) : IDialogParameter, INavigationParameter
{
    public string FilePath { get; } = filePath;
    public IEnumerable<PangoPassword> PreLoadedContent { get; } = preLoadedContent ?? [];
    public List<Guid> SelectedItems { get; set; } = [];
}

public class ImportDataParametersWithPassword(string filePath, string password, IEnumerable<PangoPassword> content) : ImportDataParameters(filePath, content)
{
    public string Password { get; } = password;
}