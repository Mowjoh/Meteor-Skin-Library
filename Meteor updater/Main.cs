using SharpCompress.Common;
using SharpCompress.Reader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Meteor_updater
{
    public partial class Main : Form
    {
        //Setting class variables
        String version;
        String patchnotes;
        String app_path = Application.StartupPath;

        //Status
        Boolean downloadstatus;
        Boolean archivestatus;
        Boolean movestatus;
        Boolean deletestatus;

        public Main()
        {
            InitializeComponent();

            console_write("Welcome to Meteor Skin Library's uploader. It will now update so please be patient :D");

            //Write patch info
            write_patch();

            update_worker.RunWorkerAsync();
        }

        #region Console
        //Writes a line to the console
        public void console_write(String text)
        {
            console.Text += text + "\n";
        }

        public void console_write2(String text)
        {
            console.Text = text + "\n" + console.Text;
        }

        public void console_write_line()
        {

            console_write("------------------------------------------------------------------------------------------------------");
        }

        public void write_patch()
        {
            //Getting Xml Info
            XmlDocument xml = new XmlDocument();
            xml.Load("http://mmsl.lunaticfox.com/newcorepackage.xml");
            XmlNode nodes = xml.SelectSingleNode("package");
            XmlNodeList patches = xml.SelectNodes("package/patchnote");
            version = nodes.Attributes[0].Value;
            patchnotes = nodes.InnerText;


            console_write_line();
            console_write("This will update to version " + version + "\n");

            console_write_line();

            int i = 1;

            foreach (XmlElement patch in patches)
            {
                console_write("Patch " + patch.Attributes["version"].Value);
                XmlNodeList patchnodes = xml.SelectNodes("package/patchnote[attribute::version='" + patch.Attributes["version"].Value + "']/patch");
                foreach (XmlElement xe in patchnodes)
                {
                    console_write("- " + xe.InnerText + "\n");
                }
            }

        }

        private void Message()
        {
            
        }
        #endregion

        #region Worker
        private void update_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            download();

        }

        private void update_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if (progressBar1.Value == 50)
            {
                console_write("- Download Completed");
            }
            if (progressBar1.Value == 75)
            {
                console_write("- Archive Extracted");
            }
        }

        private void update_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
        }
        #endregion

        #region Files
        private void download()
        {
            if(File.Exists(app_path + "/build.zip"))
            {
                File.Delete(app_path + "/build.zip");
            }
            //Getting file
            using (WebClient webClient = new WebClient())
            {
                //Progress changed for loading box
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(delegate (object sender2, DownloadProgressChangedEventArgs e2)
                {

                });

                //When download is completed
                webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler
                    (delegate (object sender2, System.ComponentModel.AsyncCompletedEventArgs e2)
                    {
                        if (e2.Error == null && !e2.Cancelled)
                        {
                            this.downloadstatus = true;
                            //Doing the work
                            if (Directory.Exists(app_path + "/build"))
                            {
                                Directory.Delete(app_path + "/build", true);
                                Directory.CreateDirectory(app_path + "/build");
                            }
                            else
                            {
                                Directory.CreateDirectory(app_path + "/build");
                            }

                            try
                            {
                                //Extracting archive
                                ProcessStartInfo pro = new ProcessStartInfo();
                                pro.WindowStyle = ProcessWindowStyle.Hidden;
                                pro.FileName = Application.StartupPath + "/7za.exe";
                                String arguments = "x \"" + (app_path + "/build.zip") + "\" -o\"" + (app_path + "/build") + "\"";
                                pro.Arguments = arguments;
                                Process x = Process.Start(pro);
                                x.WaitForExit();

                                archivestatus = true;

                                try
                                {
                                    //Copying files
                                    String base_path = Application.StartupPath;
                                    //Copy all the files & Replaces any files with the same name
                                    foreach (string newPath in Directory.GetFiles(app_path + "/build", "*.*", SearchOption.AllDirectories))
                                    {
                                        String source = newPath;
                                        String dest = newPath.Replace(app_path + "/build", base_path);
                                        if (!Directory.Exists(Path.GetDirectoryName(dest)))
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(dest));
                                        }
                                        if (File.Exists(dest))
                                        {
                                            File.Delete(dest);
                                        }
                                        File.Copy(source, dest);
                                    }
                                    movestatus = true;
                                    //Deleting old files
                                    try
                                    {
                                        //Doing the work
                                        
                                        deletestatus = true;
                                    }
                                    catch
                                    {
                                        deletestatus = false;
                                    }
                                }
                                catch
                                {
                                    movestatus = false;
                                }
                            }
                            catch
                            {
                                archivestatus = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine(e2.Error);
                            downloadstatus = false;
                        }
                    });
                if(check_beta() == "1")
                {
                    webClient.DownloadFileAsync(new Uri("http://mmsl.lunaticfox.com/betabuild.zip"), app_path + "/build.zip");
                }else
                {
                    webClient.DownloadFileAsync(new Uri("http://mmsl.lunaticfox.com/newbuild.zip"), app_path + "/build.zip");
                }
               

            }
        }

        private void update()
        {

            



            

            
        }

        private String check_beta()
        {

            XmlDocument xml = new XmlDocument();
            xml.Load(Application.StartupPath+"/mmsl_config/Config.xml");
            XmlNode property = xml.SelectSingleNode("/config/property[attribute::name='beta']");
            if (property == null)
            {
                return "";
            }
            else
            {
                return property.InnerText;
            }
        }
        #endregion

        //Launch MMSL
        private void launch_MSL(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                console_write(Application.StartupPath + "/Meteor Skin Library.exe");
                pro.FileName = Application.StartupPath + "/Meteor Skin Library.exe";
                Process x = Process.Start(pro);
                Application.Exit();
            }
            catch (Exception e2)
            {
                console_write("The updater couldn't launch Meteor Skin Library");
            }

        }

        
    }
}
