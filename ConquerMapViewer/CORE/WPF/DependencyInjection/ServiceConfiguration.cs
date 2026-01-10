using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Core.Services;
using ConquerMapViewer.Infrastructure.Repositories;
using ConquerMapViewer.WPF.Configuration;
using ConquerMapViewer.WPF.Services;
using ConquerMapViewer.WPF.ViewModels;
using ConquerMapViewer.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ConquerMapViewer.WPF.DependencyInjection;

public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configuration
        services.AddSingleton<AppSettingsManager>();
        services.AddSingleton(sp => sp.GetRequiredService<AppSettingsManager>().Settings);

        // Infrastructure - using factory pattern for better error handling
        services.AddSingleton<IPackageReader>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            ValidateDirectory(settings.ConquerDirectory, "Conquer directory");
            return new TqPackageReader(settings.ConquerDirectory);
        });

        services.AddSingleton<IAniDictionary>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            ValidateDirectory(settings.ConquerDirectory, "Conquer directory");
            return new AniDictionary(settings.ConquerDirectory);
        });

        services.AddSingleton<IMapFileLoader, MapFileLoader>();

        services.AddSingleton<IPuzzleFileLoader>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            ValidateDirectory(settings.ConquerDirectory, "Conquer directory");
            return new PuzzleFileLoader(settings.ConquerDirectory);
        });

        services.AddSingleton<IGameMapRepository>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var gameMapPath = settings.GameMapFilePath;

            if (!File.Exists(gameMapPath))
            {
                var logger = sp.GetRequiredService<ILogger<GameMapRepository>>();
                logger.LogError("GameMap file not found at: {Path}", gameMapPath);
                throw new FileNotFoundException($"GameMap file not found: {gameMapPath}");
            }

            return new GameMapRepository(gameMapPath);
        });

        // Application services
        services.AddSingleton<MapLoadingService>();
        services.AddSingleton<MapViewerService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        // UI Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        return services.BuildServiceProvider();
    }

    private static void ValidateDirectory(string path, string description)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException(
                $"{description} has not been configured. Please run the setup wizard."
            );
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"{description} not found at: {path}. " +
                $"Please verify the path in settings or run the setup wizard again."
            );
        }
    }
}
