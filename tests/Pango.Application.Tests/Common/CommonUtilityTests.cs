using Pango.Application.Common;
using Pango.Application.Common.Exceptions;

namespace Pango.Application.Tests.Common;

public class CommonUtilityTests
{
    [Fact]
    public void Ensure_HasValue_DoesNotThrow_WhenValid()
    {
        // Act & Assert: Should not throw
        Ensure.HasValue("valid string", "param1");
        Ensure.HasValue(new object(), "param2");
    }

    [Fact]
    public void Ensure_HasValue_ThrowsArgumentException_WhenNullOrEmpty()
    {
        // Act & Assert: Null object
        Assert.Throws<ArgumentException>(() => Ensure.HasValue<object>(null!, "nullObj"));

        // Act & Assert: Empty string
        Assert.Throws<ArgumentException>(() => Ensure.HasValue("", "emptyStr"));
    }

    [Fact]
    public void Ensure_AreEqual_DoesNotThrow_WhenEqual()
    {
        // Act & Assert
        Ensure.AreEqual(5, 5, "numbers");
        Ensure.AreEqual("test", "test", "strings");
    }

    [Fact]
    public void Ensure_AreEqual_ThrowsArgumentException_WhenNotEqual()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Ensure.AreEqual(1, 2, "numbers"));
        Assert.Throws<ArgumentException>(() => Ensure.AreEqual("a", "b", "strings"));
    }

    [Theory]
    [InlineData("Folder1", "Folder2", "Folder1/Folder2")]
    [InlineData("Folder1", "", "Folder1")]
    [InlineData("", "", "")]
    [InlineData(null, "Folder", "Folder")]
    public void PasswordPathUtility_BuildCatalogPath_ReturnsCorrectPath(string? p1, string? p2, string expected)
    {
        // Act
        string result = PasswordPathUtility.BuildCatalogPath(p1 ?? string.Empty, p2 ?? string.Empty);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Folder1", "Folder1", true)]
    [InlineData("/Folder1", "Folder1", true)]
    [InlineData("Folder1", "/Folder1", true)]
    [InlineData("Folder1", "Folder2", false)]
    [InlineData("", null, true)]
    public void PasswordPathUtility_AreEqual_EvaluatesCorrectly(string? path1, string? path2, bool expected)
    {
        // Act
        bool result = PasswordPathUtility.AreEqual(path1, path2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PangoException_Constructors_SetCodeCorrectly()
    {
        // Act: Test standard constructor
        var ex1 = new PangoException("ERR_01");

        // Act: Test constructor with message
        var ex2 = new PangoException("ERR_02", "Message");

        // Act: Test constructor with inner exception
        var ex3 = new PangoException("ERR_03", "Message", new Exception());

        // Assert
        Assert.Equal("ERR_01", ex1.Code);

        Assert.Equal("ERR_02", ex2.Code);
        Assert.Equal("Message", ex2.Message);

        Assert.Equal("ERR_03", ex3.Code);
        Assert.NotNull(ex3.InnerException);
    }
}
