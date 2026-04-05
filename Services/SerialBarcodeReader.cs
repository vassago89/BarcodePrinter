using System.IO.Ports;
using BarcodePrinter.Models;

namespace BarcodePrinter.Services;

public class SerialBarcodeReader : IDisposable
{
    private SerialPort? _serialPort;
    private string _buffer = "";

    public event Action<string>? BarcodeReceived;
    public event Action<bool>? ConnectionChanged;

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    public void Open(SerialConfig config)
    {
        Close();

        _serialPort = new SerialPort
        {
            PortName = config.PortName,
            BaudRate = config.BaudRate,
            DataBits = config.DataBits,
            Parity = Enum.Parse<Parity>(config.Parity),
            StopBits = Enum.Parse<StopBits>(config.StopBits),
            ReadTimeout = 1000,
            Encoding = System.Text.Encoding.ASCII
        };

        _serialPort.DataReceived += OnDataReceived;
        _serialPort.Open();
        _buffer = "";
        ConnectionChanged?.Invoke(true);
    }

    public void Close()
    {
        if (_serialPort == null) return;

        _serialPort.DataReceived -= OnDataReceived;
        if (_serialPort.IsOpen)
            _serialPort.Close();
        _serialPort.Dispose();
        _serialPort = null;
        ConnectionChanged?.Invoke(false);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;

        try
        {
            string data = _serialPort.ReadExisting();
            _buffer += data;
            ProcessBuffer();
        }
        catch { /* port may have closed */ }
    }

    private void ProcessBuffer()
    {
        while (true)
        {
            int endIndex = -1;
            int skipLength = 0;

            int crIdx = _buffer.IndexOf('\r');
            int lfIdx = _buffer.IndexOf('\n');

            if (crIdx >= 0 && lfIdx == crIdx + 1)
            {
                endIndex = crIdx;
                skipLength = 2;
            }
            else if (crIdx >= 0)
            {
                endIndex = crIdx;
                skipLength = 1;
            }
            else if (lfIdx >= 0)
            {
                endIndex = lfIdx;
                skipLength = 1;
            }

            if (endIndex < 0) break;

            string barcode = _buffer[..endIndex].Trim();
            _buffer = _buffer[(endIndex + skipLength)..];

            if (!string.IsNullOrEmpty(barcode))
                BarcodeReceived?.Invoke(barcode);
        }
    }

    public void Dispose()
    {
        Close();
    }
}
