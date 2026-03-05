namespace ConquerMapViewer.WPF.ViewModels;

public sealed class LayerViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;

    public DrawingAspect Aspect { get; }
    public string Name { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public LayerViewModel(DrawingAspect aspect, string name, bool isEnabled = true)
    {
        Aspect = aspect;
        Name = name;
        _isEnabled = isEnabled;
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
