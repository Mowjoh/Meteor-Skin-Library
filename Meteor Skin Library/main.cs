using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

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

        public main()
        {
            InitializeComponent();
            String startup_path = Application.StartupPath;
            //Checks Default_Library.xml presence
            if (!File.Exists(startup_path+"/mmsl_config/Default_Library.xml"))
            {
                console_write("Default Library not found, please add Default_Library.xml in the /mmsl_config folder.");
            }
            //Check Default_Config.xml presence
            if (!File.Exists(startup_path+"/mmsl_config/Default_Config.xml"))
            {
                console_write("Default Config not found, please add Default_Config.xml in the /mmsl_config folder.");
            }
            else
            {
                //Checks Config.xml presence, if not creates one based on Default_Config.xml
                if (!File.Exists(startup_path + "/mmsl_config/Config.xml"))
                {
                    console_write("Creating Config");
                    File.Copy(properties.get("default_config"), startup_path + "/mmsl_config/Config.xml");
                }
                properties.set_library_path(startup_path + "/mmsl_config /Config.xml");
                properties.add("current_library", startup_path + "/mmsl_config/Library.xml");
                console_write("Config loaded : mmsl_config/Config.xml");

                //Checks Library.xml presence, if not creates one based on Default_Library.xml
                if (!File.Exists(startup_path + "/mmsl_config/Library.xml"))
                {
                    console_write("Creating Library");
                    File.Copy(properties.get("default_library"), startup_path + "/mmsl_config/Library.xml");
                }
                Library = new LibraryHandler(properties.get("current_library"));
                console_write("Library loaded : mmsl_config/Library.xml");

                //Loads Character List
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
                region_select();
                uichar = new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));
                if (!uichar.imported)
                {
                    console_write("ui_character_db was not found in Sm4sh Explorer, please add it and relaunch this software!");
                }else
                {
                    console_write("ui_character_db was found, congrats !");
                }
                appstatus.Text = "Ready";
                loadingbox.Hide();
                processing = false;
            }

        }

        //Functions
        #region Menu
        #region FileMenu !-!
        //Menu Exit Function
        private void menu_software_exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //Open mmsl_workspace Function
        private void openmmsl_workspace(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "/mmsl_workspace");
        }
        #endregion
        #region SkinMenu !-!
        //When Add Skin is pressed
        private void skin_add(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (CharacterList.SelectedIndex != -1)
                {
                    selected_char.add_skin();
                    console_write("Skin added for " + selected_char.fullname + " in slot " + (SkinListBox.Items.Count + 1));
                    skin_ListBox_reload();
                    state_check();
                }
                else
                {
                    console_write("Please select a Character first");
                }
            }
            
            
        }
        
        #endregion
        #region OptionMenu !-!
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

                CharacterList.SelectedIndex = -1;
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


                    CharacterList.SelectedIndex = -1;
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
        #region SmashExplorerMenu !-!
        private void launch_se_import(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("It will erase all files of every mod you've added. The library containing skin information will be deleted. Continue with this Supermassive black-hole type destruction?", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    processing = true;
                    block_controls();
                    reset_all();
                    loadingbox.Show();
                    appstatus.Text = "Importing from Sm4sh Explorer";
                    backgroundWorker1.RunWorkerAsync();
                }
            }
            
                
        }
        private void launch_se_export(object sender, EventArgs e)
        {
            if (!processing)
            {
                if (MessageBox.Show("Doing this will erase fighter and ui folders from Smash Explorer's workspace. Are you sure you've made a backup? If yes, you can validate these changes", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    processing = true;
                    block_controls();
                    loadingbox.Show();
                    appstatus.Text = "Exporting to Sm4sh Explorer";
                    backgroundWorker2.RunWorkerAsync();
                }
            }
           
        }
        #endregion
        #endregion
        
        #region CharacterAction !-!
        //When a character is selected
        private void character_selected(object sender, EventArgs e)
        {
            selected_char = new Character(CharacterList.SelectedItem.ToString());
            skin_ListBox_reload();
            state_check();
        }

        #endregion
        #region SkinAction !-!
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
            this.selected_skin.set_library_name(SkinNameText.Text);
            skin_ListBox_reload();
            state_check();
        }

        //When Delete is pressed
        private void skin_delete(object sender, EventArgs e)
        {
            int index = SkinListBox.SelectedIndex + 1;
            int max = SkinListBox.Items.Count;

            if (selected_skin.origin == "Default" | selected_skin.origin == "Default Replaced")
            {
                console_write("Tou cannot delete Default slots");
            }
            else
            {
                this.selected_skin.delete_skin();
                selected_char.getSkins();
                skin_ListBox_reload();
                skin_details_reload();
                console_write("Deleted slot " + index);
            }

            state_check();

            uichar.setFile(int.Parse(Library.get_ui_char_db_id(CharacterList.SelectedItem.ToString())), 7, SkinListBox.Items.Count);

        }

        //When Clean Files is pressed
        private void clean_files_clicked(object sender, EventArgs e)
        {
            this.selected_skin.clean_skin();
            
            skin_details_reload();
            skin_ListBox_reload();
            state_check();

        }
        
        //packages skin into meteor skin
        private void package_meteor(object sender, EventArgs e)
        {
            this.selected_skin.package_meteor();
        }
        #endregion
        #region ModelAction !-!
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
            skin_ListBox_reload();
            state_check();
        }
        #endregion
        #region CspAction !-!
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
        #region MetaDataAction !-!
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

        #region Interface
        #region Reloads !-!
        //Reloads Skin Details 
        private void skin_details_reload()
        {
           
            int slot = SkinListBox.SelectedIndex;
            //emptying lists
            if(slot != -1)
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
                
            if(CharacterList.SelectedIndex != -1)
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
            if(SkinListBox.SelectedIndex != -1)
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
            batch_copy_model(this.model_folder_list,this.selected_skin);
            state_check();
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
                batch_copy_csp(FileList,SkinListBox.SelectedIndex);
                state_check();
            }
            else
            {
                //textBox6.Text = "Item wasn't a Directory";
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
        }
        #endregion
        #region Inits !-!
        //Filling Character list
        public void init_character_ListBox()
        {
            CharacterList.Items.Clear();
            foreach (String chars in Characters)
            {
                CharacterList.Items.Add(chars);
            }

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
        #region Console !-!
        //Writes string to console
        private void console_write(String s)
        {
            textConsole.Text = s + "\n" + textConsole.Text;
        }
        #endregion
        #region State !-!
        //State Checker
        private void state_check()
        {
            int character = CharacterList.SelectedIndex;
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

                //State
            }
            else
            {
                //Interactions
                meteorbox.AllowDrop = true;
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
            CharacterList.Enabled = false;
            meteorbox.Enabled = false;
        }
        private void enable_controls()
        {
            SkinListBox.Enabled = true;
            CharacterList.Enabled = true;
            meteorbox.Enabled = true;
        }
        #endregion

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
                            Regex clXX = new Regex("^[cl]([0-9]{2}|xx)$", RegexOptions.IgnoreCase);
                            if (clXX.IsMatch(Path.GetFileName(folder)))
                            {
                                skin.add_model(folder, "body");
                            }
                        }
                    }
                }

            }
            skin_details_reload();


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
                console_write("no csp detected ");
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
                    console_write("File Detected :" + Path.GetFileName(file));
                    skin.add_csp(file);
                }
                console_write("All detected CSP were moved to slot " + skin.slot);
            }
            else
            {
                console_write("no csp detected ");
            }

            skin_details_reload();
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

                    Skin meteor_skin = new Skin(CharacterList.SelectedItem.ToString(), SkinListBox.Items.Count + 1, skin_name, "Custom");

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
            uichar.setFile(int.Parse(Library.get_ui_char_db_id(CharacterList.SelectedItem.ToString())), 7, SkinListBox.Items.Count);
            skin_details_reload();
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
                    foreach (String character in characters)
                    {
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
                                                }else
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
                    }
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

                        String[] csps = Directory.GetDirectories(se_csp_path);
                        if (csps.Length > 0)
                        {
                            foreach (String cspformat in csps)
                            {
                                //check folder
                                if (Directory.GetFiles(cspformat).Length > 0)
                                {
                                    //For all slot values
                                    for (int i = 0; i < 256; i++)
                                    {
                                        foreach (String csp in Directory.GetFiles(cspformat))
                                        {
                                            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_[0-9][0-9].nut$");
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
                                                            }else
                                                            {
                                                                if(Library.get_skin_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)) == "Default")
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
                            }
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
                    if (Directory.Exists(destination + "/fighter"))
                    {
                        Directory.Delete(destination + "/fighter", true);

                    }
                    if (Directory.Exists(destination + "/ui/replace/chr"))
                    {
                        Directory.Delete(destination + "/ui/replace/chr", true);
                    }


                    foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(source, destination));

                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                        File.Copy(newPath, newPath.Replace(source, destination), true);

                    if (properties.get("datafolder") != "data")
                    {
                        if(properties.get("unlocalised") == "1")
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
                            if(Path.GetFileName(dirPath) == "model")
                            {
                                Directory.Delete(dirPath);
                            }
                            Directory.CreateDirectory(dirPath.Replace(source, destination));
                        }
                           
                        

                        //Copy all the files & Replaces any files with the same name
                        foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                            File.Copy(newPath, newPath.Replace(source, destination), true);
                    }
                    if (uichar.imported == true)
                    {
                        if (properties.get("datafolder") == "data")
                        {
                            if(properties.get("unlocalised") == "0")
                            {
                                source = Application.StartupPath + "/mmsl_workspace/data(us_en)/param/ui/ui_character_db.bin";
                            }else
                            {
                                source = Application.StartupPath + "/mmsl_workspace/data/param/ui/ui_character_db.bin";
                            }
                            
                        }
                        else
                        {
                            if (properties.get("unlocalised") == "0")
                            {
                                source = Application.StartupPath + "/mmsl_workspace/" + properties.get("datafolder") + "/param/ui/ui_character_db.bin";
                            }else
                            {
                                source = Application.StartupPath + "/mmsl_workspace/data/param/ui/ui_character_db.bin";
                            }
                        }

                        if (properties.get("datafolder") == "data")
                        {
                            if (properties.get("unlocalised") == "0")
                            {
                                destination = properties.get("explorer_workspace") + "/content/patch/data(us_en)";
                            }else
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
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = batch_import_SE();//return temp 
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            console_write((String)e.Result);//copies return value to public list we declared before
            loadingbox.Hide();
            appstatus.Text = "Import Completed";
            enable_controls();
            processing = false;
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            appstatus.Text = "Export Finished";
            loadingbox.Hide();
            enable_controls();
            processing = false;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {

            batch_export_SE();
        }
        #endregion

        private void move_up_skin(object sender, EventArgs e)
        {
            if(selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndex, SkinListBox.SelectedIndex - 1);
                skin_ListBox_reload();
                SkinListBox.SelectedIndex = selected_skin.modelslot;
            }
            
        }

        private void move_down_skin(object sender, EventArgs e)
        {
            if (selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndex, SkinListBox.SelectedIndex + 1);
                skin_ListBox_reload();
                SkinListBox.SelectedIndex = selected_skin.modelslot;
            }
            
        }

    }


}
