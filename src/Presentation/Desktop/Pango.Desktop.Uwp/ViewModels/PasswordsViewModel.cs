using CommunityToolkit.Mvvm.ComponentModel;
using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public sealed class PasswordsViewModel : ObservableObject, IViewModel
{
    private ISender _sender;
    private readonly IPasswordRepository passwordRepository;

    public PasswordsViewModel(ISender sender, IPasswordRepository passwordRepository)
    {
        _sender = sender;
        this.passwordRepository = passwordRepository;
        Passwords = new();
    }

    #region Properties

    public ObservableCollection<PasswordDto> Passwords { get; private set; }

    #endregion

    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
    {

        //var password = new Pango.Domain.Entities.Password()
        //{
        //    Id = Guid.NewGuid(),
        //    Login = "VK",
        //    Name = "VK Password",
        //    Value = "qwerty",
        //    Properties = new Dictionary<string, string>(),
        //    Target = "vk.com",
        //    UserName = "Alice",
        //    CreatedAt = DateTimeOffset.Now,
        //    LastModifiedAt = DateTimeOffset.Now
        //};

        //await passwordRepository.CreateAsync(password);

        await LoadPasswords();
    }

    private async Task LoadPasswords()
    {
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PasswordDto>>>(new UserPasswordsQuery());

        Passwords.Clear();
        foreach (var pwd in queryResult.Value)
        {
            Passwords.Add(pwd);
        }
    }
}
