using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Domain.Enums;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.WPF.Commands;
using ConquerMapViewer.WPF.Configuration;
using ConquerMapViewer.WPF.Services;
using Microsoft.Extensions.Logging;
using SharpDX.Direct2D1;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ConquerMapViewer.WPF.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IGameMapRepository _gameMapRepository;
    private readonly MapViewerService _mapViewerService;
    private readonly AppSettingsManager _settingsManager;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ILogger<MainViewModel> _logger;

    private string _statusText = "Ready";
    private GameMap? _selectedMap;
    private string _zoomPercentage = "50%";
    private int _fps;
    private string _searchText = string.Empty;
    private bool _isLoading;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string ZoomPercentage
    {
        get => _zoomPercentage;
        set => SetProperty(ref _zoomPercentage, value);
    }

    public int FPS
    {
        get => _fps;
        set => SetProperty(ref _fps, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterMaps();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public GameMap? SelectedMap
    {
        get => _selectedMap;
        set => SetProperty(ref _selectedMap, value);
    }

    public ObservableCollection<GameMap> AvailableMaps { get; }
    public ObservableCollection<GameMap> FilteredMaps { get; }
    public ObservableCollection<LayerViewModel> Layers { get; }

    public MapViewerService MapViewer => _mapViewerService;

    public ICommand LoadMapCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ResetViewCommand { get; }
    public ICommand FitToWindowCommand { get; }
    public ICommand ToggleLayerCommand { get; }
    public ICommand JumpToCoordinateCommand { get; }
    public ICommand ChangeConquerDirectoryCommand { get; }
    public ICommand ExportScreenshotCommand { get; }

    public MainViewModel(
        IGameMapRepository gameMapRepository,
        MapViewerService mapViewerService,
        AppSettingsManager settingsManager,
        IDialogService dialogService,
        IFileDialogService fileDialogService,
        ILogger<MainViewModel> logger)
    {
        _gameMapRepository = gameMapRepository;
        _mapViewerService = mapViewerService;
        _settingsManager = settingsManager;
        _dialogService = dialogService;
        _fileDialogService = fileDialogService;
        _logger = logger;

        AvailableMaps = new ObservableCollection<GameMap>(
            _gameMapRepository.GetAllMaps().Values.OrderBy(m => m.Id)
        );

        FilteredMaps = new ObservableCollection<GameMap>(AvailableMaps);

        Layers = new ObservableCollection<LayerViewModel>
        {
            new LayerViewModel(DrawingAspect.Backdrop, "Backdrop", true),
            new LayerViewModel(DrawingAspect.Puzzle, "Puzzle", true),            
            new LayerViewModel(DrawingAspect.Portals, "Portals", true),
            new LayerViewModel(DrawingAspect.Scene, "Scene", true),
            new LayerViewModel(DrawingAspect.TerrainObject, "Terrain Objects", true),
            new LayerViewModel(DrawingAspect.MapCell, "Map Cells", false),
            new LayerViewModel(DrawingAspect.BackdropGrid, "Backdrop Grid Overlay", false),
            new LayerViewModel(DrawingAspect.PuzzleGrid, "Puzzle Grid Overlay", false),
            new LayerViewModel(DrawingAspect.SceneGrid, "Scene Grid Overlay", false),
            new LayerViewModel(DrawingAspect.TerrainObjectGrid, "Terrain Grid Overlay", false)
        };

        foreach (var layer in Layers)
        {
            layer.PropertyChanged += OnLayerPropertyChanged;
        }

        LoadMapCommand = new RelayCommand<GameMap>(LoadMap, map => map != null && !IsLoading);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetViewCommand = new RelayCommand(ResetView);
        FitToWindowCommand = new RelayCommand(FitToWindow);
        ToggleLayerCommand = new RelayCommand<LayerViewModel>(ToggleLayer);
        JumpToCoordinateCommand = new RelayCommand(JumpToCoordinate);
        ChangeConquerDirectoryCommand = new RelayCommand(ChangeConquerDirectory);
        ExportScreenshotCommand = new RelayCommand(ExportScreenshot);

        _mapViewerService.ZoomChanged += OnZoomChanged;

        _logger.LogInformation("MainViewModel initialized. Maps loaded: {Count}", AvailableMaps.Count);
    }

    private void FilterMaps()
    {
        FilteredMaps.Clear();
        var filtered = string.IsNullOrWhiteSpace(_searchText)
            ? AvailableMaps
            : AvailableMaps.Where(m =>
                m.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                m.Id.ToString().Contains(_searchText));

        foreach (var map in filtered)
        {
            FilteredMaps.Add(map);
        }
    }

    private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is LayerViewModel layer && e.PropertyName == nameof(LayerViewModel.IsEnabled))
        {
            _mapViewerService.SetLayerEnabled(layer.Aspect, layer.IsEnabled);
        }
    }

    private void OnZoomChanged(object? sender, float zoom)
    {
        ZoomPercentage = $"{zoom * 100:F0}%";
    }

    public void LoadDefaultMap()
    {
        try
        {
            // Try to load last map if setting is enabled
            if (_settingsManager.Settings.LoadLastMap &&
                !string.IsNullOrEmpty(_settingsManager.Settings.LastMapPath))
            {
                var lastMap = AvailableMaps.FirstOrDefault(m => m.Path == _settingsManager.Settings.LastMapPath);
                if (lastMap != null)
                {
                    LoadMap(lastMap);
                    return;
                }
            }

            // Fall back to default map
            var map = _gameMapRepository.GetMapById(_settingsManager.Settings.DefaultMapId);
            if (map != null)
            {
                LoadMap(map);
            }
            else
            {
                _logger.LogWarning("Default map not found");
                StatusText = "No default map available. Please select a map from the list.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading default map");
            _dialogService.ShowError($"Failed to load default map: {ex.Message}");
        }
    }

    private void LoadMap(GameMap? map)
    {
        if (map == null || IsLoading) return;

        IsLoading = true;
        StatusText = $"Loading {map.DisplayName}...";

        try
        {
            _mapViewerService.LoadMap(map.Path, map.TileSize);
            foreach (var layer in Layers)            
                _mapViewerService.SetLayerEnabled(layer.Aspect, layer.IsEnabled);            
            StatusText = $"Loaded: {map.DisplayName} (ID: {map.Id}, Tile: {map.TileSize}px)";
            ResetView();

            _settingsManager.UpdateLastMap(map.Path);
            _logger.LogInformation("Map loaded successfully: {MapName}", map.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading map: {MapName}", map.DisplayName);
            _dialogService.ShowError($"Failed to load map '{map.DisplayName}': {ex.Message}");
            StatusText = "Error loading map. See log for details.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ZoomIn() => _mapViewerService.Zoom *= 1.2f;
    private void ZoomOut() => _mapViewerService.Zoom /= 1.2f;
    private void ResetView() => _mapViewerService.ResetView();
    private void FitToWindow() => _mapViewerService.FitToWindow();

    private void ToggleLayer(LayerViewModel? layer)
    {
        if (layer != null)
        {
            layer.IsEnabled = !layer.IsEnabled;
        }
    }

    private void JumpToCoordinate()
    {
        var input = _dialogService.ShowInputDialog(
            "Enter map coordinates (X,Y):",
            "Jump to Coordinate",
            "0,0"
        );

        if (string.IsNullOrEmpty(input)) return;

        try
        {
            var parts = input.Split(',');
            if (parts.Length == 2 &&
                float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y))
            {
                _mapViewerService.JumpToMapCoordinate(new Microsoft.Xna.Framework.Vector2(x, y));
                StatusText = $"Jumped to ({x}, {y})";
            }
            else
            {
                _dialogService.ShowWarning("Invalid coordinate format. Use: X,Y");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error jumping to coordinate");
            _dialogService.ShowError($"Failed to jump to coordinate: {ex.Message}");
        }
    }

    private void ChangeConquerDirectory()
    {
        var folder = _fileDialogService.OpenFolder("Select Conquer Online Directory");
        if (!string.IsNullOrEmpty(folder))
        {
            if (_dialogService.ShowConfirmation(
                "Changing the directory will restart the application. Continue?",
                "Confirm Directory Change"))
            {
                _settingsManager.UpdateConquerDirectory(folder);
                _dialogService.ShowInfo("Directory updated. Please restart the application.");
                // Optionally trigger application restart
            }
        }
    }

    private void ExportScreenshot()
    {
        var fileName = _fileDialogService.SaveFile(
            "Export Screenshot",
            "PNG Image (*.png)|*.png",
            $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        );

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                // TODO: Implement screenshot export
                _dialogService.ShowInfo($"Screenshot would be saved to: {fileName}");
                _logger.LogInformation("Screenshot exported to: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting screenshot");
                _dialogService.ShowError($"Failed to export screenshot: {ex.Message}");
            }
        }
    }

    public void UpdateFPS(int fps)
    {
        FPS = fps;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
