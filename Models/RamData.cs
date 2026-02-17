using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class RamData : ObservableObject
{
    [ObservableProperty] private float _usedGb;
    [ObservableProperty] private float _totalGb;
    [ObservableProperty] private float _availableGb;
    [ObservableProperty] private float _load;
}
