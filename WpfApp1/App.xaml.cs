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
        public ObservableCollection<MirrorContext> Contexts { get; } = new ObservableCollection<MirrorContext>();

        public static new App Current => Application.Current as App;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    var mirror = new Mirror("C:");
                    mirror.ContextCreated += (x, y) =>
                    {
                        y.Closed += z =>
                        {
                            Dispatcher.InvokeAsync(async () =>
                            {
                                if (!z.HaveErrors)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(5));
                                    Contexts.Remove(z);
                                }
                            });
                        };
                        Dispatcher.InvokeAsync(async () =>
                        {
                            Contexts.Add(y);
                            await Task.Delay(TimeSpan.FromSeconds(10));
                           
                            if (!y.HaveErrors && y.RealContext == null) Contexts.Remove(y);
                        });
                    };
                    mirror.Mount("n:\\");

                    Console.WriteLine("Success");
                }
                catch (DokanException ex)
                {

                }
            });
        }
    }
}
