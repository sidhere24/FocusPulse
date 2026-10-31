using System;
using System.Threading;
using System.Windows;

namespace FocusPulse
{
    public class App : Application
    {
        private static Mutex _mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            bool createdNew = true;
            try
            {
                _mutex = new Mutex(true, "Local\\FocusPulse_SingleInstance_Mutex_" + Environment.UserName, out createdNew);
            }
            catch
            {
                createdNew = true;
            }

            if (!createdNew)
            {
                MessageBox.Show("FocusPulse is already active on your desktop or system tray.",
                                "FocusPulse",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                    string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusPulse");
                    if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "crash.log"), e.ExceptionObject.ToString());
                }
                catch { }
            };

            try
            {
                App app = new App();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
                {
                    try
                    {
                        string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusPulse");
                        if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                        System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "dispatcher_crash.log"), e.Exception.ToString());
                    }
                    catch { }
                    e.Handled = true;
                };

                MainWindow window = new MainWindow();
                window.Show();
                app.Run(window);
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusPulse");
                    if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "startup.log"), ex.ToString());
                }
                catch { }

                MessageBox.Show("FocusPulse encountered an error during startup:\n" + ex.Message,
                                "FocusPulse Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Close();
                }
            }
        }
    }
}
