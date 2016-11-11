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
        Logger logg;
        #endregion
        #region SelectedInfo


        //Variables redone
        Skin selected_skin;
        Character selected_char;
        String last_char = "";
        String workspace_char = "";
        String[] manualfolder;
        ArrayList manual_meteors;
        Boolean started_pack = false;
        String selected_cell_charname="";
        int selected_cell_row;
        int selected_cell_column;
        String selected_cell_skin_name = "";

        #endregion
        #region Lists
        //Lists for soft
        ArrayList Characters = new ArrayList();
        ArrayList Skins = new ArrayList();

        ArrayList ui_char_db_values = new ArrayList();

        ImageList status_images;
        #endregion
        #region Files
        //Selected Files
        String[] model_folder_list;
        String[] csp_file_list;
        String[] slot_file_list;
        #endregion
        #region Processing
        Boolean processing;
        int current_step = 0;
        int steps = 0;
        int process= 0;
        int status = 0;
        double val;
        String process_text;
        int workspace_select = 0;
        #endregion
        #region Errorcodes
        int downloadcode = 0;
        int extractcode = 0;
        int archivecode = 0;
        int meteorcode = 0;
        int importcode = 0;
        int exportcode = 0;

        #endregion
        #region Appvalues
        String appversion="";
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

            //Version
            XmlDocument xml2 = new XmlDocument();
            xml2.Load(Application.StartupPath + "/newcorepackage.xml");
            XmlNode nodes2 = xml2.SelectSingleNode("package");
            this.appversion = nodes2.Attributes[0].Value;
            

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
                manual_meteors = new ArrayList();
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
                appstatus.Text = "Ready";
                processing = false;
                url_worker.RunWorkerAsync();
                reset_skin_pack_session();
                this.status_images = new ImageList();
                status_images.ImageSize = new Size(24, 24);
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/unknown.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/missing.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/new.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/checked.png"));
                SkinListBox.SmallImageList = status_images;

                if(properties.get("dev") == "1")
                {
                    resetAllToolStripMenuItem.Visible = true;
                    resetLibraryToolStripMenuItem.Visible = true;
                    resetConfigToolStripMenuItem.Visible = true;
                    resetWorkspaceToolStripMenuItem.Visible = true;
                }
                #endregion
                if (properties.get("logging") == "1")
                {
                    logg = new Logger(1,true);
                }else
                {
                    logg = new Logger(1,false);
                }

                //Launches config if not edited
                region_select();

                #region ui_char_db 
                uichar = new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));
                if (!uichar.imported)
                {
                    logg.log("ui_char_db not imported");
                    console_write("ui_character_db was not found in Sm4sh Explorer, please add it and relaunch this software!");
                }
                else
                {
                    logg.log("ui_char_db imported");
                    console_write("ui_character_db was found, congrats !");
                }
                #endregion

                #region melee.msbt
                /*
                Melee melee = new Melee();
                if (melee.check_file())
                {
                    console_write("melee.msbt file found, congrats!");
                }else
                {
                    console_write("melee.msbt was not found in S4E's workspace or extract folder");
                }
               */
                #endregion

                //If arguments are passed
                if (args.Length > 0 | (fakeargs == true))
                {
                    logg.log("args were passed");
                    //Launch download process
                    processing = true;
                    block_controls();
                    meteor_download(args);
                }
                else
                {
                    logg.log("no args passed");
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
            logg.log("trying to launch S4E at "+ path);
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
                    logg.log("Skin added for " + selected_char.fullname + " in slot " + (SkinListBox.Items.Count + 1));
                    console_write("Skin added for " + selected_char.fullname + " in slot " + (SkinListBox.Items.Count + 1));
                    skin_ListBox_reload();
                    state_check();
                    //Selects the last skin
                    SkinListBox.FocusedItem = SkinListBox.Items[SkinListBox.Items.Count - 1];
                    SkinListBox.Items[SkinListBox.Items.Count - 1].Selected = true;
                    SkinListBox.Select();
                    SkinListBox.Items[SkinListBox.Items.Count - 1].EnsureVisible();
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
            if(File.Exists(Application.StartupPath + "/mmsl_packages/meta.xml"))
            {
                File.Delete(Application.StartupPath + "/mmsl_packages/meta.xml");
            }
            console_write("Meteor skin pack session reset");
            listView1.Enabled = true;
            listView1.Items.Clear();
            meteorpack_gridview.Rows.Clear();
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


                    Characters = Library.get_character_list();
                    init_character_ListBox();
                    state_check();
                }
            }
        }
        //Resets library and workspace
        private void import_reset()
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
            //Deleting old meta
            String[] metas = Directory.GetDirectories(Application.StartupPath + "/mmsl_config/meta");
            if(metas.Length > 0)
            {
                foreach(String meta in metas)
                {
                    Directory.Delete(meta, true);
                }
            }

            File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
            File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");
            new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));

        }
        #endregion
        #region SmashExplorerMenu 
        //Launches "Replace Workspace"
        private void launch_se_import(object sender, EventArgs e)
        {
            {
                if (MessageBox.Show("This will import all the skins from S4E. Doing this will erase your actual workspace, and library.", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    //Processing 
                    processing = true;
                    block_controls();
                    loadingbox.Value = 0;
                    loadingbox.Style = ProgressBarStyle.Continuous;
                    appstatus.Text = "Importing from Sm4sh Explorer";
                    import_reset();
                    workspace_select = 0;
                    if (Characterlist2.SelectedIndices.Count > 0)
                    {
                        last_char = Characterlist2.SelectedItems[0].Text;
                    }
                    import_worker.RunWorkerAsync();
                }
            }


        }
        private void importMissingFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.workspace_select = 1;
            //Processing 
            processing = true;
            block_controls();
            loadingbox.Value = 0;
            loadingbox.Style = ProgressBarStyle.Continuous;
            appstatus.Text = "Importing missing files from S4E";
            if (Characterlist2.SelectedIndices.Count > 0)
            {
                workspace_char = Characterlist2.SelectedItems[0].Text;
            }
            import_worker.RunWorkerAsync();
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
                    if (Characterlist2.SelectedIndices.Count > 0)
                    {
                        workspace_char = Characterlist2.SelectedItems[0].Text;
                    }
                    export_worker.RunWorkerAsync();
                }
            }

        }
        #endregion
        #region HelpMenu
        private void about(object sender, EventArgs e)
        {
            if (MessageBox.Show("Segtendo Build: " + appversion + "\n\nCreator : Mowjoh \n\nHey, you ! Thanks for using Meteor Skin Library !\nYou da real MVP!", "Meteor Skin Library Beta", MessageBoxButtons.OK) == DialogResult.OK)
            {

            }
        }
        #endregion
        #region WorkspaceMenu
        private void refreshMSLsWorkspaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.workspace_select = 2;
            //Processing 
            processing = true;
            block_controls();
            loadingbox.Value = 0;
            loadingbox.Style = ProgressBarStyle.Continuous;
            appstatus.Text = "Importing missing files in workspace";
            if (Characterlist2.SelectedIndices.Count > 0)
            {
                workspace_char = Characterlist2.SelectedItems[0].Text;
            }
            import_worker.RunWorkerAsync();
        }
        private void checkMissingFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Processing 
            processing = true;
            block_controls();
            loadingbox.Value = 0;
            loadingbox.Style = ProgressBarStyle.Continuous;
            appstatus.Text = "Refreshing file list";
            if (Characterlist2.SelectedIndices.Count > 0)
            {
                workspace_char = Characterlist2.SelectedItems[0].Text;
            }
            refresh_worker.RunWorkerAsync();
        }
        #endregion
        #endregion

        //Contextual menus
        #region Contextual Menus
        private void model_click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (models_ListView.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    model_menu_strip.Show(Cursor.Position);
                }
            }
        }

        private void model_context(object sender, EventArgs e)
        {
            String path = selected_skin.get_model_path(models_ListView.SelectedItems[0].Text) + "/";
            System.Diagnostics.Process.Start(path);
        }

        private void csps_click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (csps_ListView.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    csps_menu_strip.Show(Cursor.Position);
                }
            }
        }

        private void csps_context(object sender, EventArgs e)
        {
            String path = Path.GetDirectoryName(selected_skin.get_csp_path(csps_ListView.SelectedItems[0].Text)) + "/";
            System.Diagnostics.Process.Start(path);
        }

        #endregion

        //Main Control Area
        #region Character Tab
        //When a character is selected
        private void character_selected(object sender, EventArgs e)
        {
            selected_char = new Character(Characterlist2.SelectedItems[0].Text,Library,properties,uichar,logg);
            skin_ListBox_reload();
            state_check();
        }

        //When a character is selected NEW
        private void Characterlist2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Characterlist2.SelectedItems.Count > 0)
            {
                selected_char = new Character(Characterlist2.SelectedItems[0].Text, Library, properties, uichar, logg);
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

            int index = SkinListBox.SelectedIndices[0];
            this.selected_skin.set_library_name(SkinNameText.Text);
            skin_ListBox_reload();
            state_check();
            //Selects the last skin
            SkinListBox.FocusedItem = SkinListBox.Items[index];
            SkinListBox.Items[index].Selected = true;
            SkinListBox.Select();
            SkinListBox.Items[index].EnsureVisible();

        }

        //Skin Info Saved by enter key press
        private void SkinNameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int index = SkinListBox.SelectedIndices[0];
                this.selected_skin.set_library_name(SkinNameText.Text);
                skin_ListBox_reload();
                state_check();
                SkinListBox.FocusedItem = SkinListBox.Items[index];
                SkinListBox.Items[index].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[index].EnsureVisible();
            }
        }

        //When Delete is pressed
        private void skin_delete(object sender, EventArgs e)
        {
            int index = SkinListBox.SelectedIndices[0] + 1;
            int saved_index = SkinListBox.SelectedIndices[0];
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
                //Selects the last skin
                SkinListBox.FocusedItem = SkinListBox.Items[saved_index - 1];
                SkinListBox.Items[saved_index - 1].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[saved_index - 1].EnsureVisible();
            }
            else
            {
                //Selects the last skin
                SkinListBox.FocusedItem = SkinListBox.Items[SkinListBox.Items.Count - 1];
                SkinListBox.Items[SkinListBox.Items.Count - 1].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[SkinListBox.Items.Count - 1].EnsureVisible();
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
            pack_add_item(selected_skin.get_meteor_info());
            console_write("This skin was added to the current pack session");
            started_pack = true;
        }

        //When the move up button is pressed
        private void move_up_skin(object sender, EventArgs e)
        {
            if (selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndices[0], SkinListBox.SelectedIndices[0] - 1);
                skin_ListBox_reload();
                SkinListBox.FocusedItem = SkinListBox.Items[selected_skin.modelslot];
                SkinListBox.Items[selected_skin.modelslot].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[selected_skin.modelslot].EnsureVisible();
            }

        }

        //When the move down button is pressed
        private void move_down_skin(object sender, EventArgs e)
        {
            if (selected_skin.origin != "Default")
            {
                selected_char.swap_skin(SkinListBox.SelectedIndices[0], SkinListBox.SelectedIndices[0] + 1);
                skin_ListBox_reload();
                SkinListBox.FocusedItem = SkinListBox.Items[selected_skin.modelslot];
                SkinListBox.Items[selected_skin.modelslot].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[selected_skin.modelslot].EnsureVisible();
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

        //Skin Packing
        #region Skin Packing
        public void pack_add_item(String[] values)
        {
            meteorpack_gridview.Rows.Add(values);
        }

        private void manual_drop(object sender, DragEventArgs e)
        {
            this.manualfolder = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            listView1.Enabled = false;
            listView1.Items.Add("Manual folder detected / Skins-> Reset Package folder to restart");
            
            manual_worker.RunWorkerAsync();
        }

        private void manual_dragenter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void meteorpack_gridview_CurrentCellChanged(object sender, EventArgs e)
        {
            if (started_pack)
            {
                if (meteorpack_gridview.SelectedCells.Count > 0)
                {
                    if (meteorpack_gridview.SelectedCells[0].ColumnIndex == 1)
                    {
                        selected_cell_charname = meteorpack_gridview.SelectedCells[0].Value.ToString();
                        selected_cell_row = meteorpack_gridview.SelectedCells[0].RowIndex;
                        selected_cell_column = 1;
                    }
                    if(meteorpack_gridview.SelectedCells[0].ColumnIndex == 2)
                    {
                        selected_cell_row = meteorpack_gridview.SelectedCells[0].RowIndex;
                        selected_cell_skin_name = meteorpack_gridview.Rows[selected_cell_row].Cells[2].Value.ToString();
                        selected_cell_column = 2;
                    }
                }
            }
        }

        private void meteorpack_gridview_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (started_pack)
            {
                if(selected_cell_column == 1)
                {
                    String packname = meteorpack_gridview.Rows[selected_cell_row].Cells[2].Value.ToString();

                    String source = Application.StartupPath + "/mmsl_packages/" + selected_cell_charname + "";
                    String destination = Application.StartupPath + "/mmsl_packages/" + meteorpack_gridview.SelectedCells[0].Value.ToString() + "/";


                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(source + "/meteor_xx_" + packname, "*.*", SearchOption.AllDirectories))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(newPath.Replace(source + "/meteor_xx_" + packname, destination + "/meteor_xx_" + packname))))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath.Replace(source + "/meteor_xx_" + packname, destination + "/meteor_xx_" + packname)));
                        }
                        File.Copy(newPath, newPath.Replace(source + "/meteor_xx_" + packname, destination + "/meteor_xx_" + packname), true);
                    }
                    Directory.Delete(source + "/meteor_xx_" + packname, true);
                    selected_cell_charname = meteorpack_gridview.SelectedCells[0].Value.ToString();
                }

                if(selected_cell_column == 2)
                {
                    String packname = meteorpack_gridview.Rows[selected_cell_row].Cells[2].Value.ToString();

                    String source = Application.StartupPath + "/mmsl_packages/" + meteorpack_gridview.Rows[selected_cell_row].Cells[1].Value.ToString() + "/";
                    String destination = Application.StartupPath + "/mmsl_packages/" + meteorpack_gridview.Rows[selected_cell_row].Cells[1].Value.ToString() + "/";


                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(source + "/meteor_xx_" + selected_cell_skin_name, "*.*", SearchOption.AllDirectories))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(newPath.Replace(source + "/meteor_xx_" + selected_cell_skin_name, destination + "/meteor_xx_" + packname))))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath.Replace(source + "/meteor_xx_" + selected_cell_skin_name, destination + "/meteor_xx_" + packname)));
                        }
                        File.Copy(newPath, newPath.Replace(source + "/meteor_xx_" + selected_cell_skin_name, destination + "/meteor_xx_" + packname), true);
                    }
                    Directory.Delete(source + "/meteor_xx_" + selected_cell_skin_name, true);
                    selected_cell_charname = meteorpack_gridview.SelectedCells[0].Value.ToString();
                }
                
            }

        }

        private void meteor_pack(object sender, EventArgs e)
        {
            if (!processing)
            {
                String author_name = textBox7.Text;
                String pack_version = textBox8.Text;
                File.Copy(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml", Application.StartupPath + "/mmsl_packages/meta.xml", true);

                //Creating XML files
                foreach (DataGridViewRow row in meteorpack_gridview.Rows)
                {
                    String character = row.Cells[1].Value.ToString();
                    String meteorname = row.Cells[2].Value.ToString();
                    String folder = Application.StartupPath + "/mmsl_packages/" + character + "/meteor_xx_" + meteorname;

                    XmlDocument xml = new XmlDocument();
                    xml.Load(Application.StartupPath + "/mmsl_packages/meta.xml");
                    XmlNodeList data = xml.SelectNodes("metadata/meta");
                    foreach (XmlElement xe in data)
                    {
                        if (xe.Attributes["name"].Value.ToString() == "author")
                        {
                            xe.InnerText = author_name;
                        }
                        if (xe.Attributes["name"].Value.ToString() == "version")
                        {
                            xe.InnerText = pack_version;
                        }
                        if (xe.Attributes["name"].Value.ToString() == "name")
                        {
                            xe.InnerText = meteorname;
                        }
                        if (xe.Attributes["name"].Value.ToString() == "texidfix")
                        {
                            xe.InnerText = "";
                        }
                    }
                    if (!Directory.Exists(folder + "/meta"))
                    {
                        Directory.CreateDirectory(folder + "/meta");
                    }
                    xml.Save(folder + "/meta/meta.xml");
                }

                //Deleting empty folders
                foreach (String char_dirs in Directory.GetDirectories(Application.StartupPath + "/mmsl_packages/"))
                {
                    if (Directory.GetFiles(char_dirs, "*", SearchOption.AllDirectories).Length == 0)
                    {
                        Directory.Delete(char_dirs);
                    }
                }

                //Copying manual install folder
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_packages/Manual Installation folder");

                //Copy all the files & Replaces any files with the same name
                if (manual_meteors.Count > 0)
                {
                    foreach (string newPath in Directory.GetFiles(manualfolder[0], "*.*", SearchOption.AllDirectories))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(newPath.Replace(manualfolder[0], Application.StartupPath + "/mmsl_packages/Manual Installation folder"))))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath.Replace(manualfolder[0], Application.StartupPath + "/mmsl_packages/Manual Installation folder")));
                        }
                        File.Copy(newPath, newPath.Replace(manualfolder[0], Application.StartupPath + "/mmsl_packages/Manual Installation folder"), true);
                    }
                }

                File.Delete(Application.StartupPath + "/mmsl_packages/meta.xml");


                archive_worker.RunWorkerAsync();
                loadingbox.Style = ProgressBarStyle.Marquee;
                appstatus.Text = "Archiving files...";
                processing = true;
                block_controls();
            }

        }

        #endregion

        //Interface functions
        #region Interface

        #region Reloads 
        //Reloads Skin Details 
        private void skin_details_reload()
        {

            int slot = SkinListBox.SelectedIndices.Count > 0 ? SkinListBox.SelectedIndices[0] : -1 ;
            logg.log("-- attempting to reload skin details for slot "+slot);
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


                logg.log("emptied lists, looking for csp");
                //checking csps
                if (this.selected_skin.csps.Count > 0)
                {
                    //adding csps
                    foreach (String csp in this.selected_skin.csps)
                    {
                        
                        csps_ListView.Items.Add(csp);
                        ListViewItem lvi = new ListViewItem(csp);
                        
                    }
                }
                logg.log("looking for modsl");
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
                    logg.log("Setting delete option to enabled");
                    button3.Enabled = true;
                }
                else
                {
                    logg.log("Setting delete option to disabled");
                    
                    button3.Enabled = false;
                }

                button2.Enabled = true;

                logg.log("Setting skin to imported");
                this.selected_skin.set_imported();
                if (this.selected_skin.missing)
                {
                    logg.log("skin has missing files");
                    SkinListBox.SelectedItems[0].ImageIndex = 1;
                }
                else
                {
                    logg.log("skin has no missing files");
                    SkinListBox.SelectedItems[0].ImageIndex = 3;
                    SkinListBox.SelectedItems[0].ForeColor = Color.Black;
                }
                
            }



        }
        //Reloads Skin List
        private void skin_ListBox_reload()
        {

            if (Characterlist2.SelectedIndices.Count > 0)
            {
                logg.log("-- Attempting listbox reload");
                SkinListBox.Items.Clear();
                selected_char.getSkins();
                

                foreach (Skin skin in selected_char.skins)
                {
                    logg.log("cycling through skins");
                    ListViewItem item = new ListViewItem("Slot " + skin.slotstring + " - " + skin.libraryname);

                    if (skin.unknown)
                    {
                        logg.log("unknown skin found at slot "+ skin.slotstring);
                        item.ImageIndex = 0;
                        item.ForeColor = Color.Purple;
                    }
                    else
                    {
                        if (skin.missing)
                        {
                            logg.log("missing files for skin found at slot " + skin.slotstring);
                            item.ImageIndex = 1;
                            item.ForeColor = Color.DarkRed;
                        }
                        else
                        {
                            if (skin.new_files)
                            {
                                logg.log("new files for skin found at slot " + skin.slotstring);
                                item.ImageIndex = 2;
                            }
                            else
                            {
                                logg.log("imported skin found at slot " + skin.slotstring);
                                item.ImageIndex = 3;
                            }
                           
                        }
                    }
                    SkinListBox.Items.Add(item);
                }
            }
        }
        //Reloads MetaData
        private void metadata_reload()
        {
            int slot = SkinListBox.SelectedIndices.Count > 0 ? SkinListBox.SelectedIndices[0] : -1;
            if (slot != -1)
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
            logg.log("-- dropped a model folder");
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
                logg.log("-- dropped a csp dir");
                this.csp_file_list = FileList;
                batch_copy_csp(FileList, SkinListBox.SelectedIndices[0]);
                skin_details_reload();
                state_check();
            }
            else
            {
                //textBox6.Text = "Item wasn't a Directory";
                if (FileList.Length > 0)
                {
                    logg.log("-- dropped a csp file");
                    foreach (String file in FileList)
                    {
                        selected_skin.add_csp(file);
                        logg.log("detected a csp file "+ file);
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
            logg.log("-- dropped something in the meteor zone");
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
                Column4.Items.Add(chars);
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
            int skin = 0;
            if (SkinListBox.SelectedIndices.Count > 0)
            {
                skin = SkinListBox.SelectedIndices[0];
            }else
            {
                skin = -1;
            }
            
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
                button6.Enabled = true;
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
        private void processing_state(int mode)
        {

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
                update_updater();
            }
            else
            {
                //same major version
                if (major == major2)
                {
                    //update
                    if (minor > minor2)
                    {
                        update_updater();
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
                    process_status.Text = "Success";
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
                    process_status.Text = "Success";
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
                    importcode = 0;
                    break;
                case 2:
                    appstatus.Text = "Error";
                    console_write("----------------------------------------------------------------------------------------------------");
                    console_write("Directory error: \n Could not delete the data/ui/replace/chr folder. Please remove it manually and retry.");
                    console_write("----------------------------------------------------------------------------------------------------");
                    importcode = 0;
                    break;
                case 3:
                    appstatus.Text = "Success";
                    process_status.Text = "Operation complete";
                    console_write("----------------------------------------------------------------------------------------------------");
                    switch (workspace_select)
                    {
                        case 0:
                            console_write("MSL's library and workspace were successfully replaced.");
                            break;
                        case 1:
                            console_write("MSL's library was refreshed and updated with missing files found in S4E.");
                            break;
                        case 2:
                            console_write("MSL's library was refreshed and updated with missing files found in it's workspace.");
                            break;
                    }
                    
                    console_write("----------------------------------------------------------------------------------------------------");
                    importcode = 0;
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
            String http_url = "";
            String file_ext = "";

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
                                        processbox.Value = 0;
                                        appstatus.Text = "Importing Meteor Skins";
                                        process_status.Text = "Importing Meteor Skins";
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

                    Skin meteor_skin = new Skin(Characterlist2.SelectedItems[0].Text, SkinListBox.Items.Count + 1, skin_name, "Custom", Library, properties, logg);

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
            SkinListBox.FocusedItem = SkinListBox.Items[SkinListBox.Items.Count - 1];
            SkinListBox.Items[SkinListBox.Items.Count - 1].Selected = true;
            SkinListBox.Select();
            SkinListBox.Items[SkinListBox.Items.Count - 1].EnsureVisible();
            uichar.setFile(int.Parse(Library.get_ui_char_db_id(Characterlist2.SelectedItems[0].Text)), 7, SkinListBox.Items.Count);
            skin_details_reload();
        }
        private void batch_add_slot(String path, ArrayList charlist)
        {
            try
            {
                Boolean test = false;
                this.current_step = 0;
                this.steps = Directory.GetDirectories(path).Length;
                foreach (String dir in Directory.GetDirectories(path))
                {

                    if (charlist.Contains(Path.GetFileName(dir)))
                    {
                        test = true;
                        //Get specified char and add a skin to it
                        Character selected_meteor_char = new Character(Path.GetFileName(dir), Library, properties, uichar, logg);

                        Regex meteor = new Regex("(meteor_)(x{2})(_)(p*)");
                        double count = Directory.GetDirectories(dir).Length;
                        double current = 1;
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

                                Skin meteor_skin = new Skin(selected_meteor_char.fullname, skin_slot, skin_name, "Custom", Library, properties, logg);

                                //Model files check
                                if (Directory.Exists(file + "/model"))
                                {
                                    batch_copy_model(Directory.GetDirectories(file + "/model"), meteor_skin);
                                }
                                else
                                {
                                }
                                
                                //CSP Files check
                                if (Directory.Exists(file + "/csp/"))
                                {
                                    String[] folder = new string[] { file + "/csp/" };
                                    batch_copy_csp(folder, meteor_skin);
                                }
                                else
                                {
                                }
                                if (Directory.Exists(file + "/meta"))
                                {
                                    meteor_skin.addMeta(file + "/meta/meta.xml");
                                }
                                selected_meteor_char.skins.Add(meteor_skin);
                            }

                            double val = current / count * 100;

                            double val2 = process / steps + (100 / steps) * current_step;
                            if (val2 > 100)
                            {
                                val2 = 100;
                            }

                            this.process = Convert.ToInt32(Math.Truncate(val));
                            this.status = Convert.ToInt32(val2);
                            this.process_text = "Adding Meteor Skins to " + Path.GetFileName(dir);
                            meteor_worker.ReportProgress(process);
                            current++;
                        }
                        last_char = Path.GetFileName(dir);
                        uichar.setFile(int.Parse(Library.get_ui_char_db_id(selected_meteor_char.fullname)), 7, selected_meteor_char.skins.Count);

                        
                    }
                    current_step++;
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
                                                new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), i + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_model(dir, Directory.GetParent(dir).Name);
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
            this.steps = 2;
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
                        process_text = "Removing S4E's skin files";
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
                            current_step = 0;
                            process_text = "Copying Data folder";
                            //Copy all the files & Replaces any files with the same name
                            float count = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Length;
                            float current = 1;
                            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                            {
                                File.Copy(newPath, newPath.Replace(source, destination), true);
                                float val = (current / count)*100;
                                process = Convert.ToInt32(Math.Truncate(val));
                                double val2 = process / steps + (100 / steps) * current_step;
                                status = Convert.ToInt32(val2 > 100 ? 100 : val2);
                                export_worker.ReportProgress(process);
                                current++;
                            }

                            current_step = 1;
                            process_text = "Copying localised Data folder";
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
                                    float val = (current / count) * 100;
                                    val = val > 100 ? 100 : val;
                                    process = Convert.ToInt32(Math.Truncate(val));
                                    double val2 = process / steps + (100 / steps) * current_step;
                                    status = Convert.ToInt32(val2 > 100 ? 100 : val2);
                                    export_worker.ReportProgress(process);
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
        private void workspace_worker_work(object sender, DoWorkEventArgs e)
        {
            if(workspace_select == 0)
            {
                update_files(workspace_select, properties.get("explorer_workspace"));
            }
            if (workspace_select == 1)
            {
                update_files(workspace_select, properties.get("explorer_workspace"));
            }
            if (workspace_select == 2)
            {
                update_files(workspace_select, Application.StartupPath);
            }
            //e.Result = batch_import_SE();//return temp 

        }
        //Reports import progress
        private void workspace_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double val = process / steps + (100 / steps) * current_step;
            if (val > 100)
            {
                val = 100;
            }
            loadingbox.Value = Convert.ToInt32(val);
            processbox.Value = process;
            process_status.Text = process_text;
        }
        //Reports completion of import
        private void workspace_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            console_write((String)e.Result);//copies return value to public list we declared before
            loadingbox.Value = 100;
            processbox.Value = 100;
            if(Characterlist2.SelectedIndices.Count > 0)
            {
                // Selects the character the last skin was added for
                Characterlist2.FocusedItem = Characterlist2.FindItemWithText(workspace_char);
                Characterlist2.FindItemWithText(workspace_char).Selected = true;
                Characterlist2.Select();
                Characterlist2.FindItemWithText(workspace_char).EnsureVisible();
                selected_char = new Character(workspace_char, Library, properties, uichar, logg);
                skin_ListBox_reload();
            }
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
            double val = process / steps + (100 / steps) * current_step;
            if (val > 100)
            {
                val = 100;
            }
            loadingbox.Value = Convert.ToInt32(val);
            processbox.Value = process;
            process_status.Text = process_text;
        }
        //Reports completion of export
        private void export_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            exportstatus();
            loadingbox.Value = 100;
            processbox.Value = 100;
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
            
            loadingbox.Value = status;
            processbox.Value = process;
            process_status.Text = process_text;
            
        }
        //Reports completion of meteor import
        private void meteor_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
           

            

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
            selected_char = new Character(last_char, Library, properties, uichar, logg);

            skin_ListBox_reload();
            try
            {
                //Selects the last skin
                SkinListBox.FocusedItem = SkinListBox.Items[SkinListBox.Items.Count - 1];
                SkinListBox.Items[SkinListBox.Items.Count - 1].Selected = true;
                SkinListBox.Select();
                SkinListBox.Items[SkinListBox.Items.Count - 1].EnsureVisible();

                //Reloads the UI
                skin_details_reload();
            }
            catch
            {

            }

           
            
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
            System.Diagnostics.Process.Start(Application.StartupPath+"/mmsl_packages/");
        }

        //Launches extract worker
        private void refresh_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            check_files();
        }
        //extract worker progress
        private void refresh_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadingbox.Value = status;
            processbox.Value = process;
            process_status.Text = process_text;
        }
        //extract worker complete
        private void refresh_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingbox.Value = 100;
            processbox.Value = 100;
            if (Characterlist2.SelectedIndices.Count > 0)
            {
                // Selects the character the last skin was added for
                Characterlist2.FocusedItem = Characterlist2.FindItemWithText(workspace_char);
                Characterlist2.FindItemWithText(workspace_char).Selected = true;
                Characterlist2.Select();
                Characterlist2.FindItemWithText(workspace_char).EnsureVisible();
                selected_char = new Character(workspace_char, Library, properties, uichar, logg);
                skin_ListBox_reload();
            }
            enable_controls();
            processing = false;
            appstatus.Text = "Success";
            console_write("----------------------------------------------------------------------------------------------------");
            console_write("The files were checked and their status updated");
            console_write("----------------------------------------------------------------------------------------------------");
        }

        //Launches manual worker
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            manual_meteors.Clear();
            //Getting csp files
            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_([0-9]{2}|xx|[0-9]x|x[0-9]).nut$");
            String[] files = Directory.GetFiles(this.manualfolder[0], "*.nut", SearchOption.AllDirectories);
            ArrayList manual_csps = new ArrayList();
            foreach(String file in files)
            {
                if (cspr.IsMatch(Path.GetFileName(file)))
                {
                    manual_csps.Add(file);
                }
            }

            //Getting model folders
            String[] dirs = Directory.GetDirectories(this.manualfolder[0], "*", SearchOption.AllDirectories);
            ArrayList manual_models = new ArrayList();
            foreach (String dir in dirs)
            {
                String dirname = Path.GetFileName(dir);
                //If folder name is three characters
                if(dirname.Length == 3)
                {
                    int slot;
                    //If slot can be parsed
                    if(int.TryParse(dirname.Substring(1,2),out slot)){
                        manual_models.Add(dir);
                    }
                }
            }

            //Moving the files to the appropriate location
            foreach(String csp in manual_csps)
            {
                String csp_parsed_slot = csp.Split('_')[csp.Split('_').Length - 1].Split('.')[0];
                int slot;
                if(int.TryParse(csp_parsed_slot,out slot))
                {
                    String destination = Application.StartupPath + "/mmsl_packages/unselected/meteor_xx_slot_" + slot+"/csp/";
                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }
                    String destination_file = destination + Path.GetFileName(csp);
                    File.Copy(csp, destination_file, true);
                }
            }
            //Copying models
            foreach(String model in manual_models)
            {
                String model_parsed_slot = Path.GetFileName(model).Substring(1, 2);
                int slot;
                if(int.TryParse(model_parsed_slot,out slot))
                {
                    slot++;
                    String destination = Application.StartupPath + "/mmsl_packages/unselected/meteor_xx_slot_" + slot + "/model/";
                    String parent = Path.GetFileName(Directory.GetParent(model).FullName);
                    String folder = Path.GetFileName(model);

                    String model_destination = destination + parent + "/" + folder;
                    if (!Directory.Exists(model_destination))
                    {
                        Directory.CreateDirectory(model_destination);
                    }
                    foreach(String file in Directory.GetFiles(model))
                    {
                        File.Copy(file, model_destination + "/" + Path.GetFileName(file),true);
                    }
                }
            }
            String filelist = "";
            //Listing new meteor folders
            String[] meteors = Directory.GetDirectories(Application.StartupPath + "/mmsl_packages/unselected/");
            if(meteors.Length > 0)
            {
                foreach(String meteor in meteors)
                {
                    String slot = meteor.Split('_')[meteor.Split('_').Length - 1];
                    String name = meteor.Split('_')[3];
                    foreach(String csp in Directory.GetFiles(meteor + "/csp"))
                    {
                        String csp_name = Path.GetFileName(csp).Split('_')[0] + "_" + Path.GetFileName(csp).Split('_')[1];
                        filelist += csp_name + " | ";
                    }
                    manual_meteors.Add(slot+";unselected;slot_" + slot +"; "+filelist);
                }
            }


        }
        //manual worker progress
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        //manual worker complete
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach(String val in manual_meteors)
            {
                meteorpack_gridview.Rows.Add(val.Split(';'));
            }
            started_pack = true;
        }
        #endregion

        #region Workspace
        //Grabs files for the library
        private void update_files(int mode, String update_path)
        {
            logg.log("-- starting the update process");
            //Setting base paths
            String datafolder = properties.get("datafolder");
            String source_model_path="";
            String source_csp_path = "";
            String source_csp_dlc_path = "";

            if (mode == 2)
            {
                logg.log("Refreshing workspace");
                source_model_path = update_path + "/mmsl_workspace/data/fighter/";
                 source_csp_path = update_path + "/mmsl_workspace/data/ui/replace/chr/";
                if(properties.get("unlocalised") == "1")
                {
                    source_csp_dlc_path = update_path + "/mmsl_workspace/data/ui/replace/append/chr/";
                }
                else
                {
                    source_csp_dlc_path = update_path + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
                }
                 
            }else
            {
                logg.log("S4E import/update");
                source_model_path = update_path + "/content/patch/data/fighter/";
                source_csp_path = update_path + "/content/patch/data/ui/replace/chr/";
                logg.log("source csp path is :"+ source_csp_path);
                source_csp_dlc_path = "";

                if (properties.get("unlocalised") == "1")
                {
                    source_csp_dlc_path = update_path + "/content/patch/data/ui/replace/append/chr/";
                }
                else
                {
                    source_csp_dlc_path = update_path + "/content/patch/" + datafolder + "/ui/replace/append/chr/";
                }
                logg.log("source dlc csp path is :" + source_csp_dlc_path);
            }

            String destination_model_path = Application.StartupPath + "/mmsl_workspace/data/fighter/";
            String destination_csp_path = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
            String destination_csp_dlc_path = "";
            logg.log("destination csp path is :" + destination_csp_path);


            if (properties.get("unlocalised") == "1")
            {
                destination_csp_dlc_path = Application.StartupPath + "/mmsl_workspace/data/ui/replace/append/chr/";
            }
            else
            {
                destination_csp_dlc_path = Application.StartupPath + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
            }
            logg.log("destination dlc csp path is :" + destination_csp_dlc_path);
            //Check missing files
            //fais une version par mode -------------------
            current_step = 0;
            if(mode != 0)
            {
                steps = 4;
                process_text = "Checking differences";
                logg.log("-- Checking difference");
                double current = 1;
                foreach (String character in Characters)
                {
                    logg.log("Checking character: "+character);
                    selected_char = new Character(character, Library, properties, uichar, logg);
                    selected_char.check_all_files();
                    logg.log("Files checked");
                    double val = current / Convert.ToDouble(Characters.Count) * 100;
                    this.process = Convert.ToInt32(Math.Truncate(val));
                    import_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                    current++;

                }
                current_step++;
                
            }else
            {
                steps = 3;
            }


            
            //Checking that source exists
            if (Directory.Exists(update_path))
            {
                process_text = "Importing /model files";
                //Searching for fighter/model/ differences
                logg.log("-- launching updating model");
                update_models(source_model_path, destination_model_path, mode);
                current_step++;
                process_text = "Importing ui/replace/chr files";
                //Searching for ui/replace/chr differences
                logg.log("-- launching updating csps");
                update_csps(source_csp_path, destination_csp_path, mode);
                current_step++;
                logg.log("-- launching updating dlc csps");
                process_text = "Importing ui/replace/append/chr files";
                //Searching for ui/replace/append/chr differences
                update_csps(source_csp_dlc_path, destination_csp_dlc_path, mode);

                importcode = 3;
                logg.log("Success");
            }
            else
            {
                logg.log("Error");
                importcode = 2;
            }
        }

        //Looks for all models and launches update_model for them
        private void update_models(String source, String destination, int mode)
        {
            logg.log("-- update model started");
            logg.log("source: "+ source);
            logg.log("destination: " + destination);
            //Checking source existence
            if (Directory.Exists(source))
            {
                logg.log("model source exists :"+ source);
                //Getting character folder list from fighter folder
                String[] characters = Directory.GetDirectories(source);
                //If there are folders
                if (characters.Length > 0)
                {
                    
                    //Foreach character folder
                    double current = 1;
                    foreach (String character_folder in characters)
                    {
                        logg.log("-- character_folder is :" + character_folder);
                        Boolean test = false;
                        foreach(String c in Characters)
                        {
                            try
                            {
                                if (c == Library.get_fullname_modelfolder(Path.GetFileName(character_folder)))
                                {
                                    test = true;
                                    logg.log("exists");
                                }
                            }catch(Exception e)
                            {
                                logg.log("doesn't exist in characters");
                            }
                            
                        }
                        if (test)
                        {
                            double val = current / Convert.ToDouble(characters.Length) * 100;
                            this.process = Convert.ToInt32(Math.Truncate(val));
                            import_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                            current++;
                            String current_character_folder = Path.GetFileName(character_folder);
                            String character_model_list = character_folder + "/model";
                            logg.log("model folder:"+character_model_list);
                            //Checking that there is a model folder inside
                            if (Directory.Exists(character_model_list))
                            {
                                //Getting model folders aka body
                                String[] model_folders = Directory.GetDirectories(character_model_list);

                                if (model_folders.Length > 0)
                                {
                                    //Foreach model folder
                                    foreach (String character_model_folder in model_folders)
                                    {
                                        logg.log("inside model folder:" + character_model_folder);
                                        String current_character_model_folder = Path.GetFileName(character_model_folder);
                                        //Getting cXX folders
                                        String[] models = Directory.GetDirectories(character_model_folder);
                                        //If there are cXX folders
                                        if (models.Length > 0)
                                        {
                                            //foreach cXX folder
                                            foreach (String model in models)
                                            {
                                                logg.log("cXX:" + model);
                                                String model_destination = destination + current_character_folder + "/model/" + current_character_model_folder + "/" + Path.GetFileName(model);
                                                logg.log("cXX destination:" + model_destination);
                                                update_model(model, model_destination, current_character_folder, mode);
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

        //Updates a specific model based on mode
        private void update_model(String source, String destination,String character, int mode)
        {
            //Handles the file depending on the mode
            switch (mode)
            {
                default:
                    break;

                //Replace workspace
                case 0:
                    logg.log("-- Update model");
                    //Creating destination
                    Directory.CreateDirectory(destination);
                    logg.log("Creating destination");
                    //Parsing destination slot

                    int slot;

                    if(int.TryParse(Path.GetFileName(source).Substring(1, 2),out slot))
                    {
                        logg.log("slot parsed");
                        //Checking if skin doesn't exists
                        if (!Library.check_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1))
                        {
                            logg.log("skin doesn't exist");
                            //Adds the skin to the library
                            Library.add_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1);
                        }
                        //Skin Exists
                        else
                        {
                            logg.log("skin exists");
                            //If origin = Default
                            if (Library.get_skin_origin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1) == "Default")
                            {
                                logg.log("setting default replaced");
                                //Setting origin to default replaced
                                Library.set_origin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1, "Default Replaced");
                            }
                        }
                        //Adding the model to the skin
                        new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1, "Imported skin", "Sm4sh Explorer",Library,properties,logg).add_model(source, Directory.GetParent(source).Name);

                    }
                    break;
                //Add missing files from S4E
                case 1:
                    logg.log("-- Update model");
                    //Parsing destination slot
                    int slot2;
                    if(int.TryParse(Path.GetFileName(source).Substring(1, 2),out slot2))
                    {
                        logg.log("slot parsed "+ slot2);
                        //Checking if skin exists
                        if (Library.check_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1))
                        {
                            //Getting generic modelname
                            String model = Path.GetFileName(source).Substring(0, 1) == "c" ? "cXX" : "lXX";
                            logg.log("model parsed " + model);
                            //If file is missing
                            if (Library.get_model_workspace_status(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1, Directory.GetParent(source).Name + "/" + model) == "missing")
                            {
                                logg.log("marked as missing");
                                //Adding the model to the skin
                                Skin current = new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                current.add_model(source, Directory.GetParent(source).Name);
                            }
                            else
                            {
                                logg.log("not marked as missing, setting to imported");
                                Skin current = new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                current.set_model_workspace_status(Directory.GetParent(source).Name, model, "imported");
                            }
                        }
                        else
                        {
                            logg.log("skin doesn't exist, adding one and file to it");
                            //Adds the skin to the library
                            Library.add_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1);
                            //Adding the model to the skin
                            new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot2 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_model(source, Directory.GetParent(source).Name);
                        }
                    }
                    break;
                //Actualising workspace
                case 2:
                    logg.log("-- Update model");
                    //Parsing destination slot
                    int slot3;
                    if(int.TryParse(Path.GetFileName(source).Substring(1, 2), out slot3)){
                        logg.log("slot parsed "+slot3);
                        //Checking if skin exists
                        if (Library.check_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1))
                        {
                            String model = Path.GetFileName(source);
                            model = model.Substring(0, 1) == "c" ? "cXX" : "lXX";
                            logg.log("model parsed " + model);
                            //If file is missing
                            if (Library.get_model_workspace_status(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1, Directory.GetParent(source).Name + "/" + model) == "missing")
                            {
                                logg.log("marked as missing");
                                //Adding the model to the skin
                                Skin current = new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                current.add_model(source, Directory.GetParent(source).Name);
                            }
                            else
                            {
                                logg.log("not marked as missing, setting to imported");
                                Skin current = new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                current.set_model_workspace_status(Directory.GetParent(source).Name, model, "imported");
                            }
                        }
                        else
                        {
                            logg.log("skin doesn't exist, adding one and file to it");
                            //Adds the skin to the library
                            Library.add_skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1);
                            //Adding the model to the skin
                            new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot3 + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_model(source, Directory.GetParent(source).Name);
                        }
                    }
                    break;

                case 3:

                    break;
            }
        }

        //Looks for all csps and launches update_csp for them
        private void update_csps(String source, String destination, int mode)
        {
            //Regex for detecting csp files
            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_([0-9]{2}|xx|[0-9]x|x[0-9]).nut$");

            //chr folders
            float currentcount = 1;

            //Getting chr folders
            if (!Directory.Exists(source))
            {
                logg.log("creating destination");
                Directory.CreateDirectory(source);
            }
            String[] csps = Directory.GetDirectories(source);
            //If chr contains folders
            if (csps.Length > 0)
            {
                logg.log("csp formats found");
                //Getting count for status bars
                float count = csps.Length;

                //Foreach cspformat
                foreach (String cspformat in csps)
                {
                    logg.log("- current cspformat: "+cspformat);
                    double  val = currentcount / Convert.ToDouble(csps.Length) * 100;
                    this.process = Convert.ToInt32(Math.Truncate(val));
                    import_worker.ReportProgress(process);
                    currentcount++;
                    String[] files = Directory.GetFiles(cspformat);
                    //check if cspformat contains files
                    if (files.Length > 0)
                    {
                        //foreach file
                        foreach(String csp in files)
                        {
                            logg.log("- current csp: " + Path.GetFileName(csp));
                            //If file is a csp
                            if (cspr.IsMatch(Path.GetFileName(csp)))
                            {
                                update_csp(csp, destination, mode);
                            }
                        }
                    }
                }
            }
        }

        //Updates a specific csp based on mode
        private void update_csp(String source, String destination, int mode)
        {
            logg.log("-- update csp lauched");
            logg.log("source:  " + source);
            logg.log("destination:  " + destination);
            //Parsing slot
            String slot = Path.GetFileName(source).Split('_')[3].Split('.')[0];
            int output_slot;
            //If the slot is an int
            if (int.TryParse(slot, out output_slot))
            {
                logg.log("slot parsed " + output_slot);
                //Getting foldername
                String foldername = Path.GetFileName(source).Split('_')[2];
                logg.log("foldername parsed " + foldername);
                //Checking that the character is supported by MSL
                if (Library.check_fullname_cspname(foldername))
                {
                    //Action depends on mode
                    switch (mode)
                    {
                        //Replace Workspace
                        case 0:
                            //If skin doesn't exist
                            if (!Library.check_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)))
                            {
                                logg.log("Skin doesn't exist");
                                Library.add_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot));
                            }
                            //If skin exists
                            else
                            {
                                logg.log("Skin exist");
                                //Changing origin if skin is Default
                                if (Library.get_skin_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)) == "Default")
                                {
                                    logg.log("changing to default replaced");
                                    Library.set_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot), "Default Replaced");
                                }
                            }
                            //Adding csp to skin
                            Skin cur = new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                            cur.add_csp(source);
                            cur.set_csp_workspace_status(source, "new");
                            break;

                        case 1:
                            //If skin doesn't exist
                            if (!Library.check_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)))
                            {
                                logg.log("Skin doesn't exist");
                                Library.add_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot));
                            }
                            //If skin exists
                            else
                            {
                                String csp_name = Path.GetFileName(source).Split('_')[0] + "_" + Path.GetFileName(source).Split('_')[1];
                                logg.log("Skin exist");
                                //If file is missing
                                if (Library.get_csp_workspace_status(Library.get_fullname_cspfolder(foldername), int.Parse(slot), csp_name) == "missing")
                                {
                                    logg.log("file is missing");
                                    //Adding csp to skin
                                    new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_csp(source);

                                }
                                else
                                {
                                    logg.log("file is present, setting to imported");
                                    Skin current = new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                    current.set_csp_workspace_status(source, "imported");
                                }

                                //Changing origin if skin is Default
                                if (Library.get_skin_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)) == "Default")
                                {
                                    logg.log("changing to default replaced");
                                    Library.set_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot), "Default Replaced");
                                    //Adding csp to skin
                                    new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_csp(source);
                                }
                            }
                            break;
                        //Refresh Workspace
                        case 2:
                            //If skin doesn't exist
                            if (!Library.check_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)))
                            {
                                logg.log("Skin doesn't exist");
                                Library.add_skin(Library.get_fullname_cspfolder(foldername), int.Parse(slot));
                            }
                            //If skin exists
                            else
                            {
                                logg.log("Skin exist");
                                String csp_name = Path.GetFileName(source).Split('_')[0] + "_" + Path.GetFileName(source).Split('_')[1];
                                //If file is missing
                                if (Library.get_csp_workspace_status(Library.get_fullname_cspfolder(foldername), int.Parse(slot), csp_name) == "missing")
                                {
                                    logg.log("file is missing");
                                    //Adding csp to skin
                                    new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_csp(source);

                                }else
                                {
                                    logg.log("file is present, setting to imported");
                                    Skin current = new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg);
                                    current.set_csp_workspace_status(source, "imported");
                                }

                                //Changing origin if skin is Default
                                if (Library.get_skin_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot)) == "Default")
                                {
                                    logg.log("changing to default replaced");
                                    Library.set_origin(Library.get_fullname_cspfolder(foldername), int.Parse(slot), "Default Replaced");
                                    //Adding csp to skin
                                    new Skin(Library.get_fullname_cspfolder(foldername), output_slot, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_csp(source);
                                }
                            }
                            
                            break;

                    }
                }
            }
        }


        private void check_files()
        {
            steps = 1;
            current_step = 0;
                process_text = "Checking differences";
                logg.log("-- Checking difference");
                double current = 1;
                foreach (String character in Characters)
                {
                    logg.log("Checking character: " + character);
                    selected_char = new Character(character,Library,properties,uichar,logg);
                    selected_char.check_all_files();
                    logg.log("Files checked");
                    double val = current / Convert.ToDouble(Characters.Count) * 100;
                    this.process = Convert.ToInt32(Math.Truncate(val));
                    refresh_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                    current++;

                }

            }






        #endregion

        #endregion

        
    }


}
