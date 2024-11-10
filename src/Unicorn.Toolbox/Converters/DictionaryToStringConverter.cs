using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Unicorn.Toolbox.Converters;

public class DictionaryToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Dictionary<string, string> dictionary ?
            string.Join("\n", dictionary.Select(m => m.Key + ": " + m.Value)) :
            string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
