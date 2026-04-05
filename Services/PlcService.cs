using BarcodePrinter.Models;

namespace BarcodePrinter.Services;

public class PlcService : IDisposable
{
    private readonly ModbusTcpService _modbus = new();
    private readonly PlcConfig _config;
    private CancellationTokenSource? _pollingCts;

    public event Action? PrintTriggered;
    public event Action<bool>? ConnectionChanged;
    public event Action<string>? ErrorOccurred;

    public bool IsConnected => _modbus.IsConnected;

    public PlcService(PlcConfig config)
    {
        _config = config;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _modbus.ConnectAsync(_config.IpAddress, _config.Port, ct);
        ConnectionChanged?.Invoke(true);
    }

    public void Disconnect()
    {
        StopPolling();
        _modbus.Disconnect();
        ConnectionChanged?.Invoke(false);
    }

    public async Task WriteResultAsync(bool isOk, CancellationToken ct = default)
    {
        ushort value = isOk ? (ushort)1 : (ushort)2;
        await _modbus.WriteSingleRegisterAsync(_config.SlaveId, _config.ResultRegister, value, ct);
    }

    public async Task ClearResultAsync(CancellationToken ct = default)
    {
        await _modbus.WriteSingleRegisterAsync(_config.SlaveId, _config.ResultRegister, 0, ct);
    }

    public async Task WritePrintCompleteAsync(CancellationToken ct = default)
    {
        await _modbus.WriteSingleRegisterAsync(_config.SlaveId, _config.PrintCompleteRegister, 1, ct);
        await Task.Delay(500, ct);
        await _modbus.WriteSingleRegisterAsync(_config.SlaveId, _config.PrintCompleteRegister, 0, ct);
    }

    public void StartPolling()
    {
        StopPolling();
        _pollingCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollingCts.Token);
    }

    public void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        bool wasTriggered = false;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var registers = await _modbus.ReadHoldingRegistersAsync(
                    _config.SlaveId, _config.PrintTriggerRegister, 1, ct);

                bool isTriggered = registers[0] == 1;

                if (isTriggered && !wasTriggered)
                    PrintTriggered?.Invoke();

                wasTriggered = isTriggered;
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                ConnectionChanged?.Invoke(false);
                ErrorOccurred?.Invoke($"PLC 통신 오류: {ex.Message}");

                try
                {
                    _modbus.Disconnect();
                    await Task.Delay(3000, ct);
                    await _modbus.ConnectAsync(_config.IpAddress, _config.Port, ct);
                    ConnectionChanged?.Invoke(true);
                    ErrorOccurred?.Invoke("PLC 재연결 성공");
                }
                catch (Exception) when (!ct.IsCancellationRequested)
                {
                    await Task.Delay(3000, ct);
                }
            }

            try { await Task.Delay(_config.PollingIntervalMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        StopPolling();
        _modbus.Dispose();
    }
}
