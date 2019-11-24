using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.Visualization
{
    public abstract class AbstractVisualizer
    {
        protected AbstractVisualizer(Canvas canvas, IPalette palette)
        {
            Canvas = canvas;
            Palette = palette;

            Shadow = new DropShadowEffect();

            if (Palette is DeepPurple)
            {
                Shadow.Color = Color.FromRgb(137, 137, 137);
            }
        }

        protected Canvas Canvas { get; set; }

        protected IPalette Palette { get; set; }

        protected DropShadowEffect Shadow { get; set; }

        public abstract void VisualizeAutomationData(AutomationData data, FilterType filterType);

        public abstract void VisualizeCoverage(AppSpecs specs);

        protected void PrepareCanvas()
        {
            this.Canvas.Background = Palette.BackColor;
            this.Canvas.Children.Clear();
        }

        protected Dictionary<string, int> GetAutomationStatistics(AutomationData data, FilterType filterType)
        {
            var stats = new Dictionary<string, int>();

            switch (filterType)
            {
                case FilterType.Feature:
                    {
                        foreach (var feature in data.UniqueFeatures)
                        {
                            var suites = data.SuitesInfos.Where(s => s.Features.Contains(feature));
                            var tests = from SuiteInfo s
                                        in suites
                                        select s.TestsInfos;

                            stats.Add(feature, tests.Sum(t => t.Count));
                        }

                        return stats;
                    }

                case FilterType.Category:
                    {
                        foreach (var category in data.UniqueCategories)
                        {
                            var tests = from SuiteInfo s
                                        in data.SuitesInfos
                                        select s.TestsInfos.Where(ti => ti.Categories.Contains(category));

                            stats.Add(category, tests.Sum(t => t.Count()));
                        }

                        return stats;
                    }

                case FilterType.Author:
                    {
                        foreach (var author in data.UniqueAuthors)
                        {
                            var tests = from SuiteInfo s
                                        in data.SuitesInfos
                                        select s.TestsInfos.Where(ti => ti.Author.Equals(author));

                            stats.Add(author, tests.Sum(t => t.Count()));
                        }

                        return stats;
                    }
            }

            throw new ArgumentException("please check args");
        }
    }
}
