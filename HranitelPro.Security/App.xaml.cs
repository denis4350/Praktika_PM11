using System.Windows;

namespace HranitelPro.Security
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new SecurityLoginWindow();
            loginWindow.ShowDialog();
        }
    }
}