using System.Windows;
using ConquerMapViewer.WPF.Configuration;
using ConquerMapViewer.WPF.DependencyInjection;
using ConquerMapViewer.WPF.Services;
using ConquerMapViewer.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConquerMapViewer.WPF;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Check for first run and configure directory
            var settingsManager = new AppSettingsManager();

            if (settingsManager.Settings.IsFirstRun || !settingsManager.Settings.IsValidConfiguration())
            {
                var welcomeDialog = new WelcomeDialog();
                var result = welcomeDialog.ShowDialog();

                if (result != true || string.IsNullOrEmpty(welcomeDialog.SelectedDirectory))
                {
                    MessageBox.Show(
                        "Conquer Online directory is required to run the application.",
                        "Configuration Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    Shutdown();
                    return;
                }

                settingsManager.UpdateConquerDirectory(welcomeDialog.SelectedDirectory);
            }

            // Now configure services with the valid directory
            _serviceProvider = ServiceConfiguration.ConfigureServices();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start application: {ex.Message}\n\nPlease check your configuration and try again.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
