using System;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Models.Parameters;

public class EditPasswordParameters(bool isNew, string? catalog, Guid? selectedPasswordId, List<string>? availableCatalogs) : INavigationParameter
{
    public bool IsNew { get; } = isNew;

    public string? Catalog { get; } = catalog;

    public Guid? SelectedPasswordId { get; } = selectedPasswordId;

    public List<string>? AvailableCatalogs { get; } = availableCatalogs;
}
