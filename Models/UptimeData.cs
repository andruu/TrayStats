using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class UptimeData : ObservableObject
{
    [ObservableProperty] private string _uptime = "--";
    [ObservableProperty] private string _osVersion = "";
    [ObservableProperty] private string _machineName = "";
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private DateTime _bootTime;
}
