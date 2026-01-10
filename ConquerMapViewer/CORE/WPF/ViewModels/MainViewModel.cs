namespace ConquerMapViewer.WPF.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IGameMapRepository _gameMapRepository;
    private readonly MapViewerService _mapViewerService;
    private string _statusText = "Ready";

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public MapViewerService MapViewer => _mapViewerService;

    public MainViewModel(
        IGameMapRepository gameMapRepository,
        MapViewerService mapViewerService)
    {
        _gameMapRepository = gameMapRepository;
        _mapViewerService = mapViewerService;
    }

    public void LoadDefaultMap()
    {
        var map = _gameMapRepository.GetMapById(1006);
        if (map != null)
        {
            _mapViewerService.LoadMap(map.Path, map.TileSize);
            StatusText = $"Loaded map: {map.Path}";
        }
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
