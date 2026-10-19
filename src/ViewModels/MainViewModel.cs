using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using BTSensitivitySearchV1._0.Models;
using BTSensitivitySearchV1._0.Services;

namespace BTSensitivitySearchV1._0.ViewModels;

public class ResultCell : ViewModelBase
{
    string _value = "";
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }
}

public class ResultRow : ViewModelBase
{
    public string PacketTypeName { get; set; } = "";
    public string Threshold { get; set; } = "";

    double _startLevel;
    public double StartLevel
    {
        get => _startLevel;
        set => SetProperty(ref _startLevel, value);
    }

    public ResultCell Ch1 { get; set; } = new();
    public ResultCell Ch2 { get; set; } = new();
    public ResultCell Ch3 { get; set; } = new();
}

public class MainViewModel : ViewModelBase
{
    readonly CmwController _cmw = new();
    CancellationTokenSource? _cts;
    readonly ManualResetEventSlim _pauseEvent = new(true);
    static readonly DateTime BuildTimeUtc = DateTime.ParseExact(
        BuildInfo.BuildTimeUtc,
        "yyyy-MM-ddTHH:mm:ssZ",
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    static readonly TimeSpan ValidDuration = TimeSpan.FromMinutes(518400);

    public MainViewModel()
    {
        SelectedTestMode = TestMode.ClassicBT;
        CableLoss = 1.5;
        BuildResultTable();

        ConnectCommand = new RelayCommand(_ => DoConnect(), _ => !IsConnected && !IsRunning);
        DisconnectCommand = new RelayCommand(_ => DoDisconnect(), _ => IsConnected && !IsRunning);
        StartCommand = new RelayCommand(_ => DoStart(), _ => IsConnected && !IsRunning);
        StopCommand = new RelayCommand(_ => DoStop(), _ => IsRunning);
        PauseCommand = new RelayCommand(_ => DoPause(), _ => IsRunning && !IsPaused);
        ResumeCommand = new RelayCommand(_ => DoResume(), _ => IsRunning && IsPaused);
    }

    // --- Properties ---

    public List<string> VisaAddresses { get; } =
    [
        "TCPIP0::192.0.2.10::INSTR",
        "TCPIP0::192.0.2.10::INSTR"
    ];

    string _selectedVisaAddress = "TCPIP0::192.0.2.10::INSTR";
    public string SelectedVisaAddress
    {
        get => _selectedVisaAddress;
        set => SetProperty(ref _selectedVisaAddress, value);
    }

    public List<TestModeItem> TestModeItems { get; } =
    [
        new(TestMode.ClassicBT, "Classic Bluetooth"),
        new(TestMode.BLE, "BLE")
    ];

    TestMode _selectedTestMode;
    public TestMode SelectedTestMode
    {
        get => _selectedTestMode;
        set
        {
            if (SetProperty(ref _selectedTestMode, value))
                BuildResultTable();
        }
    }

    double _cableLoss;
    public double CableLoss
    {
        get => _cableLoss;
        set => SetProperty(ref _cableLoss, value);
    }

    bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    string _statusText = "Not Connected";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    string _currentPhase = "";
    public string CurrentPhase
    {
        get => _currentPhase;
        set => SetProperty(ref _currentPhase, value);
    }

    // Result table
    string _ch1Header = "Ch 0";
    public string Ch1Header
    {
        get => _ch1Header;
        set => SetProperty(ref _ch1Header, value);
    }

    string _ch2Header = "Ch 39";
    public string Ch2Header
    {
        get => _ch2Header;
        set => SetProperty(ref _ch2Header, value);
    }

    string _ch3Header = "Ch 78";
    public string Ch3Header
    {
        get => _ch3Header;
        set => SetProperty(ref _ch3Header, value);
    }

    ObservableCollection<ResultRow> _resultRows = [];
    public ObservableCollection<ResultRow> ResultRows
    {
        get => _resultRows;
        set => SetProperty(ref _resultRows, value);
    }

    ObservableCollection<string> _logMessages = [];
    public ObservableCollection<string> LogMessages
    {
        get => _logMessages;
        set => SetProperty(ref _logMessages, value);
    }

    // --- Commands ---

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }

