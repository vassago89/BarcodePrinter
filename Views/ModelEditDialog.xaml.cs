using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BarcodePrinter.Services;

namespace BarcodePrinter.Views;

public partial class ModelEditDialog : Window
{
    private readonly SerialBarcodeReader? _reader;
    private TextBox? _scanTarget;
    private Button? _activeButton;

    public string ModelName => TbModelName.Text.Trim();
    public string Barcode1Text => TbBarcode1.Text.Trim();
    public string Barcode2Text => TbBarcode2.Text.Trim();
    public string QrDataText => TbQrData.Text.Trim();

    public ModelEditDialog(SerialBarcodeReader? reader = null)
    {
        InitializeComponent();
        _reader = reader;

        if (_reader != null)
        {
            _reader.BarcodeReceived += OnBarcodeScanned;
        }
        else
        {
            BtnScan1.IsEnabled = false;
            BtnScan2.IsEnabled = false;
            BtnScanQr.IsEnabled = false;
            BtnScan1.Opacity = 0.4;
            BtnScan2.Opacity = 0.4;
            BtnScanQr.Opacity = 0.4;
            TbScanStatus.Text = "리더 미연결 - 직접 입력";
        }

        TbModelName.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_reader != null)
            _reader.BarcodeReceived -= OnBarcodeScanned;
        base.OnClosed(e);
    }

    private void SetScanTarget(TextBox target, Button button)
    {
        // Reset previous
        if (_activeButton != null)
        {
            _activeButton.Content = "스캔";
            _activeButton.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
        }

        if (_scanTarget == target)
        {
            // Toggle off
            _scanTarget = null;
            _activeButton = null;
            TbScanStatus.Text = "";
            return;
        }

        _scanTarget = target;
        _activeButton = button;
        button.Content = "대기...";
        button.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
        TbScanStatus.Text = "바코드를 스캔하세요...";
    }

    private void OnBarcodeScanned(string barcode)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_scanTarget == null) return;

            _scanTarget.Text = barcode;

            if (_activeButton != null)
            {
                _activeButton.Content = "스캔";
                _activeButton.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
            }

            TbScanStatus.Text = $"스캔 완료: {barcode}";
            _scanTarget = null;
            _activeButton = null;
        });
    }

    private void OnScanBarcode1(object sender, RoutedEventArgs e) => SetScanTarget(TbBarcode1, BtnScan1);
    private void OnScanBarcode2(object sender, RoutedEventArgs e) => SetScanTarget(TbBarcode2, BtnScan2);
    private void OnScanQrData(object sender, RoutedEventArgs e) => SetScanTarget(TbQrData, BtnScanQr);

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ModelName))
        {
            MessageBox.Show("모델명을 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            TbModelName.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(Barcode1Text))
        {
            MessageBox.Show("부품 1 바코드를 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(Barcode2Text))
        {
            MessageBox.Show("부품 2 바코드를 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(QrDataText))
        {
            MessageBox.Show("QR 데이터를 입력하세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
