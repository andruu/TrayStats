using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TrayStats.Views.Components;

/// <summary>
/// Lightweight sparkline chart drawn with WPF Polyline.
/// Bind Values to a List of double and call InvalidateValues() to trigger a redraw,
/// or bind to an ObservableCollection for automatic updates.
/// </summary>
public class SparklineChart : Canvas
{
    private readonly Polyline _line;
    private readonly Polygon _fill;
    private bool _redrawPending;
    private Brush? _cachedFillBrush;
    private Brush? _lastStrokeForFill;
    private Brush? _lastExplicitFill;
    private PointCollection _linePoints = new();
    private PointCollection _fillPoints = new();

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(nameof(Values), typeof(IList<double>), typeof(SparklineChart),
            new PropertyMetadata(null, OnValuesChanged));

    public static readonly DependencyProperty StrokeColorProperty =
        DependencyProperty.Register(nameof(StrokeColor), typeof(Brush), typeof(SparklineChart),
            new PropertyMetadata(Brushes.LimeGreen, OnAppearanceChanged));

    public static readonly DependencyProperty FillColorProperty =
        DependencyProperty.Register(nameof(FillColor), typeof(Brush), typeof(SparklineChart),
            new PropertyMetadata(null, OnAppearanceChanged));

    public static readonly DependencyProperty StrokeThicknessValueProperty =
        DependencyProperty.Register(nameof(StrokeThicknessValue), typeof(double), typeof(SparklineChart),
            new PropertyMetadata(1.5, OnAppearanceChanged));

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(nameof(MinValue), typeof(double), typeof(SparklineChart),
            new PropertyMetadata(0.0, OnDataChanged));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(SparklineChart),
            new PropertyMetadata(100.0, OnDataChanged));

    public static readonly DependencyProperty AutoScaleProperty =
        DependencyProperty.Register(nameof(AutoScale), typeof(bool), typeof(SparklineChart),
            new PropertyMetadata(false, OnDataChanged));

    public IList<double>? Values
    {
        get => (IList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public void InvalidateValues() => ScheduleRedraw();

    public Brush StrokeColor
    {
        get => (Brush)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public Brush? FillColor
    {
        get => (Brush?)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public double StrokeThicknessValue
    {
        get => (double)GetValue(StrokeThicknessValueProperty);
        set => SetValue(StrokeThicknessValueProperty, value);
    }

    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public bool AutoScale
    {
        get => (bool)GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public SparklineChart()
    {
        ClipToBounds = true;

        _fill = new Polygon
        {
            StrokeThickness = 0,
            IsHitTestVisible = false
        };

        _line = new Polyline
        {
            StrokeLineJoin = PenLineJoin.Round,
            IsHitTestVisible = false
        };

        Children.Add(_fill);
        Children.Add(_line);

        SizeChanged += (_, _) => ScheduleRedraw();
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SparklineChart)d;

        if (e.OldValue is INotifyCollectionChanged oldColl)
            oldColl.CollectionChanged -= chart.OnCollectionChanged;

        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += chart.OnCollectionChanged;

        chart.ScheduleRedraw();
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SparklineChart)d;
        chart._cachedFillBrush = null;
        chart.ScheduleRedraw();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SparklineChart)d).ScheduleRedraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleRedraw();
    }

    private void ScheduleRedraw()
    {
        if (_redrawPending) return;
        _redrawPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, Redraw);
    }

    private void Redraw()
    {
        _redrawPending = false;

        _line.Stroke = StrokeColor;
        _line.StrokeThickness = StrokeThicknessValue;

        var explicitFill = FillColor;
        if (explicitFill != null)
        {
            _fill.Fill = explicitFill;
        }
        else
        {
            if (_cachedFillBrush == null || _lastStrokeForFill != StrokeColor || _lastExplicitFill != explicitFill)
            {
                if (StrokeColor is SolidColorBrush scb)
                    _cachedFillBrush = new SolidColorBrush(Color.FromArgb(40, scb.Color.R, scb.Color.G, scb.Color.B));
                else
                    _cachedFillBrush = new SolidColorBrush(Color.FromArgb(40, 128, 255, 128));

                _cachedFillBrush.Freeze();
                _lastStrokeForFill = StrokeColor;
                _lastExplicitFill = explicitFill;
            }
            _fill.Fill = _cachedFillBrush;
        }

        var values = Values;
        double w = ActualWidth;
        double h = ActualHeight;

        if (values == null || values.Count < 2 || w <= 0 || h <= 0)
        {
            _linePoints.Clear();
            _fillPoints.Clear();
            _line.Points = _linePoints;
            _fill.Points = _fillPoints;
            return;
        }

        double minVal = MinValue;
        double maxVal = MaxValue;

        if (AutoScale)
        {
            maxVal = double.MinValue;
            minVal = double.MaxValue;
            foreach (var v in values)
            {
                if (v > maxVal) maxVal = v;
                if (v < minVal) minVal = v;
            }
            if (Math.Abs(maxVal - minVal) < 0.001) maxVal = minVal + 1;
            minVal = Math.Max(0, minVal - (maxVal - minVal) * 0.1);
            maxVal += (maxVal - minVal) * 0.1;
        }

        double range = maxVal - minVal;
        if (range <= 0) range = 1;

        int count = values.Count;
        double step = w / (count - 1);

        _linePoints = new PointCollection(count);
        _fillPoints = new PointCollection(count + 2);

        for (int i = 0; i < count; i++)
        {
            double x = i * step;
            double normalized = (values[i] - minVal) / range;
            double y = h - (normalized * h);
            y = Math.Max(0, Math.Min(h, y));

            var pt = new Point(x, y);
            _linePoints.Add(pt);
            _fillPoints.Add(pt);
        }

        _fillPoints.Add(new Point(w, h));
        _fillPoints.Add(new Point(0, h));

        _line.Points = _linePoints;
        _fill.Points = _fillPoints;
    }
}
