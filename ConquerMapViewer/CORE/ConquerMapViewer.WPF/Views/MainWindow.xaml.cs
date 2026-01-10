namespace ConquerMapViewer.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        GameControl.MapViewerService = _viewModel.MapViewer;
        GameControl.StatusChanged += OnStatusChanged;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _viewModel.LoadDefaultMap();
    }

    private void OnStatusChanged(object? sender, string status)
    {
        _viewModel.StatusText = status;
    }

    protected override void OnClosed(EventArgs e)
    {
        GameControl.StatusChanged -= OnStatusChanged;
        base.OnClosed(e);
    }
}
