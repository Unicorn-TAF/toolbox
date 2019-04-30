using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Unicorn.Toolbox.LaunchAnalasys;

namespace Unicorn.Toolbox.Visualization
{
    public class LaunchVisualizer
    {
        private const int Margin = 20;

        private readonly Random random;
        private readonly Brush fontColor;
        private readonly Brush backColor;
        
        private readonly Canvas canvas;
        private readonly List<List<TestResult>> resultsList;

        private readonly int threadsCount;

        private readonly double earliestTime = double.MaxValue;
        private readonly double latestTime = double.MinValue;

        private readonly double workHeight;
        private readonly double workWidth;
        private readonly double ratio;

        private readonly DateTime utcStart;

        private SolidColorBrush currentBrush;

        private Rectangle currentStampBar;
        private TextBlock currentStamp;

        public LaunchVisualizer(Canvas canvas, List<List<TestResult>> resultsList)
        {
            this.canvas = canvas;
            this.resultsList = resultsList;

            this.random = new Random();
            this.backColor = Brushes.White;
            this.fontColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111"));

            workHeight = canvas.RenderSize.Height - (2 * Margin);
            workWidth = canvas.RenderSize.Width - (2 * Margin);

            this.threadsCount = resultsList.Count;

            utcStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            foreach (var list in resultsList)
            {
                var min = list.Min(r => r.StartTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                earliestTime = Math.Min(earliestTime, min);

                var max = list.Max(r => r.EndTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                latestTime = Math.Max(latestTime, max);
            }

            var fullDuration = latestTime - earliestTime;
            ratio = workWidth / fullDuration;

            currentStampBar = new Rectangle
            {
                Width = 2,
                Height = canvas.RenderSize.Height,
                Fill = fontColor,
                StrokeThickness = 1
            };

            currentStamp = new TextBlock();
            currentStamp.TextAlignment = TextAlignment.Center;
            currentStamp.FontFamily = new FontFamily("Calibri");
            currentStamp.FontSize = 15;
            currentStamp.Foreground = fontColor;
        }

        public void Visualize()
        {
            canvas.Background = backColor;
            canvas.Children.Clear();

            DrawText(utcStart.AddMilliseconds(earliestTime).ToLocalTime().ToString(), Margin, 0, false);
            DrawText(utcStart.AddMilliseconds(latestTime).ToLocalTime().ToString(), canvas.RenderSize.Width - Margin, 0, true);

            SetRandomColor();
            int currentIndex = 0;

            var listId = resultsList[0][0].TestListId;

            foreach (var results in resultsList)
            {
                foreach (var result in results)
                {
                    if (!result.TestListId.Equals(listId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        SetRandomColor();
                        listId = result.TestListId;
                    }

                    var start = result.StartTime.ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                    DrawResult(result.Name, currentIndex, result.Duration.TotalMilliseconds, start, canvas);
                }

                currentIndex++;
            }

            canvas.Children.Add(currentStampBar);
            Canvas.SetLeft(currentStampBar, 1);
            Canvas.SetTop(currentStampBar, 0);

            canvas.Children.Add(currentStamp);
            Canvas.SetLeft(currentStamp, 1);
            Canvas.SetTop(currentStamp, canvas.RenderSize.Height);

            canvas.MouseMove += MoveLine;
        }

        private void DrawResult(string name, int index, double duration, double start, Canvas canvas)
        {
            double height = (workHeight / threadsCount) - Margin;
            double width = duration * ratio;

            double x = Margin + (start - earliestTime) * ratio;
            double y = Margin + (index * (height + Margin));

            var tooltipText = name + Environment.NewLine + utcStart.AddMilliseconds(start).ToLocalTime();

            var bar = new Rectangle()
            {
                Fill = currentBrush,
                Width = width,
                Height = height,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
                Effect = new DropShadowEffect(),
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

            label.Foreground = fontColor;

            double offset = rightAligned ? formattedText.Width : 0;

            canvas.Children.Add(label);
            Canvas.SetLeft(label, x + 2 - offset);
            Canvas.SetTop(label, y + 2);
        }

        private void SetRandomColor() =>
            currentBrush = new SolidColorBrush(
                    Color.FromRgb(
                    (byte)random.Next(255),
                    (byte)random.Next(255),
                    (byte)random.Next(255)
                    ));

        private void MoveLine(object sender, MouseEventArgs e)
        {
            var pos = Mouse.GetPosition(canvas);
            Canvas.SetLeft(currentStampBar, pos.X + 2);

            var formattedText = new FormattedText(
                currentStamp.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(currentStamp.FontFamily, currentStamp.FontStyle, currentStamp.FontWeight, currentStamp.FontStretch),
                currentStamp.FontSize,
                currentStamp.Foreground,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            Canvas.SetLeft(currentStamp, pos.X);
            Canvas.SetTop(currentStamp, canvas.RenderSize.Height - formattedText.Height);
            currentStamp.Text = utcStart.AddMilliseconds(earliestTime).AddMilliseconds((pos.X + Margin) / ratio).ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }
    }
}
