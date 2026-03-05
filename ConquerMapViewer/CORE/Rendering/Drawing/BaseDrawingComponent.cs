namespace ConquerMapViewer.Rendering.Drawing;

public abstract class BaseDrawingComponent : IDrawingComponent, INotifyPropertyChanged
{
    private bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public abstract void UpdateScreen(Rectangle screenRect);
    public abstract void Draw(SpriteBatch spriteBatch, Matrix transformMatrix);
}
