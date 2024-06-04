using Pango.Desktop.Uwp.Core.Enums;
using System;

namespace Pango.Desktop.Uwp.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AppViewAttribute : Attribute
{
	public AppViewAttribute(AppView appView)
	{
        View = appView;
    }

    public AppView View { get; }
}
