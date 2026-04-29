using System.Windows;
using System.Windows.Controls;

namespace PayrollSystem.Views
{
    public partial class EmployeeManagementView : UserControl
    {
        public EmployeeManagementView()
        {
            InitializeComponent();
            SearchBox.GotFocus += (s, e) => SearchPlaceholder.Visibility = Visibility.Collapsed;
            SearchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(SearchBox.Text))
                    SearchPlaceholder.Visibility = Visibility.Visible;
            };
            SearchBox.TextChanged += (s, e) =>
            {
                SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        private void DataGridColumnHeader_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var header = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;
            if (header != null && header.Content != null)
            {
                var columnStr = header.Content.ToString();
                if (columnStr != null && DataContext is ViewModels.EmployeeViewModel vm)
                {
                    vm.SortData(columnStr);
                }
            }
        }

        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Temporarily detach to prevent infinite loop
                textBox.TextChanged -= NumberTextBox_TextChanged;

                string raw = textBox.Text.Replace(",", "");
                if (decimal.TryParse(raw, out decimal result))
                {
                    int caretIndex = textBox.CaretIndex;
                    int lengthBefore = textBox.Text.Length;

                    // Format with commas, keep decimals if the user is typing them
                    if (raw.Contains("."))
                    {
                        // Don't format while they are typing the decimal part to avoid disrupting input
                    }
                    else
                    {
                        textBox.Text = string.Format("{0:N0}", result);
                        int diff = textBox.Text.Length - lengthBefore;
                        textBox.CaretIndex = System.Math.Max(0, caretIndex + diff);
                    }
                }

                textBox.TextChanged += NumberTextBox_TextChanged;
            }
        }
    }
}
