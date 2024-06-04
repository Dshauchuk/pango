using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;

namespace Pango.Application.Tests.Password
{
    public class FindUserPasswordQueryTests
    {
        [Fact]
        public void FindUserPasswordQuery_Stores_PasswordId_Correctly()
        {
            // Arrange
            Guid passwordId = Guid.NewGuid();

            // Act
            var query = new FindUserPasswordQuery(passwordId);

            // Assert
            Assert.Equal(passwordId, query.PasswordId);
        }
    }

    public class FindUserPasswordQueryHandlerTests
    {
        [Fact]
        public async Task Handle_Returns_NotFound_Error_If_Password_Not_Found()
        {
            // Arrange
            var mockRepository = new Mock<IPasswordRepository>();
            var handler = new FindUserPasswordQueryHandler(mockRepository.Object);
            var passwordId = Guid.NewGuid();
            var query = new FindUserPasswordQuery(passwordId);

            mockRepository.Setup(repo => repo.FindAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
                .ReturnsAsync((Domain.Entities.PangoPassword)null);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("General.NotFound", result.FirstError.Code);
        }

        [Fact]
        public async Task Handle_Returns_PasswordDto_If_Password_Found()
        {
            // Arrange
            var mockRepository = new Mock<IPasswordRepository>();
            var handler = new FindUserPasswordQueryHandler(mockRepository.Object);
            var passwordId = Guid.NewGuid();
            var query = new FindUserPasswordQuery(passwordId);
            var password = new Domain.Entities.PangoPassword { Id = passwordId };

            mockRepository.Setup(repo => repo.FindAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
                .ReturnsAsync(password);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Value);
        }
    }
}
