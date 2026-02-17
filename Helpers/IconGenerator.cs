using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace TrayStats.Helpers;

public enum IconStyle
{
    Bar,
    Percentage,
    MiniChart
}

public enum TrayMetric
{
    CPU,
    GPU,
    RAM
}

public static class IconGenerator
{
    private static readonly Color BgColor = Color.FromArgb(30, 30, 30);
    private static readonly Color BarColorLow = Color.FromArgb(76, 175, 80);
    private static readonly Color BarColorMid = Color.FromArgb(255, 193, 7);
    private static readonly Color BarColorHigh = Color.FromArgb(244, 67, 54);

    // Rolling history for the mini chart icon
    private static readonly float[] _history = new float[16];
    private static int _historyIndex;

    public static Icon CreateIcon(float cpuPercent, IconStyle style)
    {
        return style switch
        {
            IconStyle.Percentage => CreatePercentageIcon(cpuPercent),
            IconStyle.MiniChart => CreateMiniChartIcon(cpuPercent),
            _ => CreateBarIcon(cpuPercent)
        };
    }

    private static Color GetColor(float percent)
    {
        return percent switch
        {
            > 85 => BarColorHigh,
            > 60 => BarColorMid,
            _ => BarColorLow
        };
    }

    private static Icon CreateBarIcon(float cpuPercent)
    {
        const int size = 16;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.Clear(BgColor);

        int barHeight = (int)(cpuPercent / 100f * (size - 2));
        barHeight = Math.Max(2, Math.Min(size - 2, barHeight));

        using var brush = new SolidBrush(GetColor(cpuPercent));
        g.FillRectangle(brush, 1, size - 1 - barHeight, size - 2, barHeight);

        using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawRectangle(borderPen, 0, 0, size - 1, size - 1);

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static Icon CreatePercentageIcon(float cpuPercent)
    {
        const int size = 16;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        string text = $"{cpuPercent:F0}";
        var color = GetColor(cpuPercent);

        using var font = new Font("Segoe UI", text.Length > 2 ? 7f : 8.5f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);

        var textSize = g.MeasureString(text, font);
        float x = (size - textSize.Width) / 2f;
        float y = (size - textSize.Height) / 2f;

        g.DrawString(text, font, brush, x, y);

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static Icon CreateMiniChartIcon(float cpuPercent)
    {
        const int size = 16;

        _history[_historyIndex] = cpuPercent;
        _historyIndex = (_historyIndex + 1) % _history.Length;

        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BgColor);

        using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawRectangle(borderPen, 0, 0, size - 1, size - 1);

        int barWidth = 1;
        int maxBars = (size - 2) / barWidth;

        for (int i = 0; i < maxBars && i < _history.Length; i++)
        {
            int dataIdx = (_historyIndex - maxBars + i + _history.Length) % _history.Length;
            float val = _history[dataIdx];
            int barH = Math.Max(1, (int)(val / 100f * (size - 2)));
            int x = 1 + i * barWidth;

            var col = GetColor(val);
            using var brush = new SolidBrush(Color.FromArgb(200, col.R, col.G, col.B));
            g.FillRectangle(brush, x, size - 1 - barH, barWidth, barH);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    public static BitmapSource IconToBitmapSource(Icon icon)
    {
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }
}
