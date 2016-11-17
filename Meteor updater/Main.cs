using SharpCompress.Common;
using SharpCompress.Reader;
using System;
using System.Collections;
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
        String last_version;
        ArrayList failed_files = new ArrayList();
        ArrayList success_files = new ArrayList();
        public Main()
        {
            InitializeComponent();

            console_write("Welcome to Meteor Skin Library's uploader. It will now update so please be patient :D");

            //Getting last version info
            this.last_version = search_update();

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
            //Getting remote info
            String remote_path = "http://lunaticfox.com/MSL/Application Files/Meteor Skin Library_" + last_version + "/patchnotes.xml";
            XmlDocument xml = new XmlDocument();
            xml.Load(remote_path);
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
            console_write2("-----------------------");

            foreach (String s in success_files)
            {
                console_write2("Updated following file: " + s);
            }
            foreach (String s in failed_files)
            {
                console_write2("Failed to update following file: "+s);
            }

            if(failed_files.Count == 0)
            {
                replace_manifest();
            }
            button1.Enabled = true;
        }
        #endregion

        #region Files
        private void download()
        {
            //Getting local version info
            XmlDocument local_xml = new XmlDocument();
            local_xml.Load(Application.StartupPath+ "/Meteor Skin Library.exe.manifest");
            XmlNode nodes = local_xml.SelectSingleNode("//*[local-name()='assembly']/*[local-name()='assemblyIdentity']");
            String local_version = nodes.Attributes[1].Value;
            local_version = local_version.Replace('.', '_');

            //Getting remote info
            String remote_path = "http://lunaticfox.com/MSL/Application Files/Meteor Skin Library_" + last_version + "/updatefiles.xml";
            XmlDocument files_xml = new XmlDocument();
            files_xml.Load(remote_path);
            XmlNodeList file_list = files_xml.SelectNodes("package/file");

            foreach (XmlElement xe in file_list)
            {
                String filepath = xe.InnerText;
                String fileversion = xe.Attributes[0].Value.ToString();
                String downloadpath = "http://lunaticfox.com/MSL/Application Files/Meteor Skin Library_" + last_version + "/" + filepath;
                String destinationpath = Path.GetDirectoryName(app_path + "/" + filepath);
                if (!Directory.Exists(destinationpath))
                {
                    Directory.CreateDirectory(destinationpath);
                }

                if (new_version(local_version, fileversion))
                {
                    //Getting file
                    using (WebClient webClient = new WebClient())
                    {
                        try
                        {
                            webClient.DownloadFile(new Uri(downloadpath), app_path + "/" + filepath);
                            this.success_files.Add(filepath);
                        }
                        catch
                        {
                            this.failed_files.Add(filepath);
                        }

                    }
                }
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

        private string search_update()
        {
            //Getting remote info
            String remote_path = "http://lunaticfox.com/MSL/Application Files/patchnotes.xml";
            XmlDocument xml = new XmlDocument();
            xml.Load(remote_path);
            XmlNode nodes = xml.SelectSingleNode("package");
            String version = nodes.Attributes[0].Value.ToString();
            version = version.Replace('.', '_');
            return version;

        }

        //Tells if the remoteversion is newer
        private Boolean new_version(String localversion, String remoteversion)
        {
            int l_major = int.Parse(localversion.Split('_')[0]);
            int l_minor = int.Parse(localversion.Split('_')[1]);
            int l_build = int.Parse(localversion.Split('_')[2]);
            int l_revision = int.Parse(localversion.Split('_')[3]);

            int r_major = int.Parse(remoteversion.Split('_')[0]);
            int r_minor = int.Parse(remoteversion.Split('_')[1]);
            int r_build = int.Parse(remoteversion.Split('_')[2]);
            int r_revision = int.Parse(remoteversion.Split('_')[3]);

            //remote major is superior
            if(r_major > l_major)
            {
                return true;
            }
            else
            {
                if(r_minor > l_minor)
                {
                    return true;
                }else
                {
                    if (r_build > l_build)
                    {
                        return true;
                    }
                    else
                    {
                        if(r_revision > l_revision)
                        {
                            return true;
                        }else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        private void replace_manifest()
        {
            String remote_path = "http://lunaticfox.com/MSL/Application Files/Meteor Skin Library_" + this.last_version + "/Meteor Skin Library.exe.manifest";
            XmlDocument files_xml = new XmlDocument();
            files_xml.Load(remote_path);
            files_xml.Save(Application.StartupPath + "/Meteor Skin Library.exe.manifest");
        }

    }
}
