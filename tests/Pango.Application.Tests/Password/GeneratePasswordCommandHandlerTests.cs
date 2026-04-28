using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.Password.Commands.GeneratePassword;

namespace Pango.Application.Tests.Password;

public class GeneratePasswordCommandHandlerTests
{
    private readonly Mock<IPasswordGenerator> _generator = new();
    private readonly Mock<ILogger<GeneratePasswordCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ReturnsError_WhenLengthOutOfRange()
    {
        var handler = new GeneratePasswordCommandHandler(_generator.Object, _logger.Object);
        var shortCmd = new GeneratePasswordCommand(2, true, true, true, true, false);
        var longCmd = new GeneratePasswordCommand(128, true, true, true, true, false);

        var r1 = await handler.Handle(shortCmd, CancellationToken.None);
        var r2 = await handler.Handle(longCmd, CancellationToken.None);

        Assert.True(r1.IsError);
        Assert.Equal(ApplicationErrors.Password.GenerationInvalidLength, r1.FirstError.Code);
        Assert.True(r2.IsError);
        _generator.Verify(x => x.GeneratePasswordAsync(It.IsAny<PasswordGenerationOptions>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenNoCharsetSelected()
    {
        var handler = new GeneratePasswordCommandHandler(_generator.Object, _logger.Object);
        var cmd = new GeneratePasswordCommand(16, false, false, false, false, false);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.GenerationInvalidCharsets, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ReturnsGeneratedPassword_WhenValid()
    {
        _generator.Setup(x => x.GeneratePasswordAsync(It.IsAny<PasswordGenerationOptions>()))
            .ReturnsAsync("Generated!1");

        var handler = new GeneratePasswordCommandHandler(_generator.Object, _logger.Object);
        var cmd = new GeneratePasswordCommand(16, true, true, true, true, true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("Generated!1", result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenGeneratorThrowsException()
    {
        // Arrange
        _generator.Setup(x => x.GeneratePasswordAsync(It.IsAny<PasswordGenerationOptions>()))
            .ThrowsAsync(new Exception("Crypto failure"));

        var handler = new GeneratePasswordCommandHandler(_generator.Object, _logger.Object);
        var cmd = new GeneratePasswordCommand(16, true, true, true, true, true);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.CreationFailed, result.FirstError.Code);
    }
}
