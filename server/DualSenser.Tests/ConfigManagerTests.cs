using System;
using System.IO;
using DualSenser.Service.Common;
using Xunit;

namespace DualSenser.Tests;

public class ConfigManagerTests
{
    [Fact]
    public void GetConfigDirectory_CreatesAndReturnsDirectoryAboveServer()
    {
        // Act
        string configDir = ConfigManager.GetConfigDirectory();

        // Assert
        Assert.NotNull(configDir);
        Assert.True(Directory.Exists(configDir), "O diretório Config deve ser criado automaticamente.");

        string expectedParent = Path.Combine(LoggerConfig.GetRootDirectory(), "Config");
        Assert.Equal(expectedParent, configDir);
    }

    [Fact]
    public void LoadOrCreateConfig_CreatesConfigFileWithNetworkSettings()
    {
        // Arrange
        string filePath = ConfigManager.GetConfigFilePath();

        // Act
        var config = ConfigManager.LoadOrCreateConfig();

        // Assert
        Assert.NotNull(config);
        Assert.True(File.Exists(filePath), "O arquivo config.ini deve existir.");

        string content = File.ReadAllText(filePath);
        Assert.Contains("HttpPort=", content);
        Assert.Contains("EnableUdpBeacon=", content);
    }
}
