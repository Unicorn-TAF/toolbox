using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Unicorn.Toolbox.Converters
{
    public class ArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string delimiter = (string)parameter;

            if (value is IEnumerable<string> ienumerable)
            {
                string result = string.Join(delimiter, ienumerable);

                if (delimiter.Trim() == "#")
                {
                    result = "#" + result;
                }

                return result.ToLowerInvariant();
            }
            else
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
