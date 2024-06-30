using System;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public record PasswordDetailsParameters(Guid PasswordId, List<string> AllAvailableCatalogs) : IDialogParameter;
