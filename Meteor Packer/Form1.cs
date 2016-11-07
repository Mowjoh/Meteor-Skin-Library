using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            skin_dgv.Rows.Add("1", "Segtendwolf", "body/cXX | chr_00 | chr_11 | stock_segtendo");

        }


        public ArrayList searchfiles()
        {
            ArrayList files = new ArrayList();
            return files;
        }
    }
}
