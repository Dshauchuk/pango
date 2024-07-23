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
}
