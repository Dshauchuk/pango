using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.MovePasswordsToCatalog;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class MovePasswordsToCatalogCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _mockPasswordRepository;
    private readonly Mock<IUserContextProvider> _mockUserContextProvider;
    private readonly Mock<IRepositoryContextFactory> _mockRepositoryContextFactory;
    private readonly Mock<ILogger<MovePasswordsToCatalogCommandHandler>> _mockLogger;
    private readonly Mock<IRepositoryActionContext> _mockContext;

    public MovePasswordsToCatalogCommandHandlerTests()
    {
        _mockPasswordRepository = new Mock<IPasswordRepository>();
        _mockUserContextProvider = new Mock<IUserContextProvider>();
        _mockRepositoryContextFactory = new Mock<IRepositoryContextFactory>();
        _mockLogger = new Mock<ILogger<MovePasswordsToCatalogCommandHandler>>();
        _mockContext = new Mock<IRepositoryActionContext>();

        // Common setup for context creation
        _mockUserContextProvider.Setup(x => x.GetUserName()).Returns("testuser");
        _mockUserContextProvider.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("key", "salt"));
        _mockRepositoryContextFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>()))
            .Returns(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenCommandIsEmpty()
    {
        // Arrange
        var command = new MovePasswordsToCatalogCommand(new Dictionary<Guid, string>());
        var handler = GetHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.Value);
        _mockPasswordRepository.Verify(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), It.IsAny<IRepositoryActionContext>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPasswordsNotFound()
    {
        // Arrange
        var passwordId = Guid.NewGuid();
        var command = new MovePasswordsToCatalogCommand(new Dictionary<Guid, string>
        {
            { passwordId, "New/Path" }
        });

        // Setup repository to return empty list (password not found)
        _mockPasswordRepository
            .Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _mockContext.Object))
            .ReturnsAsync(new List<PangoPassword>());

        var handler = GetHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCountMismatch()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var command = new MovePasswordsToCatalogCommand(new Dictionary<Guid, string>
        {
            { id1, "Path1" },
            { id2, "Path2" }
        });

        // Return only one password
        var existingPassword = new PangoPassword { Id = id1 };
        _mockPasswordRepository
            .Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _mockContext.Object))
            .ReturnsAsync(new List<PangoPassword> { existingPassword });

        var handler = GetHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
        Assert.Contains(id2.ToString(), result.FirstError.Description);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCatalogPaths_WhenSuccess()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var newPath1 = "Folder/SubA";
        var newPath2 = "Folder/SubB";

        var command = new MovePasswordsToCatalogCommand(new Dictionary<Guid, string>
        {
            { id1, newPath1 },
            { id2, newPath2 }
        });

        var pwd1 = new PangoPassword { Id = id1, CatalogPath = "OldPath" };
        var pwd2 = new PangoPassword { Id = id2, CatalogPath = "OldPath" };

        _mockPasswordRepository
            .Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _mockContext.Object))
            .ReturnsAsync(new List<PangoPassword> { pwd1, pwd2 });

        var handler = GetHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.True(result.Value);

        // Verify objects were modified
        Assert.Equal(newPath1, pwd1.CatalogPath);
        Assert.Equal(newPath2, pwd2.CatalogPath);

        // Verify UpdateAsync was called for both
        _mockPasswordRepository.Verify(x => x.UpdateAsync(pwd1, _mockContext.Object), Times.Once);
        _mockPasswordRepository.Verify(x => x.UpdateAsync(pwd2, _mockContext.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenRepositoryThrowsException()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var command = new MovePasswordsToCatalogCommand(new Dictionary<Guid, string>
        {
            { id1, "Path" }
        });

        var pwd1 = new PangoPassword { Id = id1 };

        _mockPasswordRepository
            .Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _mockContext.Object))
            .ReturnsAsync(new List<PangoPassword> { pwd1 });

        _mockPasswordRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PangoPassword>(), _mockContext.Object))
            .ThrowsAsync(new Exception("Database error"));

        var handler = GetHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.ModificationFailed, result.FirstError.Code);

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    private MovePasswordsToCatalogCommandHandler GetHandler()
    {
        return new MovePasswordsToCatalogCommandHandler(
            _mockPasswordRepository.Object,
            _mockUserContextProvider.Object,
            _mockRepositoryContextFactory.Object,
            _mockLogger.Object
        );
    }
}