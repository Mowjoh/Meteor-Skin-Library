using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeteorSkinLibrary
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Mutex mt;
            if (Mutex.TryOpenExisting("mmsl_mutex", out mt))
            {
                System.IO.File.WriteAllLines(Application.StartupPath+"/mmsl_downloads/url.txt", args);
                Mutex test = new Mutex(true, "mmsl_url");
            }
            else
            {
                Mutex mutex = new Mutex(true, "mmsl_mutex");
                Application.Run(new main(args));
            }
        }
    }
}
