using System.Windows.Controls;
using PayrollSystem.ViewModels;

namespace PayrollSystem.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void DismissNotification_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
                vm.DismissNotification();
        }
    }
}
