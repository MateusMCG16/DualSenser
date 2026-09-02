using System;
using System.IO;
using DualSenser.Service.Common;
using Xunit;

namespace DualSenser.Tests;

public class LoggerConfigTests
{
    [Fact]
    public void GetLogsDirectory_CreatesAndReturnsDirectoryAboveServer()
    {
        // Act
        string logsDir = LoggerConfig.GetLogsDirectory();

        // Assert
        Assert.NotNull(logsDir);
        Assert.True(Directory.Exists(logsDir), "O diretório Logs deve ser criado automaticamente.");
        
        string expectedParent = Path.Combine(LoggerConfig.GetRootDirectory(), "Logs");
        Assert.Equal(expectedParent, logsDir);
    }

    [Fact]
    public void ConfigureLogger_InitializesWithoutException()
    {
        // Arrange
        var config = new AppConfig();

        // Act & Assert (não deve lançar exceção ao configurar)
        var exception1 = Record.Exception(() => LoggerConfig.ConfigureLogger(config));
        var exception2 = Record.Exception(() => LoggerConfig.ConfigureLogger(null));

        Assert.Null(exception1);
        Assert.Null(exception2);
    }
}
