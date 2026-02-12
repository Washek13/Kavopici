using Kavopici.Services;
using Xunit;

namespace Kavopici.Tests.Services;

public class AppSettingsServiceTests
{
    [Fact]
    public void ExpandPath_TildeAtStart_ExpandsToUserHome()
    {
        // Arrange
        var path = "~/Documents/test.db";
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "test.db");

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandPath_TildeOnly_ExpandsToUserHome()
    {
        // Arrange
        var path = "~";
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpandPath_AbsolutePath_ReturnsNormalized()
    {
        // Arrange
        var path = OperatingSystem.IsWindows() ? @"C:\Users\test\test.db" : "/Users/test/test.db";

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Contains("test.db", result);
    }

    [Fact]
    public void ExpandPath_RelativePath_ConvertsToAbsolute()
    {
        // Arrange
        var path = "test.db";

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.EndsWith("test.db", result);
    }

    [Fact]
    public void ExpandPath_RelativePathWithDirectory_ConvertsToAbsolute()
    {
        // Arrange
        var path = "./data/test.db";

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Contains("data", result);
        Assert.EndsWith("test.db", result);
    }

    [Fact]
    public void ExpandPath_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var path = "";

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ExpandPath_Whitespace_ReturnsWhitespace()
    {
        // Arrange
        var path = "   ";

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void ExpandPath_PathWithWhitespace_TrimsAndExpands()
    {
        // Arrange
        var path = "  ~/Documents/test.db  ";
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "test.db");

        // Act
        var result = AppSettingsService.ExpandPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPlatformPlaceholderPath_ReturnsCorrectFormat()
    {
        // Act
        var result = AppSettingsService.GetPlatformPlaceholderPath();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("kavopici.db", result);

        if (OperatingSystem.IsWindows())
        {
            Assert.Contains(@"C:\", result);
        }
        else
        {
            Assert.Contains("~/Documents", result);
        }
    }

    [Fact]
    public void SetDatabasePath_ExpandsPath_BeforeSaving()
    {
        // Arrange
        var service = new AppSettingsService();
        var path = "~/Documents/test.db";
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "test.db");

        // Act
        service.SetDatabasePath(path);

        // Assert
        Assert.Equal(expected, service.DatabasePath);
    }

    [Fact]
    public void SetDatabasePath_NullPath_SetsNull()
    {
        // Arrange
        var service = new AppSettingsService();

        // Act
        service.SetDatabasePath(null);

        // Assert
        Assert.Null(service.DatabasePath);
    }

    [Fact]
    public void SetDatabasePath_EmptyPath_SetsNull()
    {
        // Arrange
        var service = new AppSettingsService();

        // Act
        service.SetDatabasePath("");

        // Assert
        Assert.Null(service.DatabasePath);
    }
}
