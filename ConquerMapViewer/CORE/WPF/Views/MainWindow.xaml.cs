using System.Windows;
using ConquerMapViewer.WPF.ViewModels;

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
        GameControl.FPSChanged += OnFPSChanged;
    }

    // Add this method to the partial class:

    private void MapList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedMap != null)
        {
            _viewModel.LoadMapCommand.Execute(_viewModel.SelectedMap);
        }
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

    private void OnFPSChanged(object? sender, int fps)
    {
        _viewModel.UpdateFPS(fps);
    }

    protected override void OnClosed(EventArgs e)
    {
        GameControl.StatusChanged -= OnStatusChanged;
        GameControl.FPSChanged -= OnFPSChanged;
        base.OnClosed(e);
    }
}
