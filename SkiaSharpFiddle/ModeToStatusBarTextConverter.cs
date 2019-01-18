using System;
using System.Globalization;
using System.Windows.Data;

namespace SkiaSharpFiddle
{
    public class ModeToStatusBarTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Mode mode)
            {
                switch (mode)
                {
                    case Mode.Working:
                        return "Compiling drawing code...";
                    case Mode.Error:
                        return "Some errors were found.";
                }
            }

            return "Ready.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
