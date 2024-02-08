using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.Password.Queries.UserPasswords;

namespace Pango.Application.Tests.Password;

public class UserPasswordsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Passwords()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new UserPasswordsQueryHandler(mockRepository.Object);
        var expectedPasswords = new List<Domain.Entities.PangoPassword>
        {
            new Domain.Entities.PangoPassword { Id = Guid.NewGuid(), Name = "Password1" },
            new Domain.Entities.PangoPassword { Id = Guid.NewGuid(), Name = "Password2" }
        };

        mockRepository.Setup(repo => repo.QueryAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
            .ReturnsAsync(expectedPasswords);

        // Act
        var request = new UserPasswordsQuery();
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(expectedPasswords.Count, result.Value.Count());
        Assert.All(expectedPasswords, password =>
        {
            Assert.Contains(result.Value, dto => dto.Id == password.Id && dto.Name == password.Name);
        });
    }

    [Fact]
    public async Task Handle_Returns_Empty_List_When_No_Passwords_Found()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new UserPasswordsQueryHandler(mockRepository.Object);

        mockRepository.Setup(repo => repo.QueryAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
            .ReturnsAsync(new List<Domain.Entities.PangoPassword>());

        // Act
        var request = new UserPasswordsQuery();
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }
}
