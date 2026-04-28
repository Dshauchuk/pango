using Pango.Application.Common.Exceptions;
using Pango.Application.Models;

namespace Pango.Application.Tests.Common;

/// <summary>
/// Tests to ensure all custom exceptions and models are correctly instantiated and their properties/constructors work as expected.
/// </summary>
public class ExceptionsAndModelsTests
{
    [Fact]
    public void Exceptions_Constructors_WorkCorrectly()
    {
        // Act & Assert for PangoDataDecryptionException
        var decEx1 = new PangoDataDecryptionException("ERR", "Message");
        var decEx2 = new PangoDataDecryptionException("ERR", "Message", new Exception());
        Assert.Equal("ERR", decEx1.Code);
        Assert.NotNull(decEx2.InnerException);

        // Act & Assert for PangoDataEncryptionException
        var encEx1 = new PangoDataEncryptionException("ERR", "Message");
        var encEx2 = new PangoDataEncryptionException("ERR", "Message", new Exception());
        Assert.Equal("ERR", encEx1.Code);
        Assert.NotNull(encEx2.InnerException);

        // Act & Assert for PangoExportException
        var expEx1 = new PangoExportException("Export failed");
        var expEx2 = new PangoExportException("Export failed", new Exception());
        Assert.Equal("Export failed", expEx1.Message);
        Assert.NotNull(expEx2.InnerException);

        // Act & Assert for PangoImportException
        var impEx1 = new PangoImportException("Import failed");
        var impEx2 = new PangoImportException("Import failed", new Exception());
        Assert.Equal("Import failed", impEx1.Message);
        Assert.NotNull(impEx2.InnerException);

        // Act & Assert for PasswordNotFoundException
        var pnfEx = new PasswordNotFoundException("Not found");
        Assert.Equal("Not found", pnfEx.Message);
    }

    [Fact]
    public void PasswordGeneratorSettings_Properties_SetAndGetCorrectly()
    {
        // Arrange
        var settings = new PasswordGeneratorSettings
        {
            Length = 20,
            UseUppercase = false,
            UseLowercase = false,
            UseDigits = false,
            UseSpecial = true,
            ExcludeAmbiguous = true
        };

        // Act & Assert
        Assert.Equal(20, settings.Length);
        Assert.False(settings.UseUppercase);
        Assert.False(settings.UseLowercase);
        Assert.False(settings.UseDigits);
        Assert.True(settings.UseSpecial);
        Assert.True(settings.ExcludeAmbiguous);
    }
}
