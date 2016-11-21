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

        #region Class Variables
        String path = "";
        bool enabled = false;
        #endregion

        #region Contructors
        //Constructor
        public Logger(Boolean activated)
        {
            this.path = Application.StartupPath + "/mmsl_logs/log.txt";

            if (!Directory.Exists(Application.StartupPath + "/mmsl_logs"))
            {
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_logs");
            }
        }

        //Constructor that checks if it's already created
        public Logger(int mode, Boolean activated)
        {
            this.enabled = activated;
            if (mode == 1)
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
        #endregion

        #region Logging
        //Logs a line
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
        #endregion

    }
}
