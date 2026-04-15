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
    }
}
