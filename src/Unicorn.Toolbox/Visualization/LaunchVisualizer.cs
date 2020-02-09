using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Unicorn.Toolbox.LaunchAnalysis;

namespace Unicorn.Toolbox.Visualization
{
    public class LaunchVisualizer
    {
        private const int Margin = 20;
        private const double MaxBarHeight = 100;
        private const double MinBarHeight = 30;

        private readonly Random _random;
        private readonly Brush _fontColor;
        private readonly Brush _backColor;
        
        private readonly Canvas _canvas;
        private readonly List<Execution> _resultsList;

        private readonly int _threadsCount;

        private readonly double _earliestTime = double.MaxValue;
        private readonly double _latestTime = double.MinValue;

        private readonly double _workHeight;
        private readonly double _workWidth;
        private readonly double _ratio;

        private readonly DateTime _utcStart;

        private SolidColorBrush _currentBrush;

        private readonly Rectangle _currentStampBar;
        private readonly TextBlock _currentStamp;

        private Rectangle _currentSuite;
        private bool _newSuite = false;

        public LaunchVisualizer(Canvas canvas, List<Execution> resultsList)
        {
            _canvas = canvas;
            _canvas.Height = Math.Max(canvas.RenderSize.Height, resultsList.Count * (MinBarHeight + Margin) + Margin);
            _resultsList = resultsList;

#pragma warning disable S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive
            _random = new Random();
#pragma warning restore S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive
            _backColor = Brushes.White;
            _fontColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111"));

            _workHeight = canvas.RenderSize.Height - (2 * Margin);
            _workWidth = canvas.RenderSize.Width - (2 * Margin);

            _threadsCount = resultsList.Count;

            _utcStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            foreach (var execution in resultsList)
            {
                var min = execution.TestResults.Min(r => r.StartTime).ToUniversalTime().Subtract(_utcStart).TotalMilliseconds;
                _earliestTime = Math.Min(_earliestTime, min);

                var max = execution.TestResults.Max(r => r.EndTime).ToUniversalTime().Subtract(_utcStart).TotalMilliseconds;
                _latestTime = Math.Max(_latestTime, max);
            }

            var fullDuration = _latestTime - _earliestTime;
            _ratio = _workWidth / fullDuration;

            _currentStampBar = new Rectangle
            {
                Width = 2,
                Height = _canvas.Height,
                Fill = _fontColor,
                StrokeThickness = 1
            };

            _currentStamp = new TextBlock();
            _currentStamp.TextAlignment = TextAlignment.Center;
            _currentStamp.FontFamily = new FontFamily("Calibri");
            _currentStamp.FontSize = 15;
            _currentStamp.Foreground = _fontColor;
        }

        public void Visualize()
        {
            _canvas.Background = _backColor;
            _canvas.Children.Clear();

            DrawText(_utcStart.AddMilliseconds(_earliestTime).ToLocalTime().ToString(), Margin, 0, false);
            DrawText(_utcStart.AddMilliseconds(_latestTime).ToLocalTime().ToString(), _canvas.RenderSize.Width - Margin, 0, true);

            SetRandomColor();
            int currentIndex = 0;

            var listId = _resultsList[0].TestResults[0].TestListId;
            _newSuite = true;

            foreach (var execution in _resultsList)
            {
                foreach (var result in execution.TestResults)
                {
                    if (!result.TestListId.Equals(listId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        SetRandomColor();
                        listId = result.TestListId;
                        _newSuite = true;
                    }

                    var start = result.StartTime.ToUniversalTime().Subtract(_utcStart).TotalMilliseconds;
                    DrawResult(result, currentIndex, start, _canvas);
                }

                currentIndex++;
            }

            _canvas.Children.Add(_currentStampBar);
            Canvas.SetLeft(_currentStampBar, 1);
            Canvas.SetTop(_currentStampBar, 0);

            _canvas.Children.Add(_currentStamp);
            Canvas.SetLeft(_currentStamp, 1);
            Canvas.SetTop(_currentStamp, _canvas.RenderSize.Height);

            _canvas.MouseMove += MoveLine;
        }

        private void DrawResult(TestResult result, int index, double start, Canvas canvas)
        {
            double height = (_workHeight / _threadsCount) - Margin;
            height = Math.Min(MaxBarHeight, height);
            height = Math.Max(MinBarHeight, height);

            double width = result.Duration.TotalMilliseconds * _ratio;

            double x = Margin + (start - _earliestTime) * _ratio;
            double y = Margin + (index * (height + Margin));

            if (_newSuite)
            {
                _newSuite = false;

                var brush = new SolidColorBrush();
                brush.Opacity = 0.2;
                brush.Color = _currentBrush.Color;

                _currentSuite = new Rectangle
                {
                    Fill = brush,
                    Width = width,
                    Height = height,
                    StrokeThickness = 1,
                    Stroke = Brushes.Gray,
                    StrokeDashArray = new DoubleCollection() { 5, 1 },
                    ToolTip = result.TestListName,
                };

                Canvas.SetLeft(_currentSuite, x);
                Canvas.SetTop(_currentSuite, y);
                canvas.Children.Add(_currentSuite);
            }
            else
            {
                _currentSuite.Width += width;
            }

            var tooltipText = result.Name + Environment.NewLine + _utcStart.AddMilliseconds(start).ToLocalTime();

            var bar = new Rectangle
            {
                Fill = _currentBrush,
                Width = width,
                Height = height * 0.75,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
                ToolTip = tooltipText,
            };

            bar.MouseEnter += (s, e) => { bar.Stroke = Brushes.LightBlue; bar.StrokeThickness = 2; };
            bar.MouseLeave += (s, e) => { bar.Stroke = Brushes.Black; bar.StrokeThickness = 1; };

            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            canvas.Children.Add(bar);
        }

        private void DrawText(string text, double x, double y, bool rightAligned)
        {
            var label = new TextBlock();
            label.Text = text;
            label.TextAlignment = TextAlignment.Center;
            label.FontFamily = new FontFamily("Calibri");
            label.FontSize = 15;

            var formattedText = new FormattedText(
                label.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                label.FontSize,
                label.Foreground,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            label.Foreground = _fontColor;

            double offset = rightAligned ? formattedText.Width : 0;

            _canvas.Children.Add(label);
            Canvas.SetLeft(label, x + 2 - offset);
            Canvas.SetTop(label, y + 2);
        }

        private void SetRandomColor() =>
            _currentBrush = new SolidColorBrush(
                    Color.FromRgb(
                    (byte)_random.Next(255),
                    (byte)_random.Next(255),
                    (byte)_random.Next(255)
                    ));

        private void MoveLine(object sender, MouseEventArgs e)
        {
            var pos = Mouse.GetPosition(_canvas);
            Canvas.SetLeft(_currentStampBar, pos.X + 2);

            _currentStamp.Text = _utcStart.AddMilliseconds(_earliestTime).AddMilliseconds((pos.X + Margin) / _ratio).ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff");

            var formattedText = new FormattedText(
                _currentStamp.Text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(_currentStamp.FontFamily, _currentStamp.FontStyle, _currentStamp.FontWeight, _currentStamp.FontStretch),
                _currentStamp.FontSize,
                _currentStamp.Foreground,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            var xPosition = pos.X;

            if (xPosition + formattedText.Width > this._canvas.RenderSize.Width - 20)
            {
                xPosition -= formattedText.Width + 10;
            }
            else
            {
                xPosition += 5;
            }

            Canvas.SetLeft(_currentStamp, xPosition);
            Canvas.SetTop(_currentStamp, _canvas.RenderSize.Height - formattedText.Height);
        }
    }
}
