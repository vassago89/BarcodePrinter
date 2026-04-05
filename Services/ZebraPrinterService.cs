using System.Net.Sockets;
using System.Text;
using BarcodePrinter.Models;

namespace BarcodePrinter.Services;

public class ZebraPrinterService : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private PrinterConfig _config;

    public event Action<bool>? ConnectionChanged;

    public bool IsConnected => _client?.Connected ?? false;

    public ZebraPrinterService(PrinterConfig config)
    {
        _config = config;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        Disconnect();
        _client = new TcpClient
        {
            SendTimeout = 5000,
            ReceiveTimeout = 5000
        };
        await _client.ConnectAsync(_config.IpAddress, _config.Port, ct);
        _stream = _client.GetStream();
        ConnectionChanged?.Invoke(true);
    }

    public void Disconnect()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        ConnectionChanged?.Invoke(false);
    }

    public async Task PrintQrCodeAsync(string data, string? labelText = null, CancellationToken ct = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Printer not connected");

        var zpl = new StringBuilder();
        zpl.AppendLine("^XA");

        // QR Code
        zpl.AppendLine($"^FO{_config.OriginX},{_config.OriginY}");
        zpl.AppendLine($"^BQN,2,{_config.QrMagnification}");
        zpl.AppendLine($"^FDMA,{data}^FS");

        // Label text below QR code
        if (!string.IsNullOrEmpty(labelText))
        {
            int textY = _config.OriginY + (_config.QrMagnification * 25) + 20;
            zpl.AppendLine($"^FO{_config.OriginX},{textY}");
            zpl.AppendLine("^A0N,30,30");
            zpl.AppendLine($"^FD{labelText}^FS");
        }

        zpl.AppendLine("^XZ");

        byte[] bytes = Encoding.UTF8.GetBytes(zpl.ToString());
        await _stream.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
