namespace BarcodePrinter.Models;

public class AppConfig
{
    public PlcConfig Plc { get; set; } = new();
    public SerialConfig Serial { get; set; } = new();
    public PrinterConfig Printer { get; set; } = new();
}

public class PlcConfig
{
    public string IpAddress { get; set; } = "192.168.1.10";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;
    public ushort ResultRegister { get; set; } = 100;
    public ushort PrintTriggerRegister { get; set; } = 200;
    public ushort PrintCompleteRegister { get; set; } = 201;
    public int PollingIntervalMs { get; set; } = 200;
}

public class SerialConfig
{
    public string PortName { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string Parity { get; set; } = "None";
    public string StopBits { get; set; } = "One";
}

public class PrinterConfig
{
    public string IpAddress { get; set; } = "192.168.1.20";
    public int Port { get; set; } = 9100;
    public int QrMagnification { get; set; } = 10;
    public int OriginX { get; set; } = 50;
    public int OriginY { get; set; } = 50;
}
