using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Meteor_Skin_Library
{
    class Logger
    {
        String path="";
        bool enabled = true;

        public Logger()
        {
            this.path = Application.StartupPath + "/mmsl_logs/log.txt";

            if(!Directory.Exists(Application.StartupPath + "/mmsl_logs")){
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_logs");
            }
        }

        public Logger(int mode)
        {

            if(mode == 1)
            {
                this.path = Application.StartupPath + "/mmsl_logs/log.txt";

                if (!Directory.Exists(Application.StartupPath + "/mmsl_logs"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "/mmsl_logs");
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public void log(String line)
        {
            if (enabled)
            {
                using (StreamWriter file = new StreamWriter(path, true))
                {
                    file.WriteLine(DateTime.Now.ToString() + " | " + line);
                }
            }
        }
    }
}
