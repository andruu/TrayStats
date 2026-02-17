using CommunityToolkit.Mvvm.ComponentModel;

namespace TrayStats.Models;

public partial class NetAdapterData : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _speed = string.Empty;
    [ObservableProperty] private bool _isActive;
}

public partial class NetData : ObservableObject
{
    [ObservableProperty] private double _downloadBytesPerSec;
    [ObservableProperty] private double _uploadBytesPerSec;
    [ObservableProperty] private long _totalDownloaded;
    [ObservableProperty] private long _totalUploaded;

    public List<NetAdapterData> Adapters { get; set; } = new();

    public string DownloadFormatted => FormatSpeed(DownloadBytesPerSec);
    public string UploadFormatted => FormatSpeed(UploadBytesPerSec);
    public string TotalDownloadedFormatted => FormatBytes(TotalDownloaded);
    public string TotalUploadedFormatted => FormatBytes(TotalUploaded);

    partial void OnDownloadBytesPerSecChanged(double value)
    {
        OnPropertyChanged(nameof(DownloadFormatted));
    }

    partial void OnUploadBytesPerSecChanged(double value)
    {
        OnPropertyChanged(nameof(UploadFormatted));
    }

    partial void OnTotalDownloadedChanged(long value)
    {
        OnPropertyChanged(nameof(TotalDownloadedFormatted));
    }

    partial void OnTotalUploadedChanged(long value)
    {
        OnPropertyChanged(nameof(TotalUploadedFormatted));
    }

    public static string FormatSpeed(double bytesPerSec)
    {
        if (bytesPerSec < 1024)
            return $"{bytesPerSec:F0} B/s";
        if (bytesPerSec < 1024 * 1024)
            return $"{bytesPerSec / 1024:F1} KB/s";
        if (bytesPerSec < 1024 * 1024 * 1024)
            return $"{bytesPerSec / (1024 * 1024):F1} MB/s";
        return $"{bytesPerSec / (1024 * 1024 * 1024):F2} GB/s";
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
