using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Roi;

namespace Unicorn.Toolbox.ViewModels
{
    public class RoiConfigurationViewModel : FunctionalityViewModelBase
    {
        private readonly RoiInputs _config;

        public RoiConfigurationViewModel(RoiInputs config)
        {
            _config = config;
            ManualTestExecutionMins = _config.ManualTestExecutionHrs * 60;
            CalculateRoiCommand = new CalculateRoiCommand(_config);
        }

        public ICommand CalculateRoiCommand { get; }

        [Notify]
        public double ManualTestExecutionMins
        {
            get => _config.ManualTestExecutionHrs * 60;
            set => _config.ManualTestExecutionHrs = value / 60;
        }

        [Notify]
        public double FailAnalyzeMins
        {
            get => _config.FailAnalyzeHrs * 60;
            set => _config.FailAnalyzeHrs = value / 60;
        }

        [Notify]
        public double TafCreationHrs
        {
            get => _config.TafCreationHrs;
            set => _config.TafCreationHrs = value;
        }

        [Notify]
        public double AutoTestCreationHrs
        {
            get => _config.AutoTestCreationHrs;
            set => _config.AutoTestCreationHrs = value;
        }

        [Notify]
        public double AutoTestFixHrs
        {
            get => _config.AutoTestFixHrs;
            set => _config.AutoTestFixHrs = value;
        }

        [Notify]
        public double AtFailsPercent
        {
            get => _config.AtFailsRatio * 100;
            set => _config.AtFailsRatio = value / 100;
        }

        [Notify]
        public double DevFailsPercent
        {
            get => _config.DevFailsRatio * 100;
            set => _config.DevFailsRatio = value / 100;
        }

        [Notify]
        public double NonDevHrs
        {
            get => _config.NonDevHrs;
            set => _config.NonDevHrs = value;
        }

        [Notify]
        public int BuildsPerPeriod
        {
            get => _config.BuildsPerPeriod;
            set => _config.BuildsPerPeriod = value;
        }

        [Notify]
        public int TeamCount
        {
            get => _config.TeamCount;
            set => _config.TeamCount = value;
        }


        [Notify]
        public int PeriodsCount
        {
            get => _config.PeriodsCount;
            set => _config.PeriodsCount = value;
        }

        public double AtVsMtSalaryRatio { get; set; } = 1.5;
    }
}
