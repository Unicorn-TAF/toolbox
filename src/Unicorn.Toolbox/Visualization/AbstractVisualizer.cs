using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

        public abstract void VisualizeData(IOrderedEnumerable<KeyValuePair<string, int>> data);

        protected void PrepareCanvas()
        {
            Canvas.Background = Palette.BackColor;
            Canvas.Children.Clear();
        }
    }
}
