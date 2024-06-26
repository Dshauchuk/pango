﻿using Pango.Desktop.Uwp.Dialogs.Parameters;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IDialogService
{
    Task ShowNewCatalogDialogAsync(EditCatalogParameters catalogParameters);

    Task ShowPasswordDetailsAsync(PasswordDetailsParameters passwordDetailsParameters);

    Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText);
}

