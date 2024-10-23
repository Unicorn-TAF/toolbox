using Unicorn.Toolbox.Roi;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands;

public class CalculateRoiCommand : CommandBase
{
    private readonly RoiInputs _inputs;

    public CalculateRoiCommand(RoiInputs inputs)
    {
        _inputs = inputs;
    }

    public override void Execute(object parameter)
    {
        Calculator calc = new(_inputs);
        RoiForecast forecast = calc.CalculateForecast();

        RoiForecastViewModel roiForecastVm = new(forecast, _inputs);

        DialogHost window = new DialogHost("ROI forecast: " + _inputs.ToString())
        {
            DataContext = new DialogHostViewModel(roiForecastVm),
            ShowActivated = true
        };

        window.Show();
    }
}
