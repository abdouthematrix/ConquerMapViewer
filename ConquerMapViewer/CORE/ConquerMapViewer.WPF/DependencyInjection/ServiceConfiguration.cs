namespace ConquerMapViewer.WPF.DependencyInjection;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        const string conquerDirectory = @"C:\Users\AbdouMatrix\Downloads\CO\6090\";

        // Infrastructure        
        services.AddSingleton<IPackageReader>(sp => new TqPackageReader(conquerDirectory));
        services.AddSingleton<IAniDictionary>(sp => new AniDictionary(conquerDirectory));
        services.AddSingleton<IMapFileLoader, MapFileLoader>();
        services.AddSingleton<IPuzzleFileLoader>(sp => new PuzzleFileLoader(conquerDirectory));
        services.AddSingleton<IGameMapRepository>(sp => new GameMapRepository(
            System.IO.Path.Combine(conquerDirectory, "ini/gamemap.dat")
        ));

        // Application services
        services.AddSingleton<MapLoadingService>();
        services.AddTransient<MapViewerService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }
}
