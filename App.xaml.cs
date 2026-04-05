using System.IO;
using System.Text.Json;
using System.Windows;
using BarcodePrinter.Models;
using BarcodePrinter.ViewModels;
using BarcodePrinter.Views;

namespace BarcodePrinter;

public partial class App : Application
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        AppConfig config;

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        else
        {
            config = new AppConfig();
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonOptions));
        }

        var viewModel = new MainViewModel(config);
        var mainView = new MainView { DataContext = viewModel };
        mainView.Show();
    }
}
