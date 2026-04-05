using System.IO;
using System.Net.Sockets;

namespace BarcodePrinter.Services;

public class ModbusTcpService : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _transactionId;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsConnected => _client?.Connected ?? false;

    public async Task ConnectAsync(string ipAddress, int port, CancellationToken ct = default)
    {
        Disconnect();
        _client = new TcpClient
        {
            ReceiveTimeout = 3000,
            SendTimeout = 3000
        };
        await _client.ConnectAsync(ipAddress, port, ct);
        _stream = _client.GetStream();
        _transactionId = 0;
    }

    public void Disconnect()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort quantity, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        await _semaphore.WaitAsync(ct);
        try
        {
            _transactionId++;
            byte[] request =
            [
                (byte)(_transactionId >> 8), (byte)(_transactionId & 0xFF),
                0x00, 0x00,
                0x00, 0x06,
                unitId,
                0x03,
                (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                (byte)(quantity >> 8), (byte)(quantity & 0xFF)
            ];

            await _stream.WriteAsync(request, ct);

            var header = new byte[7];
            await ReadExactAsync(header, 7, ct);

            int pduLength = (header[4] << 8) | header[5];
            var pdu = new byte[pduLength - 1];
            await ReadExactAsync(pdu, pdu.Length, ct);

            if (pdu[0] == 0x83)
                throw new IOException($"Modbus error code: 0x{pdu[1]:X2}");

            var registers = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
                registers[i] = (ushort)((pdu[2 + i * 2] << 8) | pdu[3 + i * 2]);

            return registers;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected");

        await _semaphore.WaitAsync(ct);
        try
        {
            _transactionId++;
            byte[] request =
            [
                (byte)(_transactionId >> 8), (byte)(_transactionId & 0xFF),
                0x00, 0x00,
                0x00, 0x06,
                unitId,
                0x06,
                (byte)(address >> 8), (byte)(address & 0xFF),
                (byte)(value >> 8), (byte)(value & 0xFF)
            ];

            await _stream.WriteAsync(request, ct);

            var response = new byte[12];
            await ReadExactAsync(response, 12, ct);

            if (response[7] == 0x86)
                throw new IOException($"Modbus write error: 0x{response[8]:X2}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ReadExactAsync(byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await _stream!.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) throw new IOException("Connection closed by remote host");
            offset += read;
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        Disconnect();
    }
}
