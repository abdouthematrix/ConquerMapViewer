using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Domain.Enums;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.WPF.Commands;

namespace ConquerMapViewer.WPF.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IGameMapRepository _gameMapRepository;
    private readonly MapViewerService _mapViewerService;
    private string _statusText = "Ready";
    private GameMap? _selectedMap;
    private string _zoomPercentage = "50%";
    private int _fps;
    private string _searchText = string.Empty;

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

    public MainViewModel(
        IGameMapRepository gameMapRepository,
        MapViewerService mapViewerService)
    {
        _gameMapRepository = gameMapRepository;
        _mapViewerService = mapViewerService;

        AvailableMaps = new ObservableCollection<GameMap>(
            _gameMapRepository.GetAllMaps().Values.OrderBy(m => m.Id)
        );

        FilteredMaps = new ObservableCollection<GameMap>(AvailableMaps);

        Layers = new ObservableCollection<LayerViewModel>
        {
            new LayerViewModel(DrawingAspect.Puzzle, "Puzzle", true),
            new LayerViewModel(DrawingAspect.MapCell, "Map Cells", true),
            new LayerViewModel(DrawingAspect.Portals, "Portals", true),
            new LayerViewModel(DrawingAspect.TerrainObject, "Terrain Objects", true),
            new LayerViewModel(DrawingAspect.Grid, "Grid", false)
        };

        foreach (var layer in Layers)
        {
            layer.PropertyChanged += OnLayerPropertyChanged;
        }

        LoadMapCommand = new RelayCommand<GameMap>(LoadMap, map => map != null);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ResetViewCommand = new RelayCommand(ResetView);
        FitToWindowCommand = new RelayCommand(FitToWindow);
        ToggleLayerCommand = new RelayCommand<LayerViewModel>(ToggleLayer);
        JumpToCoordinateCommand = new RelayCommand(JumpToCoordinate);

        _mapViewerService.ZoomChanged += OnZoomChanged;
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
        var map = _gameMapRepository.GetMapById(1006);
        if (map != null)
        {
            LoadMap(map);
        }
    }

    private void LoadMap(GameMap? map)
    {
        if (map == null) return;

        try
        {
            _mapViewerService.LoadMap(map.Path, map.TileSize);
            StatusText = $"Loaded: {map.DisplayName} (ID: {map.Id}, Tile Size: {map.TileSize})";
            ResetView();
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading map: {ex.Message}";
        }
    }

    private void ZoomIn()
    {
        _mapViewerService.Zoom *= 1.2f;
    }

    private void ZoomOut()
    {
        _mapViewerService.Zoom /= 1.2f;
    }

    private void ResetView()
    {
        _mapViewerService.ResetView();
    }

    private void FitToWindow()
    {
        _mapViewerService.FitToWindow();
    }

    private void ToggleLayer(LayerViewModel? layer)
    {
        if (layer != null)
        {
            layer.IsEnabled = !layer.IsEnabled;
        }
    }

    private void JumpToCoordinate()
    {
        // This would open a dialog to input coordinates
        // For now, just a placeholder
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
