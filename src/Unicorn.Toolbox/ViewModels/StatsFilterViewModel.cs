using System.Collections.Generic;
using System.Linq;
using Unicorn.Toolbox.Models.Stats.Filtering;

namespace Unicorn.Toolbox.ViewModels
{
    public class StatsFilterViewModel : ViewModelBase
    {
        public StatsFilterViewModel(FilterType filter)
        {
            Filter = filter;
        }

        public FilterType Filter { get; }

        public string FilterName => Filter.ToString();

        public IEnumerable<FilterItemViewModel> Values { get; set; }

        public IEnumerable<string> SelectedValues => Values.Where(t => t.Selected).Select(t => t.Name);

        public void Populate(IEnumerable<string> data)
        {
            Values = new List<FilterItemViewModel>(data.Select(i => new FilterItemViewModel(i)))
                .OrderBy(i => i.Name);

            OnPropertyChanged(nameof(Values));
        }
    }
}
