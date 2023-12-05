using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Message to notify app, that the language was changed
/// </summary>
public class AppLanguageChangedMessage : ValueChangedMessage<Type>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">Type of the page, to which the User should be redirected after the language is changed</param>
    public AppLanguageChangedMessage(Type value) : base(value)
    {
    }
}