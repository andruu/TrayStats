using System.Windows;
using System.Windows.Input;
using TrayStats.ViewModels;

namespace TrayStats.Views;

public partial class DashboardPopup : Window
{
    private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;
    private DateTime _lastDeactivated;

    public DashboardPopup()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        DataContextChanged += OnDataContextChanged;
    }

    public bool WasJustDeactivated => (DateTime.UtcNow - _lastDeactivated).TotalMilliseconds < 300;

    public event Action? DashboardHidden;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DashboardViewModel oldVm)
            oldVm.InvalidateCharts -= OnInvalidateCharts;

        if (e.NewValue is DashboardViewModel newVm)
            newVm.InvalidateCharts += OnInvalidateCharts;
    }

    private void OnInvalidateCharts()
    {
        CpuChart.InvalidateValues();
        GpuChart.InvalidateValues();
        RamChart.InvalidateValues();
        BatteryChart.InvalidateValues();
        NetDownChart.InvalidateValues();
        NetUpChart.InvalidateValues();
    }

    public void ShowAtTray()
    {
        ApplyMaxHeight();
        PositionNearTray();
        Show();
        Activate();
    }

    private void ApplyMaxHeight()
    {
        var workArea = SystemParameters.WorkArea;
        MaxHeight = workArea.Height - 20;
    }

    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;

        Left = workArea.Right - Width - 8;

        double height = ActualHeight > 0 ? ActualHeight : 400;
        Top = workArea.Bottom - height - 8;

        // Clamp so the top never goes above the work area
        if (Top < workArea.Top)
            Top = workArea.Top + 4;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsVisible)
            PositionNearTray();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        PositionNearTray();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        _lastDeactivated = DateTime.UtcNow;
        Hide();
        DashboardHidden?.Invoke();
    }

    private void WeatherRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleWeatherDetailCommand.Execute(null);
    }

    private void CpuRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleCpuDetailCommand.Execute(null);
    }

    private void GpuRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleGpuDetailCommand.Execute(null);
    }

    private void RamRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleRamDetailCommand.Execute(null);
    }

    private void DiskRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleDiskDetailCommand.Execute(null);
    }

    private void BatteryRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleBatteryDetailCommand.Execute(null);
    }

    private void NetRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleNetDetailCommand.Execute(null);
    }

    private void ProcessesRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleProcessesDetailCommand.Execute(null);
    }

    private void BluetoothRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleBluetoothDetailCommand.Execute(null);
    }

    private void UptimeRow_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.ToggleUptimeDetailCommand.Execute(null);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        DashboardHidden?.Invoke();
    }
}
