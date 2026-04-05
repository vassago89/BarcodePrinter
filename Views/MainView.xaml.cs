using System.Windows;
using BarcodePrinter.ViewModels;

namespace BarcodePrinter.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        (DataContext as MainViewModel)?.Dispose();
        base.OnClosed(e);
    }
}
