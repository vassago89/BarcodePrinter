using System.IO;
using System.Text.Json;
using System.Windows;
using BarcodePrinter.Models;

namespace BarcodePrinter.Views;

public partial class SettingsDialog : Window
{
    private readonly string _configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppConfig? UpdatedConfig { get; private set; }

    public SettingsDialog(AppConfig config)
    {
        InitializeComponent();
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        LoadValues(config);
    }

    private void LoadValues(AppConfig c)
    {
        // PLC
        TbPlcIp.Text = c.Plc.IpAddress;
        TbPlcPort.Text = c.Plc.Port.ToString();
        TbPlcSlaveId.Text = c.Plc.SlaveId.ToString();
        TbPlcPolling.Text = c.Plc.PollingIntervalMs.ToString();
        TbPlcResultReg.Text = c.Plc.ResultRegister.ToString();
        TbPlcTriggerReg.Text = c.Plc.PrintTriggerRegister.ToString();
        TbPlcCompleteReg.Text = c.Plc.PrintCompleteRegister.ToString();

        // Serial
        TbSerialPort.Text = c.Serial.PortName;
        TbSerialBaud.Text = c.Serial.BaudRate.ToString();
        TbSerialDataBits.Text = c.Serial.DataBits.ToString();
        TbSerialParity.Text = c.Serial.Parity;
        TbSerialStopBits.Text = c.Serial.StopBits;

        // Printer
        TbPrnIp.Text = c.Printer.IpAddress;
        TbPrnPort.Text = c.Printer.Port.ToString();
        TbPrnQrMag.Text = c.Printer.QrMagnification.ToString();
        TbPrnOriginX.Text = c.Printer.OriginX.ToString();
        TbPrnOriginY.Text = c.Printer.OriginY.ToString();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = new AppConfig
            {
                Plc = new PlcConfig
                {
                    IpAddress = TbPlcIp.Text.Trim(),
                    Port = int.Parse(TbPlcPort.Text.Trim()),
                    SlaveId = byte.Parse(TbPlcSlaveId.Text.Trim()),
                    PollingIntervalMs = int.Parse(TbPlcPolling.Text.Trim()),
                    ResultRegister = ushort.Parse(TbPlcResultReg.Text.Trim()),
                    PrintTriggerRegister = ushort.Parse(TbPlcTriggerReg.Text.Trim()),
                    PrintCompleteRegister = ushort.Parse(TbPlcCompleteReg.Text.Trim())
                },
                Serial = new SerialConfig
                {
                    PortName = TbSerialPort.Text.Trim(),
                    BaudRate = int.Parse(TbSerialBaud.Text.Trim()),
                    DataBits = int.Parse(TbSerialDataBits.Text.Trim()),
                    Parity = TbSerialParity.Text.Trim(),
                    StopBits = TbSerialStopBits.Text.Trim()
                },
                Printer = new PrinterConfig
                {
                    IpAddress = TbPrnIp.Text.Trim(),
                    Port = int.Parse(TbPrnPort.Text.Trim()),
                    QrMagnification = int.Parse(TbPrnQrMag.Text.Trim()),
                    OriginX = int.Parse(TbPrnOriginX.Text.Trim()),
                    OriginY = int.Parse(TbPrnOriginY.Text.Trim())
                }
            };

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configPath, json);

            UpdatedConfig = config;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"입력값 오류: {ex.Message}", "저장 실패",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
