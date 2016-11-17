using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Meteor_Packer
{
    public partial class Main : Form
    {
        //Setting up character values
        String[] characters = new String[] { "" };

        public Main()
        {
            InitializeComponent();
        }

        public ArrayList searchfiles()
        {
            ArrayList files = new ArrayList();
            return files;
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (String dir in Directory.GetDirectories(Application.StartupPath + "/mmsl_packages"))
            {
                Directory.Delete(dir, true);
            }
            if (File.Exists(Application.StartupPath + "/mmsl_packages/meta.xml"))
            {
                File.Delete(Application.StartupPath + "/mmsl_packages/meta.xml");
            }
            listView1.Enabled = true;
            listView1.Items.Clear();
            meteorpack_gridview.Rows.Clear();

        }
    }
}
