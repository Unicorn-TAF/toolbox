using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.LaunchAnalasys;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.Visualization
{
    public static class LaunchVisualizer
    {
        private static int margin = 15;
        private static Brush fontColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111"));
        private static Brush backColor = Brushes.White;
        private static Random random = new Random();

        public static void VisualizeAllData(List<List<TestResult>> resultsList, Canvas canvas)
        {
            canvas.Background = backColor;
            canvas.Children.Clear();

            int barsCount = resultsList.Count;

            var minStart = double.MaxValue;
            var maxEnd = double.MinValue;

            foreach (var list in resultsList)
            {
                var min = list.Min(r => r.StartTime).ToUniversalTime().Subtract(
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                minStart = Math.Min(minStart, min);

                var max = list.Max(r => r.EndTime).ToUniversalTime().Subtract(
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                maxEnd = Math.Max(maxEnd, max);
            }

            var maxDuration = maxEnd - minStart;

            var ratio = (canvas.RenderSize.Width - 2 * margin) / maxDuration;

            int currentIndex = 0;

            foreach(var results in resultsList)
            {
                foreach (var result in results)
                {
                    var start = result.StartTime.ToUniversalTime().Subtract(
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                    DrawResult(result.Name, currentIndex, ratio, minStart, result.Duration.TotalMilliseconds, start, barsCount, canvas);
                }

                currentIndex++;
            }
            
        }

        private static void DrawResult(string name, int index, double ratio, double minStart, double duration, double start, int barsCount, Canvas canvas)
        {
            var workHeight = canvas.RenderSize.Height - (2 * margin);
            var workWidth = canvas.RenderSize.Width - (2 * margin);

            double height = (workHeight / barsCount) - margin;
            double width = duration * ratio; 

            double x = margin + (start - minStart) * ratio;
            double y = margin + (index * (height + margin));

            var bar = new Rectangle()
            {
                Fill = GetRandomColor(),
                Width = width,
                Height = height,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
                Effect = new DropShadowEffect(),
                ToolTip = name
            };

            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            canvas.Children.Add(bar);
        }

        private static SolidColorBrush GetRandomColor()
        {

            return new SolidColorBrush(
                    Color.FromRgb(
                    (byte)random.Next(255),
                    (byte)random.Next(255),
                    (byte)random.Next(255)
                    ));
        }
    }
}
