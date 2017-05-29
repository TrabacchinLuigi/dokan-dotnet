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

        private void Application_Startup(object sender, StartupEventArgs e)
        {


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
    }
}
