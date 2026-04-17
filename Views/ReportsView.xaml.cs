using System.Windows.Controls;

namespace PayrollSystem.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void DataGridColumnHeader_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var header = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;
            if (header != null && header.Content != null)
            {
                var columnStr = header.Content.ToString();
                if (DataContext is ViewModels.ReportsViewModel vm)
                {
                    vm.SortData(columnStr);
                }
            }
        }
    }
}
