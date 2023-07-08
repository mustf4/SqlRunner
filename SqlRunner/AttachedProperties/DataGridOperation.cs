using System;
using System.Windows;
using System.Windows.Controls;

namespace SqlRunner.AttachedProperties
{
    public class DataGridOperation
    {
        public static string GetDateTimeFormatAutoGenerate(DependencyObject obj) => (string)obj.GetValue(DateTimeFormatAutoGenerateProperty);
        public static void SetDateTimeFormatAutoGenerate(DependencyObject obj, string value) => obj.SetValue(DateTimeFormatAutoGenerateProperty, value);
        public static readonly DependencyProperty DateTimeFormatAutoGenerateProperty =
            DependencyProperty.RegisterAttached("DateTimeFormatAutoGenerate", typeof(string), typeof(DataGridOperation),
                new PropertyMetadata(null, (d, e) => AddEventHandlerOnGenerating<DateTime>(d, e)));

        public static string GetTimeSpanFormatAutoGenerate(DependencyObject obj) => (string)obj.GetValue(TimeSpanFormatAutoGenerateProperty);
        public static void SetTimeSpanFormatAutoGenerate(DependencyObject obj, string value) => obj.SetValue(TimeSpanFormatAutoGenerateProperty, value);
        public static readonly DependencyProperty TimeSpanFormatAutoGenerateProperty =
            DependencyProperty.RegisterAttached("TimeSpanFormatAutoGenerate", typeof(string), typeof(DataGridOperation),
                new PropertyMetadata(null, (d, e) => AddEventHandlerOnGenerating<TimeSpan>(d, e)));

        private static void AddEventHandlerOnGenerating<T>(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dGrid)
                return;

            if ((e.NewValue is string format))
                dGrid.AutoGeneratingColumn += (o, e) => AddFormatOnGenerating<T>(e, format);
        }

        private static void AddFormatOnGenerating<T>(DataGridAutoGeneratingColumnEventArgs e, string format)
        {
            if (e.PropertyType == typeof(T))
                (e.Column as DataGridTextColumn).Binding.StringFormat = format;
        }
    }
}
