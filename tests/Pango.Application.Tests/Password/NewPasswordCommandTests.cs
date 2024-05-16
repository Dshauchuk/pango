using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;

namespace Pango.Application.Tests.Password;

public class NewPasswordCommandTests
{
    [Fact]
    public void NewPasswordCommand_Creation_Succeeds_With_Default_Properties()
    {
        // Arrange
        string name = "Password";
        string login = "user";
        string value = "encrypted_value";

        // Act
        var command = new NewPasswordCommand(name, login, value);

        // Assert
        Assert.Equal(name, command.Name);
        Assert.Equal(login, command.Login);
        Assert.Equal(value, command.Value);
        Assert.NotNull(command.Properties);
        Assert.Empty(command.Properties);
    }
}

public class NewPasswordCommandHandlerTests
{
    [Fact]
    public async Task NewPasswordCommandHandler_Creates_Password_Successfully()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new NewPasswordCommandHandler(mockRepository.Object);
        var request = new NewPasswordCommand("Password", "user", "encrypted_value");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<Domain.Entities.PangoPassword>()), Times.Once);
        Assert.False(result.IsError);
    }

    [Fact]
    public void T()
    {
        List<PangoPasswordListItemDto> passwords = new List<PangoPasswordListItemDto>()
        {
            new PangoPasswordListItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Facebook",
                IsCatalog = true,
                Children = new List<PangoPasswordListItemDto>()
                {
                    new PangoPasswordListItemDto()
                    {
                         Id = Guid.NewGuid(),
                         IsCatalog = false,
                         Name = "My Facebook",
                         CatalogPath = "Facebook"
                         
                    },
                    new PangoPasswordListItemDto()
                    {
                         Id = Guid.NewGuid(),
                         IsCatalog = false,
                         Name = "Dad's Facebook",
                         CatalogPath = "Facebook"
                    },
                    new PangoPasswordListItemDto()
                    {
                         Id = Guid.NewGuid(),
                         IsCatalog = true,
                         Name = "Old",
                         CatalogPath = "Facebook",
                         Children = new List<PangoPasswordListItemDto>()
                         {
                             new PangoPasswordListItemDto 
                             { 
                                 Id = Guid.NewGuid(),
                                 IsCatalog = false,
                                 Name = "My Old facebook #1",
                                 CatalogPath = "Facebook/Old"
                             }
                         }
                    }
                },
            }
        };


        var catalogHolder = new PangoPasswordListItemDto()
        {
            Id = Guid.NewGuid(),
            CatalogPath = "Facebook/Old",
            IsCatalog = true,
            Name = "catalogholder"
        };

        var newPwd = new PangoPasswordListItemDto
        {
            Id = Guid.NewGuid(),
            CatalogPath = "Facebook/Old",
            IsCatalog = false,
            Name = "My Old facebook #2",
        };

        string[] catalogs = catalogHolder.CatalogPath.Split('/');

        Queue<string> catalogQueue = new Queue<string>(catalogs);


        AddPassword(passwords, catalogHolder, catalogQueue);
        AddPassword(passwords, newPwd, catalogQueue);
    }

    void AddPassword(List<PangoPasswordListItemDto> passwords, PangoPasswordListItemDto password, Queue<string> catalogs)
    {
        string catalogName = catalogs.Dequeue();

        var catalog = passwords.FirstOrDefault(p => p.IsCatalog && p.Name == catalogName);

        if (catalogs.Any())
        {
            AddPassword(catalog.Children, password, catalogs);
        }
        else
        {
            catalog.Children.Add(password);
        }
    }
}
