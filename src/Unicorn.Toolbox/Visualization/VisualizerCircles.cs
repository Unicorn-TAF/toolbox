﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.Visualization
{
    public class VisualizerCircles : AbstractVisualizer
    {
        private const int Margin = 30;

        private readonly Random _random;
        private readonly List<Rect> _rects;

        public VisualizerCircles(Canvas canvas, IPalette palette) : base(canvas, palette)
        {
            _rects = new List<Rect>();

#pragma warning disable S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive
            _random = new Random();
#pragma warning restore S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive
        }

        public override void VisualizeData(IOrderedEnumerable<KeyValuePair<string, int>> data)
        {
            PrepareCanvas();

            _rects.Clear();

            int currentIndex = 0;
            int maxValue = data.Max(p => p.Value);
            int itemsCount = data.Count();

            foreach (KeyValuePair<string, int> pair in data)
            {
                int radius = CalculateRadius(pair.Value, maxValue, itemsCount, (int)Canvas.RenderSize.Width);
                DrawFeature(pair.Key, pair.Value, radius, currentIndex++, itemsCount, Canvas);
            }
        }

        private void DrawFeature(string name, int tests, int radius, int index, int featuresCount, Canvas canvas)
        {
            int x = 0;
            int y = 0;
            Rect rect;

            do
            {
                x = _random.Next(Margin + radius, (int)canvas.RenderSize.Width - radius - Margin);
                y = _random.Next(Margin + radius, (int)canvas.RenderSize.Height - radius - Margin);

                rect = new Rect(x - radius - Margin, y - radius - Margin, (radius + Margin) * 2, (radius + Margin) * 2);
            }
            while (_rects.Any(r => r.IntersectsWith(rect)));

            _rects.Add(rect);

            double colorIndexStep = (double)Palette.DataColors.Count / featuresCount;
            int currentColorIndex = (int)(((index + 1) * colorIndexStep) - 1);

            double diameter = radius * 2;

            Ellipse ellipse = new Ellipse
            {
                Fill = Palette.DataColors[currentColorIndex],
                Width = diameter,
                Height = diameter,
                StrokeThickness = 0.5,
                Stroke = Brushes.Black,
                Effect = Shadow,
                ToolTip = tests
            };

            double shift = Shadow.BlurRadius / 2; // default shadow blur radius.

            ellipse.MouseEnter += (s, e) =>
            {
                Canvas.SetLeft(ellipse, x - radius + shift);
                Canvas.SetTop(ellipse, y - radius + shift);
                ellipse.Effect = null;
            };

            ellipse.MouseLeave += (s, e) =>
            {
                Canvas.SetLeft(ellipse, x - radius);
                Canvas.SetTop(ellipse, y - radius);
                ellipse.Effect = Shadow;
            };

            canvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, x - radius);
            Canvas.SetTop(ellipse, y - radius);

            double fontSize = featuresCount < 20 ? 15 : 13;
            AddLabel(x, y, radius, name, fontSize);
        }

        private void AddLabel(double x, double y, double yOffset, string labelText, double fontSize)
        {
            var label = new TextBlock
            {
                Text = labelText
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Calibri"),
                FontSize = fontSize,
                Foreground = Palette.DataFontColor
            };

            var formattedText = new FormattedText(
                label.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                label.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Display);

            Canvas.Children.Add(label);
            Canvas.SetLeft(label, x - (formattedText.Width / 2));
            Canvas.SetTop(label, y - yOffset - formattedText.Height);
        }

        private int CalculateRadius(int capacity, int max, int count, int canvasSize)
        {
            if (capacity == 0)
            {
                return 1;
            }

            double radius = (double)canvasSize / Math.Sqrt(count + Margin);
            double ratio = (double)capacity / (double)max;
            return (int)(radius * ratio / 2);
        }

        private string CamelCase(string s)
        {
            var x = s.Replace("_", " ");

            if (x.Length == 0)
            {
                return "Null";
            }

            x = Regex.Replace(x, "([A-Z])([A-Z]+)", m => m.Groups[1].Value + m.Groups[2].Value.ToLower());

            return char.ToUpper(x[0]) + x.Substring(1);
        }
    }
}
