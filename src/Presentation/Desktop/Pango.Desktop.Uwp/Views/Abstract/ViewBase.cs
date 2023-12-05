﻿using Pango.Desktop.Uwp.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class ViewBase : UserControl
{
    public ViewBase()
    {
        RegisterMessages();
    }

    public IViewModel ViewModel => (IViewModel)DataContext;

    protected virtual async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnNavigatedToAsync(e.Parameter);
    }

    protected virtual void RegisterMessages()
    {

    }
}
