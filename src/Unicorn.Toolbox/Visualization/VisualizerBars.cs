using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.Visualization
{
    public class VisualizerBars : AbstractVisualizer
    {
        private const int Margin = 15;
        private const int MinBarHeight = 20;

        public VisualizerBars(Canvas canvas, IPalette palette) : base(canvas, palette)
        {
        }

        public override void VisualizeData(IOrderedEnumerable<KeyValuePair<string, int>> data)
        {
            PrepareCanvas();

            int currentIndex = 0;
            int maxValue = data.Max(p => p.Value);
            int itemsCount = data.Count();
            int expectedHeight = (MinBarHeight + Margin) * itemsCount + Margin;

            if (Canvas.RenderSize.Height < expectedHeight)
            {
                Canvas.Height = expectedHeight;
            }

            foreach (KeyValuePair<string, int> pair in data)
            {
                DrawFeature(pair.Key, pair.Value, currentIndex++, maxValue, itemsCount, Canvas);
            }
        }

        private void DrawFeature(string name, int tests, int index, int max, int featuresCount, Canvas canvas)
        {
            double workHeight = canvas.RenderSize.Height - (2 * Margin);
            double workWidth = canvas.RenderSize.Width - (2 * Margin);

            double height = Math.Max(MinBarHeight, (workHeight / featuresCount) - Margin);
            double width = tests == 0 ? 1 : workWidth * ((double)tests / max); 

            double x = Margin;
            double y = Margin + (index * (height + Margin));

            double colorIndexStep = (double)Palette.DataColors.Count / featuresCount;
            int currentColorIndex = (int)(((index + 1) * colorIndexStep) - 1);

            Rectangle bar = new Rectangle()
            {
                Fill = Palette.DataColors[currentColorIndex],
                Width = width,
                Height = height,
                StrokeThickness = 1,
                Stroke = Brushes.Black,
                Effect = Shadow
            };

            double shift = Shadow.BlurRadius;

            bar.MouseEnter += (s, e) => 
            {
                Canvas.SetLeft(bar, x + shift);
                Canvas.SetTop(bar, y + shift);
                bar.Effect = null;
            };

            bar.MouseLeave += (s, e) => 
            {
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                bar.Effect = Shadow;
            };

            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            canvas.Children.Add(bar);

            var label = new TextBlock();
            label.Text = $"{name}: {tests}";
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

            label.Foreground = formattedText.Width > width ? Palette.FontColor : Palette.DataFontColor;

            canvas.Children.Add(label);
            Canvas.SetLeft(label, x + 2);
            Canvas.SetTop(label, y + 2);
        }
    }
}
