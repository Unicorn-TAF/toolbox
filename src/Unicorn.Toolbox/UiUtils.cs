using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Unicorn.Toolbox
{
    internal class UiUtils
    {
        internal static void FillGrid(Grid grid, HashSet<string> items)
        {
            var sortedItems = items.OrderBy(s => s).ToList();

            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            grid.Height = (sortedItems.Count + 2) * 20;

            for (int i = 0; i < sortedItems.Count + 2; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            int index = 2;

            foreach (var item in sortedItems)
            {
                var itemCheckbox = new CheckBox
                {
                    Content = item,
                    IsChecked = true
                };

                grid.Children.Add(itemCheckbox);
                Grid.SetRow(itemCheckbox, index++);
            }
        }
    }
}
