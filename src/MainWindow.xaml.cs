using System.Collections.Specialized;
using System.Windows;

namespace BTSensitivitySearchV1._0;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (LogListBox.ItemsSource is INotifyCollectionChanged ncc)
        {
            ncc.CollectionChanged += (_, _) =>
            {
                if (LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[^1]);
            };
        }
    }
}