    // --- Methods ---

    void BuildResultTable()
    {
        var rows = new ObservableCollection<ResultRow>();
        var channels = SelectedTestMode == TestMode.ClassicBT
            ? PacketConfig.ClassicBTChannels : PacketConfig.BLEChannels;
        var configs = SelectedTestMode == TestMode.ClassicBT
            ? PacketConfig.ClassicBTConfigs : PacketConfig.BLEConfigs;

        Ch1Header = channels[0].DisplayName;
        Ch2Header = channels[1].DisplayName;
        Ch3Header = channels[2].DisplayName;

        string rateLabel = SelectedTestMode == TestMode.ClassicBT ? "BER" : "PER";
        foreach (var cfg in configs)
            rows.Add(new ResultRow
            {
                PacketTypeName = cfg.DisplayName,
                Threshold = $"{rateLabel}<{cfg.ErrorRateThresholdPercent}%",
                StartLevel = cfg.DefaultStartLevel
            });

        ResultRows = rows;
    }

    void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        Application.Current.Dispatcher.Invoke(() => LogMessages.Add(line));
    }

    bool IsExpired => DateTime.UtcNow - BuildTimeUtc > ValidDuration;

    void DoConnect()
    {
        if (IsExpired) return;

        try
        {
            _cmw.Connect(SelectedVisaAddress);
            var idn = _cmw.GetIdentity();
            IsConnected = true;
            StatusText = "Connected";
            Log($"Connected: {idn.Trim()}");
        }
        catch (Exception ex)
        {
            Log($"Connection failed: {ex.Message}");
            StatusText = "Connection Failed";
        }
    }

    void DoDisconnect()
    {
        _cmw.Disconnect();
        IsConnected = false;
        StatusText = "Disconnected";
        Log("Disconnected.");
    }

    void DoStop()
    {
        _cts?.Cancel();
        _pauseEvent.Set();
        IsPaused = false;
        Log("Stop requested.");
    }

    void DoPause()
    {
        _pauseEvent.Reset();
        IsPaused = true;
        CurrentPhase += " [PAUSED]";
        Log("Paused.");
    }

    void DoResume()
    {
        IsPaused = false;
        _pauseEvent.Set();
        Log("Resumed.");
    }

    void WaitIfPaused(CancellationToken ct)
    {
        _pauseEvent.Wait(ct);
    }

    ResultCell GetCell(int rowIndex, int colIndex)
    {
        var row = ResultRows[rowIndex];
        return colIndex switch { 0 => row.Ch1, 1 => row.Ch2, _ => row.Ch3 };
    }

    void SetCellValue(int rowIndex, int colIndex, string value)
    {
        Application.Current.Dispatcher.Invoke(() => GetCell(rowIndex, colIndex).Value = value);
    }

    void SetCellTesting(int rowIndex, int colIndex, bool testing)
    {
        Application.Current.Dispatcher.Invoke(() => GetCell(rowIndex, colIndex).IsTesting = testing);
    }

    async void DoStart()
    {
        IsRunning = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        BuildResultTable();

        var configs = SelectedTestMode == TestMode.ClassicBT
            ? PacketConfig.ClassicBTConfigs
            : PacketConfig.BLEConfigs;

        var channels = SelectedTestMode == TestMode.ClassicBT
            ? PacketConfig.ClassicBTChannels
            : PacketConfig.BLEChannels;

        try
        {
            await Task.Run(() =>
            {
                _cmw.SetExternalAttenuation(CableLoss);

                for (int ri = 0; ri < configs.Length; ri++)
                {
                    var config = configs[ri];
                    double startLevel = ResultRows[ri].StartLevel;
                    Log($"=== Configuring {config.DisplayName} (Start: {startLevel} dBm) ===");
                    _cmw.ConfigurePacket(config);

                    for (int ci = 0; ci < channels.Length; ci++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var chPair = channels[ci];
                        SetCellTesting(ri, ci, true);
                        SetCellValue(ri, ci, "...");

                        Log($"--- {config.DisplayName} {chPair.DisplayName}: Sensitivity search ---");
                        _cmw.SetChannel(config, chPair);

                        double sensitivity = RunSensitivitySearch(config, startLevel, ct);

                        SetCellValue(ri, ci, $"{sensitivity:F1}");
                        SetCellTesting(ri, ci, false);
                        Log($"=== {config.DisplayName} {chPair.DisplayName}: {sensitivity:F1} dBm ===");
                    }
                }

                Log("========== All tests completed ==========");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentPhase = "Done";
                    StatusText = "Completed";
                });
            }, ct);
        }
        catch (OperationCanceledException)
        {
            Log("Test cancelled.");
            StatusText = "Cancelled";
            CurrentPhase = "Cancelled";
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
            StatusText = "Error";
            CurrentPhase = "Error";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    double RunSensitivitySearch(PacketConfig config, double startLevel, CancellationToken ct)
    {
        double level = startLevel;
        double threshold = config.ErrorRateThresholdPercent;
        string rateLabel = config.TestMode == TestMode.ClassicBT ? "BER" : "PER";

        // Step 1: Probe (-1 dB)
        CurrentPhase = $"Step 1: Probe {config.DisplayName}";
        Log($"  Step 1: Probe from {level:F1} dBm (-1 dB)");

        while (!ct.IsCancellationRequested)
        {
            WaitIfPaused(ct);
            var m = _cmw.MeasureErrorRate(level, config, ct);
            Log($"    {level:F1} dBm => {rateLabel}={m.ErrorRatePercent:F5}%");

            if (m.ErrorRatePercent >= threshold)
            {
                Log($"    {rateLabel} >= {threshold}% => Step 2");
                break;
            }
            level -= 1.0;
        }
        ct.ThrowIfCancellationRequested();

        // Step 2: Search (+0.1 dB)
        CurrentPhase = $"Step 2: Search {config.DisplayName}";
        Log($"  Step 2: Search from {level:F1} dBm (+0.1 dB)");

        while (!ct.IsCancellationRequested)
        {
            WaitIfPaused(ct);
            level = Math.Round(level + 0.1, 1);
            var m = _cmw.MeasureErrorRate(level, config, ct);
            Log($"    {level:F1} dBm => {rateLabel}={m.ErrorRatePercent:F5}%");

            if (m.ErrorRatePercent < threshold)
            {
                Log($"    {rateLabel} < {threshold}% => Step 3");
                break;
            }
        }
        ct.ThrowIfCancellationRequested();

        // Step 3: Triple confirmation
        CurrentPhase = $"Step 3: Confirm {config.DisplayName}";
        Log($"  Step 3: Triple confirm at {level:F1} dBm");

        while (!ct.IsCancellationRequested)
        {
            bool allPass = true;

            for (int i = 1; i <= 3; i++)
            {
                WaitIfPaused(ct);
                var m = _cmw.MeasureErrorRate(level, config, ct);
                Log($"    Confirm {i}/3: {level:F1} dBm => {rateLabel}={m.ErrorRatePercent:F5}%");

                if (m.ErrorRatePercent >= threshold)
                {
                    Log($"    Confirm {i}/3 FAIL => +0.1 dB");
                    allPass = false;
                    level = Math.Round(level + 0.1, 1);
                    break;
                }
            }

            if (allPass) return level;
        }

        ct.ThrowIfCancellationRequested();
        return level;
    }
}

public record TestModeItem(TestMode Mode, string DisplayName)
{
    public override string ToString() => DisplayName;
}
