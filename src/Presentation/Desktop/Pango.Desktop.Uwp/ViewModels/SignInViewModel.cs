using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public class SignInViewModel : ObservableObject
{
    #region Fields


    #endregion

    public SignInViewModel()
    {
        Users = new() { "John Doe", "Jane Doe" };

        SignInCommand = new AsyncRelayCommand(OnSignIn);
    }

    #region Events

    public event Action SignInSuceeded;

    #endregion

    #region Commands

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public List<string> Users { get; }

    #endregion

    #region Private Methods

    private async Task OnSignIn()
    {
        await Task.CompletedTask;

        SignInSuceeded?.Invoke();
    }

    #endregion
}
