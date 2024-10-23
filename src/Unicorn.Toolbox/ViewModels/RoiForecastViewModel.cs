using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Legends;
using OxyPlot.Series;
using System.Collections.Generic;
using Unicorn.Toolbox.Roi;

namespace Unicorn.Toolbox.ViewModels
{
    public class RoiForecastViewModel : ViewModelBase
    {
        public RoiForecastViewModel(RoiForecast forecast, RoiInputs inputs)
        {
            RoiEntries = forecast.Series;
            RoiZeroIndex = forecast.ZeroReachIndex;

            BuildRoiPlotModel();
            BuildEfficiencyPlotModel();

            Configuration = inputs.ToString();
        }

        public IList<RoiEntry> RoiEntries { get; }

        public int RoiZeroIndex { get; }

        public PlotModel RoiPlotModel { get; set; }

        public PlotModel EfficiencyPlotModel { get; set; }

        public string Configuration { get; set; }

        private void BuildRoiPlotModel()
        {
            RoiPlotModel = new PlotModel
            {
                Title = "ROI"
            };

            LineSeries roiSeries = new LineSeries()
            {
                Color = OxyColors.ForestGreen,
            };

            for (int i = 0; i < RoiEntries.Count; i++)
            {
                roiSeries.Points.Add(new DataPoint(i + 1, RoiEntries[i].ROI));
            }

            RoiPlotModel.Series.Add(roiSeries);
            RoiPlotModel.Annotations.Add(GetAnnotation());
        }

        private void BuildEfficiencyPlotModel()
        {
            EfficiencyPlotModel = new PlotModel
            {
                Title = "Efficiency"
            };

            LineSeries atSeries = new LineSeries()
            {
                Title = "Time spent on automation (man-days)",
                Color = OxyColors.IndianRed
            };

            LineSeries mtSavedSeries = new LineSeries()
            {
                Title = "Time saved on manual testing (man-days)",
                Color = OxyColors.ForestGreen,
            };

            for (int i = 0; i < RoiEntries.Count; i++)
            {
                atSeries.Points.Add(new DataPoint(i + 1, RoiEntries[i].AtManDaysTotal));
                mtSavedSeries.Points.Add(new DataPoint(i + 1, RoiEntries[i].MtManDaysSavedTotal));
            }

            EfficiencyPlotModel.Series.Add(atSeries);
            EfficiencyPlotModel.Series.Add(mtSavedSeries);
            EfficiencyPlotModel.Annotations.Add(GetAnnotation());

            EfficiencyPlotModel.Legends.Add(new Legend()
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter
            });
        }

        private LineAnnotation GetAnnotation() =>
            new LineAnnotation()
            {
                StrokeThickness = 1,
                LineStyle = LineStyle.LongDashDot,
                Color = OxyColors.Coral,
                Type = LineAnnotationType.Vertical,
                TextLinePosition = 0.05,
                TextOrientation = AnnotationTextOrientation.Horizontal,
                Text = RoiZeroIndex.ToString() + "  ",
                TextColor = OxyColors.Black,
                X = RoiZeroIndex,
            };
    }
}
