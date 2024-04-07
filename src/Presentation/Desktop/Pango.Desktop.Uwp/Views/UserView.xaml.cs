﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.User)]
public sealed partial class UserView : PageBase
{
    public UserView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<UserViewModel>();
    }
}
