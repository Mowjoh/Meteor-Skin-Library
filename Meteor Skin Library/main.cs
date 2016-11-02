using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using SharpCompress.Reader;
using SharpCompress.Common;
using System.Diagnostics;
using System.Threading;
using SharpCompress.Archive;
using SharpCompress.Archive.Zip;
using SharpCompress.Writer;
using System.Xml;
using System.Drawing;
using Meteor_Skin_Library;

namespace MeteorSkinLibrary
{
    public partial class main : Form
    {
        //Variables
        #region Handlers
        LibraryHandler Library;
        PropertyHandler properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Default_Config.xml");
        MetaHandler meta = new MetaHandler(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml");
        UICharDBHandler uichar;
        #endregion
        #region SelectedInfo


        //Variables redone
        Skin selected_skin;
        Character selected_char;
        String last_char = "";

        #endregion
        #region Lists
        //Lists for soft
        ArrayList Characters = new ArrayList();
        ArrayList Skins = new ArrayList();

        ArrayList ui_char_db_values = new ArrayList();
        #endregion
        #region Files
        //Selected Files
        String[] model_folder_list;
        String[] csp_file_list;
        String[] slot_file_list;
        #endregion
        #region Processing
        Boolean processing;
        #endregion
        #region Errorcodes
        int downloadcode = 0;
        int extractcode = 0;
        int archivecode = 0;
        int meteorcode = 0;
        int importcode = 0;
        int exportcode = 0;

        #endregion

        Boolean fakeargs = false;

        public main(String[] args)
        {
            InitializeComponent();

            #region Library and Config
            Boolean lib = true;
            Boolean conf = true;

            //Checks Default_Library.xml presence
            if (!File.Exists(Application.StartupPath + "/mmsl_config/Default_Library.xml"))
            {
                console_write("Default Library not found, please add Default_Library.xml in the /mmsl_config folder.");
                lib = false;
            }
            //Check Default_Config.xml presence
            if (!File.Exists(Application.StartupPath + "/mmsl_config/Default_Config.xml"))
            {
                console_write("Default Config not found, please add Default_Config.xml in the /mmsl_config folder.");
                conf = false;
            }

            #endregion
            

            if (conf && lib)
            {
                #region Default Copy
                //Checks Config.xml presence, if not creates one based on Default_Config.xml
                if (!File.Exists(Application.StartupPath + "/mmsl_config/Config.xml"))
                {
                    console_write("Creating Config");
                    File.Copy(properties.get("default_config"), Application.StartupPath + "/mmsl_config/Config.xml");
                }
                properties.set_library_path(Application.StartupPath + "/mmsl_config/Config.xml");
                properties.add("current_library", Application.StartupPath + "/mmsl_config/Library.xml");
                console_write("Config loaded : mmsl_config/Config.xml");

                //Checks Library.xml presence, if not creates one based on Default_Library.xml
                if (!File.Exists(Application.StartupPath + "/mmsl_config/Library.xml"))
                {
                    console_write("Creating Library");
                    File.Copy(properties.get("default_library"), Application.StartupPath + "/mmsl_config/Library.xml");
                }
                Library = new LibraryHandler(properties.get("current_library"));
                console_write("Library loaded : mmsl_config/Library.xml");
                #endregion

                #region UI Init
                //Loads Character List
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
                appstatus.Text = "Ready";
                processing = false;
                url_worker.RunWorkerAsync();
                reset_skin_pack_session();
                #endregion

                //Launches config if not edited
                region_select();

                #region ui_char_db 
                uichar = new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));
                if (!uichar.imported)
                {
                    console_write("ui_character_db was not found in Sm4sh Explorer, please add it and relaunch this software!");
                }
                else
                {
                    console_write("ui_character_db was found, congrats !");
                }
                #endregion

                #region melee.msbt
                Melee melee = new Melee();
                if (melee.check_file())
                {
                    console_write("melee.msbt file found, congrats!");
                }else
                {
                    console_write("melee.msbt was not found in S4E's workspace or extract folder");
                }
                #endregion

                //If arguments are passed
                if (args.Length > 0 | (fakeargs == true))
                {
                    //Launch download process
                    processing = true;
                    block_controls();
                    meteor_download(args);
                }
                else
                {
                    //Launch update process
                    check_updates();
                    check_updater();
                }


            }

        }

