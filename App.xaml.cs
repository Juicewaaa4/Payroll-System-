using System.Threading.Tasks;
using System.Windows;
using PayrollSystem.DataAccess;

namespace PayrollSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Pre-test and cache DB connection on background thread
            // so login and dashboard are instant
            Task.Run(() =>
            {
                DatabaseHelper.TestConnection();
                try { DatabaseHelper.InitializeDatabase(); } catch { }
            });
        }
    }
}
