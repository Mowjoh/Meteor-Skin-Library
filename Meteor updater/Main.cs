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
        String version;
        String patchnotes;
        String app_path = Application.StartupPath;

        public Main()
        {
            InitializeComponent();

            //Getting Xml Info
            XmlDocument xml = new XmlDocument();
            xml.Load("http://mmsl.lunaticfox.com/newcorepackage.xml");
            XmlNode nodes = xml.SelectSingleNode("package");
            XmlNodeList patches = xml.SelectNodes("package/patchnote");
            version = nodes.Attributes[0].Value;
            patchnotes = nodes.InnerText;

            console_write("Welcome to Meteor Skin Library's uploader. It will now update so please be patient :D");
            console_write_line();
            console_write("This will update to version " + version+"\n");
            int i = 1;

            foreach(XmlElement patch in patches)
            {
                console_write("Patch "+patch.Attributes["version"].Value);
                XmlNodeList patchnodes = xml.SelectNodes("package/patchnote[attribute::version='" + patch.Attributes["version"].Value + "']/patch");
                foreach(XmlElement xe in patchnodes)
                {
                    console_write("- " + xe.InnerText+"\n");
                }
            }

            console_write_line();

            console_write("- Downloading");
            console_write("- Installing");

            update_worker.RunWorkerAsync();

        }
        //Writes a line to the console
        public void console_write(String text)
        {
            console.Text += text + "\n";
        }

        public void console_write_line()
        {

            console_write("------------------------------------------------------------------------------------------------------");
        }

        private void update_worker_DoWork(object sender, DoWorkEventArgs e)
        {
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
                            //Extracting archive
                            using (Stream stream = File.OpenRead(app_path + "/build.zip"))
                            {
                                var reader = ReaderFactory.Open(stream);
                                while (reader.MoveToNextEntry())
                                {
                                    if (!reader.Entry.IsDirectory)
                                    {
                                        reader.WriteEntryToDirectory(app_path + "/build", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                                    }
                                }
                                reader.Dispose();
                            }

                            //Doing the work
                            File.Delete(app_path + "/build.zip");
                            //Copying files
                            String base_path = Application.StartupPath;
                            //Copy all the files & Replaces any files with the same name
                            foreach (string newPath in Directory.GetFiles(app_path + "/build", "*.*", SearchOption.AllDirectories))
                            {
                                if(!Directory.Exists(Path.GetDirectoryName(newPath.Replace(app_path + "/build", base_path))))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(newPath.Replace(app_path + "/build", base_path)));
                                }
                                if(File.Exists(newPath.Replace(app_path + "/build", base_path)))
                                {
                                    File.Delete(newPath.Replace(app_path + "/build", base_path));
                                }
                                
                                File.Copy(newPath, newPath.Replace(app_path + "/build", base_path), true);
                            }

                            Directory.Delete(app_path + "/build", true);
                        }
                        else
                        {

                        }
                    });

                webClient.DownloadFileAsync(new Uri("http://mmsl.lunaticfox.com/newbuild.zip"),app_path+"/build.zip");
            }
        }

        private void update_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if(progressBar1.Value == 50)
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
            console_write("- Update complete");
            button1.Enabled = true;
        }

        //Launch MMSL
        private void button1_Click(object sender, EventArgs e)
        {
            ProcessStartInfo pro = new ProcessStartInfo();
            console_write(Application.StartupPath + "/Meteor Skin Library.exe");
            pro.FileName = Application.StartupPath + "/Meteor Skin Library.exe";
            Process x = Process.Start(pro);
            Application.Exit();
        }
    }
}
