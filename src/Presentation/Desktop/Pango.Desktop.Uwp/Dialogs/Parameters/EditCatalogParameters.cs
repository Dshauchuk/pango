using Pango.Desktop.Uwp.Models;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public class EditCatalogParameters(List<string> allAvailableCatalogs, string defaultCatalog,
    PangoExplorerItem? selectedCatalog, List<string> existingCatalogs) 
    : IDialogParameter
{
    /// <summary>
    /// List of catalogs that could be parent for the new/updated one
    /// </summary>
    public List<string> AllAvailableCatalogs { get; } = allAvailableCatalogs;

    /// <summary>
    /// Initially set catalog
    /// </summary>
    public string DefaultCatalog { get; } = defaultCatalog;

    /// <summary>
    /// Selected catalog to update
    /// </summary>
    public PangoExplorerItem? SelectedCatalog { get; } = selectedCatalog;

    /// <summary>
    /// Existing catalogs
    /// </summary>
    public List<string> ExistingCatalogs { get; } = existingCatalogs;
}
