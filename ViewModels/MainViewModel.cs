using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarcodePrinter.Models;
using BarcodePrinter.Services;
using BarcodePrinter.Views;
using QRCoder;

namespace BarcodePrinter.ViewModels;

public enum ScanState
{
    Idle,
    WaitingForScan1,
    WaitingForScan2,
    CompareOk,
    CompareNg,
    Printing
}

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly PlcService _plc;
    private readonly SerialBarcodeReader _reader;
    private readonly ZebraPrinterService _printer;
    private readonly MappingService _mapping;
    private readonly AppConfig _config;

    // Model selection
    public ObservableCollection<string> ModelNames { get; } = [];
    [ObservableProperty] private string? _selectedModelName;
    private BarcodeMapping? _selectedMapping;
    private bool _pauseScanning;

    // Expected barcodes (from selected model)
    [ObservableProperty] private string _expectedBarcode1 = "";
    [ObservableProperty] private string _expectedBarcode2 = "";
    [ObservableProperty] private string _expectedQrData = "";

    // Barcode scan
    [ObservableProperty] private string _barcode1 = "";
    [ObservableProperty] private string _barcode2 = "";
    [ObservableProperty] private bool _isBarcode1Scanned;
    [ObservableProperty] private bool _isBarcode2Scanned;
    [ObservableProperty] private bool _isBarcode1Match;
    [ObservableProperty] private bool _isBarcode2Match;

    // Status
    [ObservableProperty] private string _statusMessage = "모델을 선택하고 연결하세요";
    [ObservableProperty] private string _comparisonResult = "";
    [ObservableProperty] private ScanState _currentState = ScanState.Idle;
    [ObservableProperty] private BitmapImage? _qrImage;
    [ObservableProperty] private bool _isPlcConnected;
    [ObservableProperty] private bool _isReaderConnected;
    [ObservableProperty] private bool _isPrinterConnected;
    [ObservableProperty] private bool _isConnecting;

    public string StateDisplay => CurrentState switch
    {
        ScanState.Idle => "대기",
        ScanState.WaitingForScan1 => "부품1 대기",
        ScanState.WaitingForScan2 => "부품2 대기",
        ScanState.CompareOk => "OK",
        ScanState.CompareNg => "NG",
        ScanState.Printing => "출력 중",
        _ => ""
    };

    partial void OnCurrentStateChanged(ScanState value) => OnPropertyChanged(nameof(StateDisplay));

    partial void OnSelectedModelNameChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _selectedMapping = null;
            ExpectedBarcode1 = "";
            ExpectedBarcode2 = "";
            ExpectedQrData = "";
            return;
        }

        _selectedMapping = _mapping.GetByModel(value);
        if (_selectedMapping == null) return;

        ExpectedBarcode1 = _selectedMapping.Barcode1;
        ExpectedBarcode2 = _selectedMapping.Barcode2;
        ExpectedQrData = _selectedMapping.QrData;
        ClearScanState();
        CurrentState = ScanState.WaitingForScan1;
        StatusMessage = $"모델 [{value}] 선택 - 부품 1을 스캔하세요";
    }

    public MainViewModel(AppConfig config)
    {
        _config = config;
        _plc = new PlcService(config.Plc);
        _reader = new SerialBarcodeReader();
        _printer = new ZebraPrinterService(config.Printer);
        _mapping = new MappingService(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mappings.json"));

        _plc.ConnectionChanged += c => Dispatch(() => IsPlcConnected = c);
        _plc.PrintTriggered += OnPrintTriggered;
        _plc.ErrorOccurred += msg => Dispatch(() => StatusMessage = msg);

        _reader.ConnectionChanged += c => Dispatch(() => IsReaderConnected = c);
        _reader.BarcodeReceived += bc => Dispatch(() => ProcessBarcode(bc));

        _printer.ConnectionChanged += c => Dispatch(() => IsPrinterConnected = c);

        _mapping.Load();
        foreach (var name in _mapping.GetAllModelNames())
            ModelNames.Add(name);
    }

    [RelayCommand]
    private async Task ConnectAllAsync()
    {
        if (IsConnecting) return;
        IsConnecting = true;
        StatusMessage = "장치 연결 중...";

        if (!IsPlcConnected)
        {
            try { await _plc.ConnectAsync(); _plc.StartPolling(); }
            catch (Exception ex) { StatusMessage = $"PLC 연결 실패: {ex.Message}"; }
        }

        if (!IsReaderConnected)
        {
            try { _reader.Open(_config.Serial); }
            catch (Exception ex) { StatusMessage = $"리더 연결 실패: {ex.Message}"; }
        }

        if (!IsPrinterConnected)
        {
            try { await _printer.ConnectAsync(); }
            catch (Exception ex) { StatusMessage = $"프린터 연결 실패: {ex.Message}"; }
        }

        IsConnecting = false;

        if (IsPlcConnected && IsReaderConnected && IsPrinterConnected)
            StatusMessage = _selectedMapping != null
                ? "모든 장치 연결 완료 - 부품 1을 스캔하세요"
                : "모든 장치 연결 완료 - 모델을 선택하세요";
        else if (IsReaderConnected)
            StatusMessage = "일부 장치 미연결 (바코드 스캔 가능)";
    }

    [RelayCommand]
    private void DisconnectAll()
    {
        _plc.Disconnect();
        _reader.Close();
        _printer.Disconnect();
        StatusMessage = "장치 연결 해제";
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        ClearScanState();

        if (_selectedMapping != null)
        {
            CurrentState = ScanState.WaitingForScan1;
            StatusMessage = $"초기화 - 부품 1을 스캔하세요";
        }
        else
        {
            CurrentState = ScanState.Idle;
            StatusMessage = "초기화 완료 - 모델을 선택하세요";
        }

        if (IsPlcConnected)
        {
            try { await _plc.ClearResultAsync(); } catch { }
        }
    }

    [RelayCommand]
    private void AddModel()
    {
        var dialog = new ModelEditDialog(_reader)
        {
            Owner = Application.Current.MainWindow
        };

        _pauseScanning = true;
        try
        {
            if (dialog.ShowDialog() != true) return;

            if (_mapping.GetByModel(dialog.ModelName) != null)
            {
                MessageBox.Show($"모델 [{dialog.ModelName}]이 이미 존재합니다.", "중복",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mapping = new BarcodeMapping
            {
                ModelName = dialog.ModelName,
                Barcode1 = dialog.Barcode1Text,
                Barcode2 = dialog.Barcode2Text,
                QrData = dialog.QrDataText
            };

            _mapping.Add(mapping);
            ModelNames.Add(mapping.ModelName);
            SelectedModelName = mapping.ModelName;
            StatusMessage = $"모델 [{mapping.ModelName}] 추가됨";
        }
        finally
        {
            _pauseScanning = false;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var dialog = new SettingsDialog(_config)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            StatusMessage = "설정이 저장되었습니다. 재시작 후 적용됩니다.";
        }
    }

    [RelayCommand]
    private async Task DeleteModelAsync()
    {
        if (SelectedModelName == null)
        {
            StatusMessage = "삭제할 모델을 선택하세요";
            return;
        }

        var result = MessageBox.Show(
            $"모델 [{SelectedModelName}]을(를) 삭제하시겠습니까?",
            "모델 삭제", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        string removed = SelectedModelName;
        _mapping.Remove(removed);
        ModelNames.Remove(removed);
        SelectedModelName = null;
        _selectedMapping = null;
        ExpectedBarcode1 = "";
        ExpectedBarcode2 = "";
        ExpectedQrData = "";
        ClearScanState();
        CurrentState = ScanState.Idle;
        StatusMessage = $"모델 [{removed}] 삭제됨";

        if (IsPlcConnected)
        {
            try { await _plc.ClearResultAsync(); } catch { }
        }
    }

    [RelayCommand]
    private async Task ManualPrintAsync()
    {
        if (_selectedMapping == null || ComparisonResult != "OK")
        {
            StatusMessage = "OK 판정 후 출력 가능합니다";
            return;
        }
        await ExecutePrintAsync();
    }

    private async void ProcessBarcode(string barcode)
    {
        if (_pauseScanning) return;

        if (_selectedMapping == null)
        {
            StatusMessage = "모델을 먼저 선택하세요";
            return;
        }

        switch (CurrentState)
        {
            case ScanState.WaitingForScan1:
                Barcode1 = barcode;
                IsBarcode1Scanned = true;
                IsBarcode1Match = barcode == _selectedMapping.Barcode1;
                CurrentState = ScanState.WaitingForScan2;
                StatusMessage = IsBarcode1Match
                    ? $"부품 1 일치 - 부품 2를 스캔하세요"
                    : $"부품 1 불일치! (예상: {_selectedMapping.Barcode1}) - 부품 2를 스캔하세요";
                break;

            case ScanState.WaitingForScan2:
                Barcode2 = barcode;
                IsBarcode2Scanned = true;
                IsBarcode2Match = barcode == _selectedMapping.Barcode2;
                await EvaluateResultAsync();
                break;

            default:
                StatusMessage = "초기화 후 다시 스캔하세요";
                break;
        }
    }

    private async Task EvaluateResultAsync()
    {
        if (IsBarcode1Match && IsBarcode2Match)
        {
            CurrentState = ScanState.CompareOk;
            ComparisonResult = "OK";
            QrImage = GenerateQrImage(_selectedMapping!.QrData);
            StatusMessage = $"판정 OK - 모델: {_selectedMapping.ModelName}";

            if (IsPlcConnected)
            {
                try { await _plc.WriteResultAsync(true); }
                catch (Exception ex) { StatusMessage = $"PLC 쓰기 실패: {ex.Message}"; }
            }
        }
        else
        {
            CurrentState = ScanState.CompareNg;
            ComparisonResult = "NG";
            QrImage = null;

            var detail = (!IsBarcode1Match, !IsBarcode2Match) switch
            {
                (true, true) => "부품 1, 2 모두 불일치",
                (true, false) => "부품 1 불일치",
                (false, true) => "부품 2 불일치",
                _ => ""
            };
            StatusMessage = $"판정 NG - {detail}";

            if (IsPlcConnected)
            {
                try { await _plc.WriteResultAsync(false); }
                catch (Exception ex) { StatusMessage = $"PLC 쓰기 실패: {ex.Message}"; }
            }
        }
    }

    private void OnPrintTriggered()
    {
        Dispatch(async () =>
        {
            try
            {
                if (_selectedMapping == null || ComparisonResult != "OK")
                {
                    StatusMessage = "PLC 출력 요청 - OK 판정 필요";
                    return;
                }
                await ExecutePrintAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"출력 오류: {ex.Message}";
            }
        });
    }

    private async Task ExecutePrintAsync()
    {
        if (_selectedMapping == null) return;

        var prevState = CurrentState;
        CurrentState = ScanState.Printing;
        StatusMessage = $"QR 코드 출력 중... [{_selectedMapping.ModelName}]";

        try
        {
            if (!IsPrinterConnected)
            {
                StatusMessage = "프린터 미연결";
                CurrentState = prevState;
                return;
            }

            await _printer.PrintQrCodeAsync(_selectedMapping.QrData, _selectedMapping.ModelName);

            if (IsPlcConnected)
            {
                try { await _plc.WritePrintCompleteAsync(); } catch { }
            }

            StatusMessage = $"출력 완료 - {_selectedMapping.ModelName}";
            await Task.Delay(2000);
            await ResetAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"출력 실패: {ex.Message}";
            CurrentState = ScanState.CompareOk;
        }
    }

    private void ClearScanState()
    {
        Barcode1 = "";
        Barcode2 = "";
        IsBarcode1Scanned = false;
        IsBarcode2Scanned = false;
        IsBarcode1Match = false;
        IsBarcode2Match = false;
        ComparisonResult = "";
        QrImage = null;
    }

    private static BitmapImage GenerateQrImage(string data)
    {
        using var gen = new QRCodeGenerator();
        using var qrData = gen.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] png = qrCode.GetGraphic(20);

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = new MemoryStream(png);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static void Dispatch(Action action)
    {
        if (Application.Current?.Dispatcher.CheckAccess() == true)
            action();
        else
            Application.Current?.Dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        _plc.Dispose();
        _reader.Dispose();
        _printer.Dispose();
    }
}
