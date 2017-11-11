using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DokanNet;
using DokanNetMirror;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static new App Current => Application.Current as App;
        private System.IO.FileSystemWatcher fsw;

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            fsw = new System.IO.FileSystemWatcher(@"C:\asd");
            fsw.Created += Fsw_Something;
            fsw.Renamed += Fsw_Something;
            fsw.Deleted += Fsw_Something;
            fsw.Changed += Fsw_Something;
            fsw.EnableRaisingEvents = true;

            var mirror = new Mirror("C:");
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    mirror.Mount("n:\\");
                    Console.WriteLine("Success");

                }
                catch (DokanException ex)
                {

                }
            });
            MainWindow = new FileSystemWatcherWindow(mirror);
            MainWindow.Show();
            var asd = new FileSystemGanttWindow(mirror);
            asd.Show();
        }

        private void Fsw_Something(object sender, System.IO.FileSystemEventArgs e)
        {
            var dokanFullPath = "n" + e.FullPath.Substring(1);
            var isDirectory = System.IO.Directory.Exists(e.FullPath);
            switch (e.ChangeType)
            {
                case System.IO.WatcherChangeTypes.Created:
                    if (isDirectory)
                        SilentWave.Utility.ShellNotifier.Folder.Created(dokanFullPath);
                    else
                        SilentWave.Utility.ShellNotifier.File.Created(dokanFullPath);
                    break;
                case System.IO.WatcherChangeTypes.Deleted:
                    if (isDirectory)
                        SilentWave.Utility.ShellNotifier.Folder.Deleted(dokanFullPath);
                    else
                        SilentWave.Utility.ShellNotifier.File.Deleted(dokanFullPath);
                    break;
                case System.IO.WatcherChangeTypes.Changed:
                    if (isDirectory)
                        SilentWave.Utility.ShellNotifier.Folder.Updated(dokanFullPath);
                    else
                        SilentWave.Utility.ShellNotifier.File.Updated(dokanFullPath);
                    break;
                case System.IO.WatcherChangeTypes.Renamed:
                    var re = e as System.IO.RenamedEventArgs;
                    var dokanOldFullPath = "n" + re.OldFullPath.Substring(1);
                    if (isDirectory)
                        SilentWave.Utility.ShellNotifier.Folder.Renamed(dokanOldFullPath, dokanFullPath);
                    else
                        SilentWave.Utility.ShellNotifier.File.Renamed(dokanOldFullPath, dokanFullPath);
                    break;
                case System.IO.WatcherChangeTypes.All:
                    break;
                default:
                    break;
            }
        }
    }
}
