using SadViewer.ViewModels;

namespace SadViewer.Tests;

public class MainViewModelTests
{
    [Fact]
    public void LogLine_FormatsLevelAndMessage()
    {
        var line = new LogLine
        {
            Time = new DateTime(2026, 1, 2, 3, 4, 5),
            Level = LogLevel.Warn,
            Message = "Aviso de prueba"
        };

        Assert.Equal("03:04:05", line.TimeText);
        Assert.Equal("WARN", line.LevelText);
        Assert.Equal("[03:04:05] WARN  Aviso de prueba", line.ToString());
    }

    [Fact]
    public void SadConfig_HasExpectedDefaults()
    {
        var config = new SADConfig();

        Assert.Equal("COM1", config.COM);
        Assert.Equal(115200, config.RATE);
        Assert.Equal("SAD", config.DatabaseFolder);
        Assert.Equal("BS.hrl", config.HRLFile);
        Assert.Equal("BS.ght", config.GHTFile);
    }

    [Fact]
    public void RelayCommand_ExecutesAction()
    {
        object? received = null;
        var command = new RelayCommand(parameter => received = parameter);
        var parameter = new object();

        Assert.True(command.CanExecute(parameter));
        command.Execute(parameter);

        Assert.Same(parameter, received);
    }

    [Fact]
    public void RelayCommand_RespectsCanExecute()
    {
        var executed = false;
        var command = new RelayCommand(_ => executed = true, _ => false);

        Assert.False(command.CanExecute(null!));
        Assert.False(executed);
    }

    [Fact]
    public void MainViewModel_FiltersLogByLevelAndText()
    {
        var viewModel = new MainViewModel();
        viewModel.LogLines.Clear();
        viewModel.LogLines.Add(new LogLine { Level = LogLevel.Info, Message = "Puerto detectado" });
        viewModel.LogLines.Add(new LogLine { Level = LogLevel.Error, Message = "Puerto inválido" });

        viewModel.ShowInfo = false;
        viewModel.LogFilter = "inválido";

        var result = viewModel.FilteredLogLines.Cast<LogLine>().ToArray();

        var line = Assert.Single(result);
        Assert.Equal(LogLevel.Error, line.Level);
        Assert.Equal("Puerto inválido", line.Message);

        viewModel.Shutdown();
    }
}
