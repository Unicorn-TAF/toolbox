using OxyPlot;
using System.Collections.Generic;

namespace Unicorn.Toolbox.Visualization;

public class Palettes
{
    public static IList<IPalette> AvailablePalettes { get; } = new List<IPalette>()
    { 
        new ForestGreen(), 
        new Purple(), 
        new IndianRed()
    };

    public class Purple : IPalette
    {
        public OxyColor MainColor { get; } = OxyColors.Purple;

        public string Name { get; } = "Purple";
    }

    public class ForestGreen : IPalette
    {
        public OxyColor MainColor { get; } = OxyColors.ForestGreen;

        public string Name { get; } = "ForestGreen";
    }

    public class IndianRed : IPalette
    {
        public OxyColor MainColor { get; } = OxyColors.IndianRed;

        public string Name { get; } = "IndianRed";
    }
}
