using System;
using MeteorSkinLibrary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using Meteor_Skin_Library.Items;
using System.IO;

namespace Meteor_Skin_Library
{
    public partial class FilebankWindow : Form
    {
        #region Class Variables
        Filebank filebank;
        LibraryHandler library;
        PropertyHandler properties;
        main main;


        #endregion
        public FilebankWindow(Filebank fb, LibraryHandler lb, PropertyHandler pp, main mn)
        {
            InitializeComponent();

            #region Handlers
            filebank = fb;
            library = lb;
            properties = pp;
            main = mn;
            #endregion
            init_characters();

        }


        public void init_characters()
        {
            ArrayList characters = library.get_character_list();
            foreach(String character in characters)
            {
                comboBox1.Items.Add(character);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            load_skins();

        }

        private void dataGridView1_Click(object sender, MouseEventArgs e)
        {
        }

        private void skin_cell_rightclick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Ignore if a column or row header is clicked
            if (e.RowIndex != -1 && e.ColumnIndex != -1)
            {
                if (e.Button == MouseButtons.Right)
                {
                    DataGridViewCell clickedCell = (sender as DataGridView).Rows[e.RowIndex].Cells[e.ColumnIndex];

                    // Here you can do whatever you want with the cell
                    this.dataGridView1.CurrentCell = clickedCell;  // Select the clicked cell, for instance

                    // Get mouse position relative to the vehicles grid
                    var relativeMousePosition = dataGridView1.PointToClient(Cursor.Position);

                    // Show the context menu
                    this.contextMenuStrip1.Show(dataGridView1, relativeMousePosition);
                }
            }
        }

        private void skin_delete(object sender, EventArgs e)
        {
            String character = comboBox1.SelectedItem.ToString();

            DataGridViewCell cell = this.dataGridView1.CurrentCell;
            DataGridViewRow row = dataGridView1.Rows[cell.RowIndex];
            NewSkin skin = new NewSkin(int.Parse(row.Cells[0].Value.ToString()), character, 0,library,properties,filebank);

            ArrayList library_skins = library.get_skins(character);
            Boolean id_check = false;
            foreach (String s in library_skins)
            {
                if (s.Split(';')[2] == row.Cells[0].Value.ToString())
                {
                    id_check = true;
                }
            }
            if (!id_check)
            {
                if (Directory.Exists(skin.filebank_folder))
                {
                    Directory.Delete(skin.filebank_folder, true);
                }
                filebank.delete_skin(comboBox1.SelectedItem.ToString(), int.Parse(row.Cells[0].Value.ToString()));
                load_skins();
                main.write("Skin removed from Filebank", 2);
            }else
            {
                main.write("You cannot delete an associated skin in the Filebank", 1);
            }

            

        }

        private void load_skins()
        {
            dataGridView1.Rows.Clear();
            String charname = comboBox1.SelectedItem.ToString();
            ArrayList skins = filebank.get_skins(charname);
            foreach (String[] skin in skins)
            {
                String[] newskin = { skin[0].Split(';')[1], skin[0].Split(';')[0], skin[1].Replace(';', ' '), skin[2].Replace(';', ' ') };
                dataGridView1.Rows.Add(newskin);
            }
        }

        private void insertSkinInLibraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String character = comboBox1.SelectedItem.ToString();
            DataGridViewCell cell = this.dataGridView1.CurrentCell;
            DataGridViewRow row = dataGridView1.Rows[cell.RowIndex];
            

            ArrayList library_skins = library.get_skins(character);
            Boolean id_check = false;
            foreach(String s in library_skins)
            {   
                if(s.Split(';')[2] == row.Cells[0].Value.ToString())
                {
                    id_check = true;
                }
            }

            if (!id_check)
            {
                NewSkin skin = new NewSkin(int.Parse(row.Cells[0].Value.ToString()), comboBox1.SelectedItem.ToString(), library_skins.Count + 1, library, properties, filebank);

                library.add_skin(character, library_skins.Count + 1, row.Cells[1].Value.ToString());
                library.set_id(skin);
                main.skin_ListBox_reload();
                main.write(skin.libraryname+" was added from the Filebank", 2);
            }


        }
    }
}