        //Top Menu
        #region Menu
        #region FileMenu 
        //Menu Exit Function
        private void menu_software_exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //Open mmsl_workspace Function
        private void openmmsl_workspace(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath);
        }
        private void launchS4EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            launch_s4e();
        }

        private void launch_s4e()
        {
            ProcessStartInfo pro = new ProcessStartInfo();
            String s4path = properties.get("explorer_workspace");
            String path = Directory.GetParent(s4path).ToString() + "/Sm4shFileExplorer.exe";
            pro.FileName = path;
            pro.WorkingDirectory = Directory.GetParent(s4path).ToString();
            Process x = Process.Start(pro);
        }
        #endregion
        #region SkinMenu 
        //When Add Skin is pressed
        private void skin_add(object sender, EventArgs e)
        {
            if (!processing)
            {

                if (Characterlist2.SelectedIndices[0] != -1)
                {
                    selected_char.add_skin();
                    console_write("Skin added for " + selected_char.fullname + " in slot " + (SkinListBox.Items.Count + 1));
                    skin_ListBox_reload();
                    state_check();
                    SkinListBox.SelectedIndex = SkinListBox.Items.Count - 1;
                }
                else
                {
                    console_write("Please select a Character first");
                }
            }


        }

        //Reset skin pack session is pressed
        private void resetCurrentMeteorSkinPackSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reset_skin_pack_session();
        }

        //Deletes the packed skins
        private void reset_skin_pack_session()
        {
            foreach (String dir in Directory.GetDirectories(Application.StartupPath + "/mmsl_packages"))
            {
                Directory.Delete(dir, true);
            }
            console_write("Meteor skin pack session reseted");
        }

        //Archives the current skin pack session
        private void archiveCurrentMeteorSkinPackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!processing)
            {
                archive_worker.RunWorkerAsync();
                loadingbox.Style = ProgressBarStyle.Marquee;
                appstatus.Text = "Archiving files...";
                processing = true;
                block_controls();
            }

        }
        #endregion
        #region OptionMenu 
        //Menu Config Function
        public void menu_config(object sender, EventArgs e)
        {
            if (!processing)
            {
                config cnf = new config();
                cnf.ShowDialog();
                state_check();
            }

        }
        //Menu Reset Library
        private void menu_reset_library(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase all entries in the Library. Skins are still present in the mmsl_workspace folder. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");

                    state_check();
                    console_write("Library reset complete");
                }

                SkinListBox.SelectedIndex = -1;
                Characters = Library.get_character_list();
                init_character_ListBox();
                Characterlist2.Items[0].Selected = true;
                state_check();
            }
        }
        //mmsl_workspace reset button
        private void reset_mmsl_workspace(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase all contents of the mmsl_workspace folder which contains every file you've added. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    batch_delete(Application.StartupPath + "/mmsl_workspace");
                    Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
                    console_write("mmsl_workspace reset complete");
                }
                SkinListBox.SelectedIndex = -1;
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
            }

        }
        //Config Reset button
        private void reset_config(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase all configuration changes. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    File.Delete(Application.StartupPath + "/mmsl_config/Config.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Config.xml", Application.StartupPath + "/mmsl_config/Config.xml");

                    config cnf = new config();

                    cnf.ShowDialog();
                    state_check();
                    console_write("Config reset complete");

                }

                SkinListBox.SelectedIndex = -1;
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
            }

        }
        //Reset all button
        private void reset_all(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase all configuration changes. It will erase all files of every mod you've added. The library containing skin information will be deleted. Continue with this Supermassive black-hole type destruction?", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    File.Delete(Application.StartupPath + "/mmsl_config/Config.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Config.xml", Application.StartupPath + "/mmsl_config/Config.xml");

                    config cnf = new config();

                    cnf.ShowDialog();
                    state_check();
                    console_write("Config reset complete");

                    if (Directory.Exists(Application.StartupPath + "/mmsl_workspace"))
                    {
                        String[] files = Directory.GetFiles(Application.StartupPath + "/mmsl_workspace", "*", SearchOption.AllDirectories);
                        foreach (String file in files)
                        {
                            File.Delete(file);
                        }
                        Directory.Delete(Application.StartupPath + "/mmsl_workspace", true);
                        Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
                    }
                    else
                    {
                        Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
                    }
                    if (!Directory.Exists(Application.StartupPath + "/mmsl_workspace"))
                    {
                        Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
                    }
                    console_write(Application.StartupPath + "/mmsl_workspace reset complete");

                    File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");

                    console_write("Library reset complete");


                    SkinListBox.SelectedIndex = -1;
                    Characters = Library.get_character_list();
                    init_character_ListBox();
                    state_check();
                }
            }
        }
        //Reset all button
        private void reset_all()
        {

            if (Directory.Exists(Application.StartupPath + "/mmsl_workspace"))
            {
                String[] files = Directory.GetFiles(Application.StartupPath + "/mmsl_workspace", "*", SearchOption.AllDirectories);
                foreach (String file in files)
                {
                    File.Delete(file);
                }
                Directory.Delete(Application.StartupPath + "/mmsl_workspace", true);
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
            }
            else
            {
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
            }
            if (!Directory.Exists(Application.StartupPath + "/mmsl_workspace"))
            {
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");
            }

            File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
            File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");
            new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));

        }
        #endregion
        #region SmashExplorerMenu 
        private void launch_se_import(object sender, EventArgs e)
        {
            {
                if (MessageBox.Show("It will erase all files of every mod you've added in Meteor Skin Library to replace them by S4E's mods. Continue with this Supermassive black-hole type destruction?", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    processing = true;
                    block_controls();
                    reset_all();
                    loadingbox.Show();
                    loadingbox.Value = 0;
                    loadingbox.Style = ProgressBarStyle.Continuous;
                    appstatus.Text = "Importing from Sm4sh Explorer";
                    import_worker.RunWorkerAsync();
                }
            }


        }
        private void launch_se_export(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase fighter/[name]/model for every character that has mods and ui/replace/chr and ui/replace/append/chr from Smash Explorer's workspace. Are you sure you've made a backup? If yes, you can validate these changes and replace S4E's content by MSL's content", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    processing = true;
                    block_controls();
                    loadingbox.Show();
                    loadingbox.Value = 0;
                    loadingbox.Style = ProgressBarStyle.Continuous;
                    appstatus.Text = "Exporting to Sm4sh Explorer";
                    export_worker.RunWorkerAsync();
                }
            }

        }
        #endregion
        #endregion

        //Main Control Area
        #region Character Tab
        //When a character is selected
        private void character_selected(object sender, EventArgs e)
        {
            selected_char = new Character(Characterlist2.SelectedItems[0].Text);
            skin_ListBox_reload();
            state_check();


        }

        //When a character is selected NEW
        private void Characterlist2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Characterlist2.SelectedItems.Count > 0)
            {
                selected_char = new Character(Characterlist2.SelectedItems[0].Text);
                skin_ListBox_reload();
                state_check();
                textBox6.Text = "";
            }

        }

        //UI char db override settings saved
        private void button12_Click(object sender, EventArgs e)
        {
            int val = 0;
            if (int.TryParse(textBox6.Text, out val))
            {
                uichar.setFile(int.Parse(Library.get_ui_char_db_id(Characterlist2.SelectedItems[0].Text)), 7, val);
                console_write("Override ui_character_db settings saved");
            }
            else
            {
                console_write("What you entered wasn't a number");
            }

        }
        #endregion
        #region Skin Tab 
        //When a skin is selected
        private void skin_selected(object sender, EventArgs e)
        {
            skin_details_reload();
            state_check();
            metadata_reload();
        }

        //Skin Info Saved button is pressed
        private void set_skin_libraryname(object sender, EventArgs e)
        {

            int index = SkinListBox.SelectedIndex;
            this.selected_skin.set_library_name(SkinNameText.Text);
            skin_ListBox_reload();
            state_check();
            SkinListBox.SelectedIndex = index;


        }

        //Skin Info Saved by enter key press
        private void SkinNameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int index = SkinListBox.SelectedIndex;
                this.selected_skin.set_library_name(SkinNameText.Text);
                skin_ListBox_reload();
                state_check();
                SkinListBox.SelectedIndex = index;
            }
        }

        //When Delete is pressed
        private void skin_delete(object sender, EventArgs e)
        {
            int index = SkinListBox.SelectedIndex + 1;
            int saved_index = SkinListBox.SelectedIndex;
            int max = SkinListBox.Items.Count;

            if (selected_skin.origin == "Default" | selected_skin.origin == "Default Replaced")
            {
                console_write("Thy cannot delete Default slots");
            }
            else
            {
                selected_char.delete_skin(this.selected_skin.modelslot);
                Library.reload_skin_order(selected_char.fullname);
                selected_char.getSkins();


                skin_ListBox_reload();
                skin_details_reload();
                console_write("Deleted slot " + index);
            }

            state_check();

            uichar.setFile(int.Parse(Library.get_ui_char_db_id(Characterlist2.SelectedItems[0].Text)), 7, SkinListBox.Items.Count);
            if (!(saved_index + 1 < SkinListBox.Items.Count))
            {
                SkinListBox.SelectedIndex = saved_index - 1;
            }
            else
            {
                SkinListBox.SelectedIndex = SkinListBox.Items.Count - 1;
            }
        }

        //When Clean Files is pressed
        private void clean_files_clicked(object sender, EventArgs e)
        {
            this.selected_skin.clean_skin(0);

            skin_details_reload();
            skin_ListBox_reload();
            state_check();

        }

        //packages skin into meteor skin
        private void package_meteor(object sender, EventArgs e)
        {
            this.selected_skin.package_meteor();
            console_write("This skin was added to the current pack session");
        }

        //When the move up button is pressed
        private void move_up_skin(object sender, EventArgs e)
        {
            if (selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndex, SkinListBox.SelectedIndex - 1);
                skin_ListBox_reload();
                SkinListBox.SelectedIndex = selected_skin.modelslot;
            }

        }

        //When the move down button is pressed
        private void move_down_skin(object sender, EventArgs e)
        {
            if (selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndex, SkinListBox.SelectedIndex + 1);
                skin_ListBox_reload();
                SkinListBox.SelectedIndex = selected_skin.modelslot;
            }

        }
        #endregion
        #region Model Zone 
        //On model selected
        private void model_selected(object sender, EventArgs e)
        {
            if (models_ListView.SelectedItems.Count == 1)
            {
                label5.Text = "Selected Model : " + models_ListView.SelectedItems[0].Text;
                button6.Enabled = true;
            }
            state_check();
        }
        //On model delete
        private void remove_selected_model_Click(object sender, EventArgs e)
        {
            selected_skin.delete_model(models_ListView.SelectedItems[0].Text);
            skin_details_reload();
            state_check();
        }
        #endregion
        #region Csp Zone
        //When a csp is selected
        private void csp_selected(object sender, EventArgs e)
        {
            if (csps_ListView.SelectedItems.Count == 1)
            {
                selected_csp_name.Text = "Selected CSP : " + csps_ListView.SelectedItems[0].Text;
                remove_selected_csp.Enabled = true;
            }
            state_check();
        }
        //When a csp is deleted
        private void remove_selected_csp_Click(object sender, EventArgs e)
        {
            this.selected_skin.delete_csp(csps_ListView.SelectedItems[0].Text);
            skin_details_reload();
            state_check();
        }
        #endregion
        #region Meta Tab
        //When you save all metadata
        void meta_save(object sender, EventArgs e)
        {
            String author = textBox1.Text;
            String version = textBox2.Text;
            String name = textBox3.Text;
            String texidfix = textBox4.Text;
            this.selected_skin.saveMeta(author, version, name, texidfix);
        }
        #endregion

        //Interface functions
        #region Interface

        #region Reloads 
        //Reloads Skin Details 
        private void skin_details_reload()
        {

            int slot = SkinListBox.SelectedIndex;
            //emptying lists
            if (slot != -1)
            {
                selected_char.getSkins();
                this.selected_skin = (Skin)selected_char.skins[slot];
                csps_ListView.Clear();
                models_ListView.Clear();
                remove_selected_csp.Enabled = false;

                SlotNumberText.Text = this.selected_skin.slotstring;
                SkinNameText.Text = this.selected_skin.libraryname;
                SkinOriginText.Text = this.selected_skin.origin;

                //checking csps
                if (this.selected_skin.csps.Count > 0)
                {
                    //adding csps
                    foreach (String csp in this.selected_skin.csps)
                    {
                        csps_ListView.Items.Add(csp);
                    }
                }
                //Checking models
                if (this.selected_skin.models.Count > 0)
                {
                    //adding models
                    foreach (String model in this.selected_skin.models)
                    {
                        models_ListView.Items.Add(model);
                    }
                }
                //setting delete button
                if (this.selected_skin.origin != "Default")
                {
                    button3.Enabled = true;
                }
                else
                {
                    button3.Enabled = false;
                }

                button2.Enabled = true;
            }



        }
        //Reloads Skin List
        private void skin_ListBox_reload()
        {

            if (Characterlist2.SelectedIndices.Count > 0)
            {
                SkinListBox.Items.Clear();
                selected_char.getSkins();
                foreach (Skin skin in selected_char.skins)
                {
                    SkinListBox.Items.Add("Slot " + skin.slotstring + " - " + skin.libraryname);
                }
            }
        }
        //Reloads MetaData
        private void metadata_reload()
        {
            if (SkinListBox.SelectedIndex != -1)
            {
                //Assign values
                textBox1.Text = this.selected_skin.author;
                textBox2.Text = this.selected_skin.version;
                textBox3.Text = this.selected_skin.metaname;
                textBox4.Text = this.selected_skin.texidfix;
            }

        }

        #endregion
        #region Drag&Drop
        private void model_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void model_DragDrop(object sender, DragEventArgs e)
        {
            this.model_folder_list = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            batch_copy_model(this.model_folder_list, this.selected_skin);
            skin_details_reload();
            state_check();
            skin_details_reload();
        }
        private void csp_DragEnter2(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void csp_DragDrop2(object sender, DragEventArgs e)
        {
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (Directory.Exists(FileList[0]))
            {
                this.csp_file_list = FileList;
                batch_copy_csp(FileList, SkinListBox.SelectedIndex);
                skin_details_reload();
                state_check();
            }
            else
            {
                //textBox6.Text = "Item wasn't a Directory";
                if (FileList.Length > 0)
                {
                    foreach (String file in FileList)
                    {
                        selected_skin.add_csp(file);
                        console_write("Detected files were moved to the selected slot");
                        skin_details_reload();
                    }
                }
            }


        }
        private void slot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void slot_DragDrop(object sender, DragEventArgs e)
        {
            this.slot_file_list = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            batch_add_slot(SkinListBox.Items.Count + 1);
            state_check();
            skin_details_reload();
        }
        #endregion
        #region Inits 
        //Filling Character list
        public void init_character_ListBox()
        {
            Characterlist2.Items.Clear();
            ImageList images = new ImageList();
            images.ImageSize = new Size(24, 24);
            Characterlist2.View = View.Details;
            for (int j = 0; j < Characters.Count; j++)
            {
                String chars = (String)Characters[j];

                if (File.Exists(Application.StartupPath + "/mmsl_img/icons/" + chars + ".png"))
                {
                    images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/icons/" + chars + ".png"));
                }
                ListViewItem item = new ListViewItem(chars);
                item.ImageIndex = j;
                Characterlist2.Items.Add(item);
            }
            Characterlist2.SmallImageList = images;


        }

        //Region selecter
        public void region_select()
        {
            if (!properties.check("datafolder"))
            {
                config cnf = new config();
                cnf.ShowDialog();
            }

            state_check();
        }
        #endregion
        #region Console 
        //Writes string to console
        private void console_write(String s)
        {
            textConsole.Text = s + "\n" + textConsole.Text;
        }
        #endregion
        #region State 
        //State Checker
        private void state_check()
        {
            int character = Characterlist2.SelectedIndices.Count > 0 ? Characterlist2.SelectedIndices[0] : -1;
            int skin = SkinListBox.SelectedIndex;
            int model = models_ListView.SelectedIndices.Count;
            int csp = csps_ListView.SelectedIndices.Count;
            String origin = SkinOriginText.Text;

            if (character == -1)
            {
                //Interactions
                models_ListView.AllowDrop = false;
                csps_ListView.AllowDrop = false;
                meteorbox.AllowDrop = false;
                button1.Enabled = false;
                button12.Enabled = false;

                //State
            }
            else
            {
                //Interactions
                meteorbox.AllowDrop = true;
                button12.Enabled = true;
            }

            if (skin == -1)
            {
                //Interactions
                models_ListView.AllowDrop = false;
                csps_ListView.AllowDrop = false;
                SkinNameText.Enabled = false;
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                textBox1.ReadOnly = true;
                textBox2.ReadOnly = true;
                textBox3.ReadOnly = true;
                textBox4.ReadOnly = true;

                SlotNumberText.Text = "";
                SkinOriginText.Text = "";
                SkinNameText.Text = "";

                models_ListView.Items.Clear();
                csps_ListView.Items.Clear();

                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                //State
            }
            else
            {
                //Interactions
                models_ListView.AllowDrop = true;
                csps_ListView.AllowDrop = true;
                SkinNameText.Enabled = true;
                button1.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                textBox1.ReadOnly = false;
                textBox2.ReadOnly = false;
                textBox3.ReadOnly = false;
                textBox4.ReadOnly = false;
                if (origin == "default")
                {
                    button2.Enabled = true;
                    button3.Enabled = true;
                }
                else
                {
                    button2.Enabled = true;
                }

            }
            if (model == 0)
            {
                button6.Enabled = false;
            }
            else
            {
                remove_selected_csp.Enabled = true;
            }
            if (csp == 0)
            {
                remove_selected_csp.Enabled = false;
            }
            else
            {
                remove_selected_csp.Enabled = true;
            }



        }

        private void block_controls()
        {
            //Interactions
            models_ListView.AllowDrop = false;
            csps_ListView.AllowDrop = false;
            SkinNameText.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox4.ReadOnly = true;

            SlotNumberText.Text = "";
            SkinOriginText.Text = "";
            SkinNameText.Text = "";

            models_ListView.Items.Clear();
            csps_ListView.Items.Clear();

            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";

            SkinListBox.Enabled = false;
            Characterlist2.Enabled = false;
            meteorbox.Enabled = false;

            button7.Enabled = false;
            button8.Enabled = false;

            button12.Enabled = false;
        }
        private void enable_controls()
        {
            SkinListBox.Enabled = true;
            Characterlist2.Enabled = true;
            meteorbox.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button12.Enabled = true;
        }
        #endregion
        #region Updates
        //Checks for updates in the newcorepackage.xml
        private void check_updates()
        {

            XmlDocument xml = new XmlDocument();
            xml.Load("http://mmsl.lunaticfox.com/newcorepackage.xml");
            XmlNode nodes = xml.SelectSingleNode("package");
            String version = nodes.Attributes[0].Value;
            int major = int.Parse(version.Split('.')[1]);
            int minor = int.Parse(version.Split('.')[2]);
            int build = int.Parse(version.Split('.')[3]);

            XmlDocument xml2 = new XmlDocument();
            xml2.Load(Application.StartupPath + "/newcorepackage.xml");
            XmlNode nodes2 = xml2.SelectSingleNode("package");
            String version2 = nodes2.Attributes[0].Value;
            int major2 = int.Parse(version2.Split('.')[1]);
            int minor2 = int.Parse(version2.Split('.')[2]);
            int build2 = int.Parse(version2.Split('.')[3]);

            //update
            if (major > major2)
            {
                update();
            }
            else
            {
                //same major version
                if (major == major2)
                {
                    //update
                    if (minor > minor2)
                    {
                        update();
                    }
                    else
                    {
                        //same minor version
                        if (minor == minor2)
                        {
                            //update
                            if (build > build2)
                            {
                                update();
                            }
                        }
                    }
                }

            }



        }

        //Launches the update process
        private void update()
        {
            if (MessageBox.Show("An update is available, Do you wish to download it? It will close Meteor Skin Library and launch the updater", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.FileName = Application.StartupPath + "/Meteor updater.exe";
                Process x = Process.Start(pro);
                Environment.Exit(0);
            }
        }

        //Checks for updates in the updatepackage.xml
        private void check_updater()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("http://mmsl.lunaticfox.com/updatepackage.xml");
            XmlNode nodes = xml.SelectSingleNode("package");
            String version = nodes.Attributes[0].Value;
            int major = int.Parse(version.Split('.')[1]);
            int minor = int.Parse(version.Split('.')[2]);
            int build = int.Parse(version.Split('.')[3]);

            XmlDocument xml2 = new XmlDocument();
            xml2.Load(Application.StartupPath + "/updatepackage.xml");
            XmlNode nodes2 = xml2.SelectSingleNode("package");
            String version2 = nodes2.Attributes[0].Value;
            int major2 = int.Parse(version2.Split('.')[1]);
            int minor2 = int.Parse(version2.Split('.')[2]);
            int build2 = int.Parse(version2.Split('.')[3]);

            //update
            if (major > major2)
            {
                update();
            }
            else
            {
                //same major version
                if (major == major2)
                {
                    //update
                    if (minor > minor2)
                    {
                        update();
                    }
                    else
                    {
                        //same minor version
                        if (minor == minor2)
                        {
                            //update
                            if (build > build2)
                            {
                                update_updater();
                            }
                        }
                    }
                }

            }

        }

        //Updates the updater
        private void update_updater()
        {
            if (MessageBox.Show("An update for the updater is available, do you want to install it?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                                using (Stream stream = File.OpenRead(Application.StartupPath + "/updater.zip"))
                                {
                                    var reader = ReaderFactory.Open(stream);
                                    while (reader.MoveToNextEntry())
                                    {
                                        if (!reader.Entry.IsDirectory)
                                        {
                                            reader.WriteEntryToDirectory(Application.StartupPath + "/updater", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                                        }
                                    }
                                    reader.Dispose();
                                }

                                //Doing the work
                                File.Delete(Application.StartupPath + "/updater.zip");
                                //Copying files
                                String base_path = Application.StartupPath;
                                //Copy all the files & Replaces any files with the same name
                                foreach (string newPath in Directory.GetFiles(Application.StartupPath + "/updater", "*.*", SearchOption.AllDirectories))
                                {
                                    File.Copy(newPath, newPath.Replace(Application.StartupPath + "/updater", base_path), true);
                                }

                                Directory.Delete(Application.StartupPath + "/updater", true);
                            }
                            else
                            {

                            }
                            console_write("Updaterception : Updater updated, please relaunch the app");
                        });

                    webClient.DownloadFileAsync(new Uri("http://mmsl.lunaticfox.com/updater.zip"), Application.StartupPath + "/updater.zip");
                }
            }
        }
        #endregion
        #region Errors
        private void downloadstatus()
        {

        }
        private void meteorstatus()
        {
            switch (meteorcode)
            {
                case -1:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("- An error happened during the installation");
                    console_write("Installation Status:");
                    console_write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
                case 1:
                    appstatus.Text = "Skins installed";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("- All the skins where installed");
                    console_write("Installation Status:");
                    console_write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
                case 2:
                    appstatus.Text = "No skins found";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("- Please check that the meteor generated folders ( like \"Mario\" ) are at the root (or base folder) of the archive");
                    console_write("Installation Status: No meteor skins found");
                    console_write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
            }
        }
        private void exportstatus()
        {
            switch (exportcode)
            {
                case 1:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Directory error: \n Could not delete the fighter/[name]/model folders. Please remove them manually and retry.");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 2:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Directory error: \n Could not delete the data/ui/replace/chr folder. Please remove it manually and retry.");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 3:
                    appstatus.Text = "Export Success";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("The export was successful");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
            }
        }
        private void importstatus()
        {
            switch (importcode)
            {
                case 1:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Directory error: \n Could not delete the fighter/[name]/model folders. Please remove them manually and retry.");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 2:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Directory error: \n Could not delete the data/ui/replace/chr folder. Please remove it manually and retry.");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 3:
                    appstatus.Text = "Export Success";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("The export was successful");
                    console_write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
            }
        }
        #endregion

        #endregion

        //Threading functions
        #region Threading
        #region Download
        //Launches the download for a specified path
        private void meteor_download(String[] args)
        {

            loadingbox.Value = 0;
            //Setting default path
            String http_url = "http://lunaticfox.com/pack.zip";
            String file_ext = "zip";

            //If URL is passed
            if (args.Length > 0)
            {
                try
                {
                    //Getting url
                    http_url = args[0].Split(':')[1] + ":" + args[0].Split(':')[2];
                    //Getting extension
                    file_ext = http_url.Split('.')[http_url.Split('.').Length - 1];
                }
                catch (Exception e2)
                {
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Download error: \n The meteor link is invalid");
                    console_write("----------------------------------------------------------------------------------------------------");
                }

            }
            //Setting download paths
            String base_path = Application.StartupPath + "/mmsl_downloads/";
            String file_path = Application.StartupPath + "/mmsl_downloads/";
            //Getting file

            using (WebClient webClient = new WebClient())
            {


                //Progress changed for loading box
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    loadingbox.Style = ProgressBarStyle.Continuous;
                    loadingbox.Value = e.ProgressPercentage;
                    appstatus.Text = "Downloading";
                });

                //When download is completed
                webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler
                    (delegate (object sender, AsyncCompletedEventArgs e)
                    {
                        if (e.Error == null && !e.Cancelled)
                        {
                            file_path += "meteorpack." + file_ext;
                            if (file_ext == "7z")
                            {
                                try
                                {
                                    if (Directory.Exists(base_path + "archive"))
                                    {
                                        Directory.Delete(base_path + "archive", true);
                                    }
                                    Directory.CreateDirectory(base_path + "archive");
                                    try
                                    {
                                        //Extracting archive
                                        ProcessStartInfo pro = new ProcessStartInfo();
                                        pro.WindowStyle = ProcessWindowStyle.Hidden;
                                        pro.FileName = Application.StartupPath + "/7za.exe";
                                        String arguments = "x \"" + (file_path) + "\" -o\"" + (base_path) + "archive/\"";
                                        pro.Arguments = arguments;
                                        Process x = Process.Start(pro);
                                        x.WaitForExit();

                                        //Launching the import
                                        loadingbox.Value = 0;
                                        appstatus.Text = "Importing Meteor Skins";
                                        meteor_worker.RunWorkerAsync();
                                    }
                                    catch (Exception e3)
                                    {
                                        appstatus.Text = "Error";
                                        console_write("----------------------------------------------------------------------------------------------------");
                                        console_write("Download error: \n An error has appened during the extraction of the archive");
                                        console_write("----------------------------------------------------------------------------------------------------");
                                    }

                                }
                                catch (Exception e2)
                                {
                                    appstatus.Text = "Error";
                                    console_write("----------------------------------------------------------------------------------------------------");
                                    console_write("Download error: \n The previous archive couldn't be deleted");
                                    console_write("----------------------------------------------------------------------------------------------------");
                                }
                            }
                            else
                            {
                                try
                                {
                                    if (Directory.Exists(base_path + "archive"))
                                    {
                                        Directory.Delete(base_path + "archive", true);
                                    }
                                    Directory.CreateDirectory(base_path + "archive");
                                    try
                                    {
                                        //Extracting archive
                                        using (Stream stream = File.OpenRead(file_path))
                                        {
                                            var reader = ReaderFactory.Open(stream);
                                            while (reader.MoveToNextEntry())
                                            {
                                                if (!reader.Entry.IsDirectory)
                                                {
                                                    reader.WriteEntryToDirectory(base_path + "archive", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                                                }
                                            }
                                            reader.Dispose();
                                        }

                                        loadingbox.Value = 0;
                                        appstatus.Text = "Importing Meteor Skins";
                                        meteor_worker.RunWorkerAsync();
                                    }
                                    catch (Exception e3)
                                    {
                                        appstatus.Text = "Error";
                                        console_write("----------------------------------------------------------------------------------------------------");
                                        console_write("Archive error: \n An error has appened during the extraction of the archive");
                                        console_write("----------------------------------------------------------------------------------------------------");
                                    }
                                }
                                catch (Exception e2)
                                {
                                    appstatus.Text = "Error";
                                    console_write("----------------------------------------------------------------------------------------------------");
                                    console_write("Download error: \n The previous archive couldn't be deleted");
                                    console_write("----------------------------------------------------------------------------------------------------");
                                }
                            }
                        }
                        else
                        {
                            appstatus.Text = "Error";
                            console_write("----------------------------------------------------------------------------------------------------");
                            console_write("Download error: \n Either the ressource is missing or the download process failed.");
                            console_write("----------------------------------------------------------------------------------------------------");
                        }
                    });

                if (http_url != "")
                {
                    if (file_ext == "zip" | file_ext == "rar" | file_ext == "7z")
                    {
                        try
                        {
                            webClient.DownloadFileAsync(new Uri(http_url), file_path + "meteorpack." + file_ext);
                        }
                        catch (Exception e)
                        {
                            appstatus.Text = "Error";
                            console_write("----------------------------------------------------------------------------------------------------");
                            console_write("Download error: \n The meteor link is invalid");
                            console_write("----------------------------------------------------------------------------------------------------");
                        }
                    }
                    else
                    {
                        appstatus.Text = "Error";
                        console_write("----------------------------------------------------------------------------------------------------");
                        console_write("Download error: \n The archive is in an unsupported format");
                        console_write("----------------------------------------------------------------------------------------------------");
                    }
                }
            }



        }
        #endregion
        #region Batch
        //batch copy with drag&dropped folder and current slot
        private void batch_copy_model(String[] folderlist, Skin skin)
        {


            foreach (String folder in folderlist)
            {
                if (Library.check_character_foldername(skin.modelfolder.ToLower()))
                {
                    // Base folder level
                    //It means, folders are inside
                    if (Path.GetFileName(folder) == "model")
                    {
                        // Model folder level
                        //Getting directories inside
                        String[] model_folders = Directory.GetDirectories(folder);
                        //Checking folder presence
                        if (model_folders.Length > 0)
                        {
                            foreach (String folder2 in model_folders)
                            {
                                //body others level

                                String[] body_folders = Directory.GetDirectories(folder2);
                                foreach (String folder3 in body_folders)
                                {
                                    //cXX / lXX level
                                    if (Directory.GetFiles(folder3).Length > 0)
                                    {
                                        skin.add_model(folder3, folder2);
                                    }

                                }
                            }
                        }
                    }
                    //body others level
                    //moving a folder that's inside model
                    else
                    {
                        String[] body_folders = Directory.GetDirectories(folder);
                        if (Directory.GetFiles(folder).Length == 0)
                        {
                            foreach (String folder2 in body_folders)
                            {
                                //cXX / lXX level
                                if (Directory.GetFiles(folder2).Length > 0)
                                {
                                    skin.add_model(folder2, folder);
                                }

                            }
                        }
                        else
                        {
                            Regex clXX = new Regex("^[cl]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
                            if (clXX.IsMatch(Path.GetFileName(folder)))
                            {
                                skin.add_model(folder, "body");
                            }
                        }
                    }
                }

            }
            //skin_details_reload();


        }
        //batch copy with drag&dropped folder and current slot
        private void batch_copy_csp()
        {
            String path = csp_file_list[0];
            String filter = "*" + this.selected_skin.cspfolder + "_" + "*.nut";

            String[] files = Directory.GetFiles(path, filter, SearchOption.AllDirectories);

            foreach (String file in files)
            {
                console_write("File Detected :" + Path.GetFileName(file));
                this.selected_skin.add_csp(file);
            }
            console_write("All detected CSP were moved to slot " + this.selected_skin.slotstring);
            skin_details_reload();
        }
        //batch copy with specified source and destination slot
        private void batch_copy_csp(String[] filelist, int slot)
        {
            if (filelist.Length != 0)
            {

                String path = filelist[0];

                String filter = "*.nut";

                String[] files = Directory.GetFiles(path, filter, SearchOption.AllDirectories);

                foreach (String file in files)
                {
                    console_write("File Detected :" + Path.GetFileName(file));
                    selected_skin.add_csp(file);
                }
                console_write("All detected CSP were moved to slot " + selected_skin.slot);
            }
            else
            {

            }

            skin_details_reload();
        }
        //batch copy with specified source and destination slot
        private void batch_copy_csp(String[] filelist, Skin skin)
        {
            if (filelist.Length != 0)
            {

                String path = filelist[0];

                String filter = "*.nut";

                String[] files = Directory.GetFiles(path, filter, SearchOption.AllDirectories);

                foreach (String file in files)
                {
                    skin.add_csp(file);
                }
            }
            else
            {
            }

        }
        //batch copy with specified source and new next slot
        private void batch_add_slot(int slot)
        {
            Regex meteor = new Regex("(meteor_)(x{2})(_)(p*)");
            foreach (String file in this.slot_file_list)
            {
                if (meteor.IsMatch(Path.GetFileName(file)))
                {
                    skin_ListBox_reload();
                    console_write("Slot Detected : " + Path.GetFileName(file));
                    String skin_name = Path.GetFileName(file).Split('_')[2];
                    if (skin_name == "")
                    {
                        skin_name = "empty";
                    }

                    int skin_slot = SkinListBox.Items.Count + 1;

                    Skin meteor_skin = new Skin(Characterlist2.SelectedItems[0].Text, SkinListBox.Items.Count + 1, skin_name, "Custom");

                    //Model files check
                    if (Directory.Exists(file + "/model"))
                    {
                        console_write("Slot model folder detected");
                        batch_copy_model(Directory.GetDirectories(file + "/model"), meteor_skin);
                    }
                    else
                    {
                        console_write("Slot model folder missing");
                    }
                    //CSP Files check
                    if (Directory.Exists(file + "/csp/"))
                    {
                        console_write("Slot csp folder detected");
                        String[] folder = new string[] { file + "/csp/" };
                        batch_copy_csp(folder, meteor_skin);
                    }
                    else
                    {
                        console_write("Slot csp folder missing");
                    }
                    if (Directory.Exists(file + "/meta"))
                    {
                        console_write("meta folder detected");
                        meteor_skin.addMeta(file + "/meta/meta.xml");
                    }

                }
            }
            skin_ListBox_reload();
            SkinListBox.SelectedIndex = (SkinListBox.Items.Count - 1);
            uichar.setFile(int.Parse(Library.get_ui_char_db_id(Characterlist2.SelectedItems[0].Text)), 7, SkinListBox.Items.Count);
            skin_details_reload();
        }

        private void batch_add_slot(String path, ArrayList charlist)
        {
            try
            {
                Boolean test = false;
                foreach (String dir in Directory.GetDirectories(path))
                {

                    if (charlist.Contains(Path.GetFileName(dir)))
                    {
                        test = true;
                        //Get specified char and add a skin to it
                        Character selected_meteor_char = new Character(Path.GetFileName(dir));

                        Regex meteor = new Regex("(meteor_)(x{2})(_)(p*)");
                        float count = Directory.GetDirectories(dir).Length;
                        float current = 0;
                        foreach (String file in Directory.GetDirectories(dir))
                        {
                            if (meteor.IsMatch(Path.GetFileName(file)))
                            {
                                String skin_name = Path.GetFileName(file).Split('_')[2];
                                if (skin_name == "")
                                {
                                    skin_name = "empty";
                                }

                                int skin_slot = selected_meteor_char.skins.Count + 1;

                                Skin meteor_skin = new Skin(selected_meteor_char.fullname, skin_slot, skin_name, "Custom");

                                //Model files check
                                if (Directory.Exists(file + "/model"))
                                {
                                    batch_copy_model(Directory.GetDirectories(file + "/model"), meteor_skin);
                                }
                                else
                                {
                                }
                                float val = (1 / count) * current + (1 / count) * 1 / 3 * 100;
                                meteor_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                                //CSP Files check
                                if (Directory.Exists(file + "/csp/"))
                                {
                                    String[] folder = new string[] { file + "/csp/" };
                                    batch_copy_csp(folder, meteor_skin);
                                }
                                else
                                {
                                }
                                val = (1 / count) * current + (1 / count) * 2 / 3 * 100;
                                meteor_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                                if (Directory.Exists(file + "/meta"))
                                {
                                    meteor_skin.addMeta(file + "/meta/meta.xml");
                                }
                                selected_meteor_char.skins.Add(meteor_skin);
                                val = (1 / count) * current + (1 / count) * 100;
                                meteor_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                            }
                            current++;
                        }

                        last_char = Path.GetFileName(dir);
                        uichar.setFile(int.Parse(Library.get_ui_char_db_id(selected_meteor_char.fullname)), 7, selected_meteor_char.skins.Count);
                    }
                }
                if (test == true)
                {
                    meteorcode = 1;
                }
                else
                {
                    meteorcode = 2;
                }
            }
            catch (Exception e)
            {
                meteorcode = -1;
                Console.WriteLine(e.Message);
            }

        }
        //Used to import SmashExplorer mmsl_workspace into Library
        private String batch_import_SE()
        {
            String text = "";
            //Reseting all to avoid conflicts

            //Setting SE paths
            String se_mmsl_workspace_path = properties.get("explorer_workspace");
            String se_model_path = se_mmsl_workspace_path + "/content/patch/data/fighter/";
            String se_csp_path = se_mmsl_workspace_path + "/content/patch/data/ui/replace/chr/";
            String datafolder = properties.get("datafolder");
            String se_csp_path_dlc = se_mmsl_workspace_path + "/content/patch/" + datafolder + "/ui/replace/append/chr/";

            String slot_model = (SkinListBox.Items.Count + 1).ToString();



            //mmsl_workspace folder check
            if (Directory.Exists(se_mmsl_workspace_path))
            {
                #region ModelImporting
                //characters folder check
                if (Directory.Exists(se_model_path))
                {
                    //Character folders based on folders
                    String[] characters = Directory.GetDirectories(se_model_path);
                    float current = 1;
                    foreach (String character in characters)
                    {
                        float count = characters.Length;

                        //Checking said character has folders 
                        if (Directory.GetDirectories(character).Length != 0)
                        {
                            //Checking model folder
                            if (Directory.Exists(character + "/model"))
                            {

                                if (Library.check_character_foldername(Path.GetFileName(character)))
                                {
                                    text = "Detected character: " + Library.get_fullname_modelfolder(Path.GetFileName(character)) + "\n" + text;
                                    for (int i = 0; i < 256; i++)
                                    {
                                        String slot = i < 10 ? "0" + i : i.ToString();
                                        //Checking subfolders
                                        String[] Directories = Directory.GetDirectories(character + "/model", "*" + slot, SearchOption.AllDirectories);
                                        if (Directories.Length > 0)
                                        {
                                            text = "Detected model files \n" + text;

                                            foreach (String dir in Directories)
                                            {
                                                text = "Detected: " + Path.GetFileName(Directory.GetParent(dir).FullName) + "/" + Path.GetFileName(dir) + text;
                                                if (!Library.check_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), int.Parse(slot) + 1))
                                                {
                                                    Library.add_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), int.Parse(slot) + 1);
                                                }
                                                else
                                                {

                                                    if (Library.get_skin_origin(Library.get_fullname_modelfolder(Path.GetFileName(character)), int.Parse(slot) + 1) == "Default")
                                                    {
                                                        Library.set_origin(Library.get_fullname_modelfolder(Path.GetFileName(character)), int.Parse(slot) + 1, "Default Replaced");
                                                        Library.set_libraryname(Library.get_fullname_modelfolder(Path.GetFileName(character)), int.Parse(slot) + 1, "Default Replaced");
                                                    }
                                                }
                                                new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), i + 1, "Imported skin", "Sm4sh Explorer").add_model(dir, Directory.GetParent(dir).Name);
                                            }

                                        }
                                    }
                                }
                            }
                        }
                        float val = current / count * 50;
                        import_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                        current = current + 1;
                    }
                }else
                {
                    console_write("fighter folder not found in S4E's workspace");
                }
                #endregion
                #region CspImporting

                for (int z = 0; z < 2; z++)
                {
                    if (z == 1)
                    {
                        se_csp_path = se_csp_path_dlc;
                    }
                    if (Directory.Exists(se_csp_path))
                    {
                        //chr folders
                        float currentcount = 1;

                        String[] csps = Directory.GetDirectories(se_csp_path);
                        if (csps.Length > 0)
                        {
                            float count = csps.Length;

                            foreach (String cspformat in csps)
                            {
                                //check folder
                                if (Directory.GetFiles(cspformat).Length > 0)
                                {

                                    for (int i = 0; i < 256; i++)
                                    {

                                        foreach (String csp in Directory.GetFiles(cspformat))
                                        {
                                            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_([0-9]{2}|xx|[0-9]x|x[0-9]).nut$");
                                            if (cspr.IsMatch(Path.GetFileName(csp)))
                                            {
                                                //got every info for file
                                                String test = Path.GetFileName(csp);
                                                String slot = Path.GetFileName(csp).Split('_')[3].Split('.')[0];
                                                int teste;
                                                if (int.TryParse(slot, out teste))
                                                {
                                                    //Same slot
                                                    if (int.Parse(slot) == (i + 1))
                                                    {
                                                        //Gettin foldername
                                                        String foldername = Path.GetFileName(csp).Split('_')[2];
                                                        if (Library.check_fullname_cspname(foldername))
                                                        {

                                                            if (!Library.check_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)))
                                                            {
                                                                Library.add_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot));
                                                            }
                                                            else
                                                            {
                                                                if (Library.get_skin_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)) == "Default")
                                                                {
                                                                    Library.set_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot), "Default Replaced");
                                                                    Library.set_libraryname(Library.get_fullname_cspfolder(foldername), int.Parse(slot), "Default Replaced");
                                                                }
                                                            }
                                                            new Skin(Library.get_fullname_cspfolder(foldername), i + 1, "Imported skin", "Sm4sh Explorer").add_csp(csp);
                                                            text = "Detected: " + Path.GetFileName(csp) + "\n" + text;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }

                                float val = (z) * 25 + 50 + currentcount / count * 25;
                                import_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                                currentcount++;
                            }
                        }

                    }else
                    {
                        if(z == 0)
                        {
                            console_write("ui/replace/chr folder not found");
                        }
                        else
                        {
                            console_write("ui/replace/append/chr folder not found");
                        }
                        
                    }
                }

                #endregion
            }
            else
            {

            }
            return text;
        }
        //Used to export MMSL_workspace to Smash Explorer
        private void batch_export_SE()
        {
            if (Directory.Exists(Application.StartupPath + "/mmsl_workspace/data"))
            {
                String destination = properties.get("explorer_workspace") + "/content/patch/data";
                String source = Application.StartupPath + "/mmsl_workspace/data";
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                if (Directory.Exists(destination))
                {
                    try
                    {
                        //Deletes the previous fighter models
                        foreach (String c in Characters)
                        {
                            if (Directory.Exists(destination + "/fighter/" + Library.get_modelfolder_fullname(c) + "/model"))
                            {
                                Directory.Delete(destination + "/fighter/" + Library.get_modelfolder_fullname(c) + "/model", true);
                            }
                        }

                        try
                        {
                            if (Directory.Exists(destination + "/ui/replace/chr"))
                            {
                                Directory.Delete(destination + "/ui/replace/chr", true);
                            }


                            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                                Directory.CreateDirectory(dirPath.Replace(source, destination));

                            //Copy all the files & Replaces any files with the same name
                            float count = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Length;
                            float current = 1;
                            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                            {
                                File.Copy(newPath, newPath.Replace(source, destination), true);
                                float val = (current / count) / 4 * 100 * 3;
                                val = val > 100 ? 100 : val;
                                export_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                                current++;
                            }


                            if (properties.get("datafolder") != "data")
                            {
                                if (properties.get("unlocalised") == "1")
                                {
                                    source = Application.StartupPath + "/mmsl_workspace/data";
                                }
                                else
                                {
                                    source = Application.StartupPath + "/mmsl_workspace/" + properties.get("datafolder");
                                }


                                destination = properties.get("explorer_workspace") + "/content/patch/" + properties.get("datafolder");
                                if (Directory.Exists(destination + "/ui/replace/chr"))
                                {
                                    Directory.Delete(destination + "/ui/replace/chr", true);
                                }
                                if (Directory.Exists(destination + "/fighter"))
                                {
                                    Directory.Delete(destination + "/fighter", true);
                                }

                                foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                                {
                                    if (Path.GetFileName(dirPath) == "model")
                                    {
                                        Directory.Delete(dirPath);
                                    }
                                    Directory.CreateDirectory(dirPath.Replace(source, destination));
                                }

                                float count2 = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Length;

                                //Copy all the files & Replaces any files with the same name
                                foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                                {
                                    File.Copy(newPath, newPath.Replace(source, destination), true);
                                    float val = (current / count) / 4 * 100 + 74;
                                    val = val > 100 ? 100 : val;
                                    export_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                                    current++;
                                }

                            }
                            if (uichar.imported == true)
                            {
                                if (properties.get("datafolder") == "data")
                                {
                                    if (properties.get("unlocalised") == "0")
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/data(us_en)/param/ui/ui_character_db.bin";
                                    }
                                    else
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/data/param/ui/ui_character_db.bin";
                                    }

                                }
                                else
                                {
                                    if (properties.get("unlocalised") == "0")
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/" + properties.get("datafolder") + "/param/ui/ui_character_db.bin";
                                    }
                                    else
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/data/param/ui/ui_character_db.bin";
                                    }
                                }

                                if (properties.get("datafolder") == "data")
                                {
                                    if (properties.get("unlocalised") == "0")
                                    {
                                        destination = properties.get("explorer_workspace") + "/content/patch/data(us_en)";
                                    }
                                    else
                                    {
                                        destination = properties.get("explorer_workspace") + "/content/patch/data";
                                    }
                                }
                                else
                                {
                                    if (properties.get("unlocalised") == "0")
                                    {
                                        destination = properties.get("explorer_workspace") + "/content/patch/" + properties.get("datafolder");
                                    }
                                    else
                                    {
                                        destination = properties.get("explorer_workspace") + "/content/patch/data";
                                    }

                                }

                                File.Copy(source, destination + "/param/ui/ui_character_db.bin", true);
                                exportcode = 3;
                            }
                        }
                        catch
                        {
                            exportcode = 2;
                        }

                        
                    }
                    catch(Exception e)
                    {
                        exportcode = 1;
                    }
                    
                }
            }
        }
        //used to delete to empty and delete directory with all subs
        private void batch_delete(String foldername)
        {
            if (Directory.Exists(foldername))
            {
                String[] directories = Directory.GetDirectories(foldername, "*", SearchOption.AllDirectories);
                String[] files = Directory.GetFiles(foldername, "*", SearchOption.AllDirectories);
                if (files.Length != 0)
                {
                    foreach (String file in files)
                    {
                        File.Delete(file);
                    }
                }
                if (directories.Length != 0)
                {
                    foreach (String directory in directories)
                    {
                        batch_delete(directory);
                    }

                }

            }
        }

        #endregion
        #region Workers
        //Launches import
        private void import_worker_work(object sender, DoWorkEventArgs e)
        {
            e.Result = batch_import_SE();//return temp 
        }
        //Reports import progress
        private void import_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadingbox.Value = e.ProgressPercentage;
        }
        //Reports completion of import
        private void import_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            console_write((String)e.Result);//copies return value to public list we declared before
            loadingbox.Value = 100;
            importstatus();
            enable_controls();
            processing = false;
        }

        //Launches Export
        private void export_worker_work(object sender, DoWorkEventArgs e)
        {
            batch_export_SE();
        }
        //Reports export progress
        private void export_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadingbox.Value = e.ProgressPercentage;
        }
        //Reports completion of export
        private void export_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            exportstatus();
            loadingbox.Value = 100;
            enable_controls();
            processing = false;
            if (MessageBox.Show("Export Finished, do you want to launch Sm4sh Explorer?", " Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                launch_s4e();
            }
        }

        //Launches url mutex check
        private void url_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Mutex mmsl_url;
            int i = 0;
            int y = 1;
            while (i == 0)
            {
                if (Mutex.TryOpenExisting("mmsl_url", out mmsl_url))
                {
                    mmsl_url.Dispose();
                    url_worker.ReportProgress(y);
                    y = y == 99 ? 1 : y++;
                }
            }
        }
        //Reports url mutex check progress
        private void url_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (File.Exists(Application.StartupPath + "/mmsl_downloads/url.txt"))
            {
                string[] lines = System.IO.File.ReadAllLines(Application.StartupPath + "/mmsl_downloads/url.txt");
                if (!processing)
                {
                    processing = true;
                    meteor_download(lines);
                    block_controls();
                }
                File.Delete(Application.StartupPath + "/mmsl_downloads/url.txt");
            }
        }
        //Reports completion of url mutex check
        private void url_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        //Launches meteor import
        private void meteor_worker_DoWork(object sender, DoWorkEventArgs e)
        {

            batch_add_slot(Application.StartupPath + "/mmsl_downloads/archive", Characters);

        }
        //Reports meteor import progress
        private void meteor_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadingbox.Value = e.ProgressPercentage;
        }
        //Reports completion of meteor import
        private void meteor_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Reloads the UI
            skin_details_reload();
            skin_ListBox_reload();

            //Selects the last skin
            SkinListBox.SelectedIndex = SkinListBox.Items.Count - 1;

            //Deletes the archive file
            //Cleans the files in mmsl_downloads
            foreach (String file in Directory.GetFiles(Application.StartupPath + "/mmsl_downloads"))
            {
                File.Delete(file);
            }
            //End process actions
            loadingbox.Value = 100;
            enable_controls();
            processing = false;

            //Checks status about the import
            meteorstatus();

            //Selects the character the last skin was added for
            Characterlist2.FocusedItem = Characterlist2.FindItemWithText(last_char);
            Characterlist2.FindItemWithText(last_char).Selected = true;
            Characterlist2.Select();
            Characterlist2.FindItemWithText(last_char).EnsureVisible();
        }

        //Launches archiving
        private void archive_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Directory.GetDirectories(Application.StartupPath + "/mmsl_packages").Length > 0)
            {
                if (File.Exists(Application.StartupPath + "/mmsl_packages/Archive.zip"))
                {
                    File.Delete(Application.StartupPath + "/mmsl_packages/Archive.zip");
                }
                using (var archive = ZipArchive.Create())
                {
                    archive.AddAllFromDirectory(Application.StartupPath + "/mmsl_packages");
                    archive.SaveTo(File.OpenWrite(Application.StartupPath + "/mmsl_packages/Archive.zip"), CompressionType.None);
                    archive.Dispose();
                }

            }

        }
        //When archive is complete
        private void archive_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingbox.Style = ProgressBarStyle.Continuous;
            loadingbox.Value = 100;
            console_write("Archive Created in mmsl_packages");
            appstatus.Text = "Archive complete";
            processing = false;
            reset_skin_pack_session();
            enable_controls();

        }
        #endregion
        #endregion







    }


}
