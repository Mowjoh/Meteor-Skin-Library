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
        String selected_cell_charname = "";
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
        int process = 0;
        int status = 0;
        double val;
        String process_text;
        int workspace_select = 0;
        int backup_select = 0;
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
        String appversion = "";
        String appbaseversion = "0_6";
        #endregion
        #region Messages
        String[][] messages_reset = new String[3][]
        {
            new String[] {
                "The library couldn't be reset",
                "The workspace couldn't be reset",
                "The config couldn't be reset",
                "The meta couldn't be reset"},
            new String[] {
                "Reset cancelled" },
            new String[] {
                "The library was reset",
                "The workspace was reset",
                "The config was reset"}
        };
        String[][] messages_skin = new String[3][]
        {
            new String[] {
                "The skin slot was added"},
            new String[] {
                "" },
            new String[] {
                "The skin slot wasn't added"}
        };

        String[][] messages_import = new String[3][]
        {
            new String[] {
                ""},
            new String[] {
                "" },
            new String[] {
                "The import preparation failed"}
        };


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
                write("Default Library not found, please add Default_Library.xml in the /mmsl_config folder.");
                lib = false;
            }
            //Check Default_Config.xml presence
            if (!File.Exists(Application.StartupPath + "/mmsl_config/Default_Config.xml"))
            {
                write("Default Config not found, please add Default_Config.xml in the /mmsl_config folder.");
                conf = false;
            }

            //Version
            XmlDocument xml2 = new XmlDocument();
            xml2.Load(Application.StartupPath + "/Meteor Skin Library.exe.manifest");
            XmlNode nodes2 = xml2.SelectSingleNode("//*[local-name()='assembly']/*[local-name()='assemblyIdentity']");
            this.appversion = nodes2.Attributes[1].Value;


            #endregion


            if (conf && lib)
            {
                #region Default Copy
                //Checks Config.xml presence, if not creates one based on Default_Config.xml
                if (!File.Exists(Application.StartupPath + "/mmsl_config/Config.xml"))
                {
                    write("Creating Config");
                    File.Copy(properties.property_get("default_config"), Application.StartupPath + "/mmsl_config/Config.xml");
                }
                properties.set_library_path(Application.StartupPath + "/mmsl_config/Config.xml");
                properties.property_add("current_library", Application.StartupPath + "/mmsl_config/Library.xml");
                write("Config loaded : mmsl_config/Config.xml");

                //Checks Library.xml presence, if not creates one based on Default_Library.xml
                if (!File.Exists(Application.StartupPath + "/mmsl_config/Library.xml"))
                {
                    write("Creating Library");
                    File.Copy(properties.property_get("default_library"), Application.StartupPath + "/mmsl_config/Library.xml");
                }
                Library = new LibraryHandler(properties.property_get("current_library"));
                write("Library loaded : mmsl_config/Library.xml");
                #endregion

                #region UI Init
                //Loads Character List
                manual_meteors = new ArrayList();
                Characters = Library.get_character_list();
                init_character_ListBox();
                state_check();
                label_app_status.Text = "Ready";
                processing = false;
                url_worker.RunWorkerAsync();
                packer_reset();
                this.status_images = new ImageList();
                status_images.ImageSize = new Size(24, 24);
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/unknown.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/missing.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/new.png"));
                status_images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/checked.png"));
                listview_skins.SmallImageList = status_images;

                if (properties.property_get("dev") != "1")
                {
                    adOptionsToolStripMenuItem.Visible = false;
                }
                #endregion
                if (properties.property_get("logging") == "1")
                {
                    logg = new Logger(1, true);
                }
                else
                {
                    logg = new Logger(1, false);
                }

                //Launches config if not edited
                region_select();

                #region ui_char_db 
                uichar = new UICharDBHandler(properties.property_get("explorer_workspace"), properties.property_get("datafolder"));
                if (!uichar.imported)
                {
                    logg.log("ui_char_db not imported");
                    write("ui_character_db was not found in Sm4sh Explorer, please add it and relaunch this software!");
                }
                else
                {
                    logg.log("ui_char_db imported");
                    write("ui_character_db was found, congrats !");
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
                    block_controls();
                    meteor_download(args);
                }
                else
                {
                    logg.log("no args passed");
                    //Launch update process
                    proper_update();
                    check_updater();


                }


            }

        }

        //Top Menu
        #region Menu Commands
        #region FileMenu 
        //Closes the application
        private void menu_exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
        //Launches S4E through a menu
        private void menu_tool_launch_s4e(object sender, EventArgs e)
        {
            tool_launch_s4e();
        }
        #endregion
        #region SkinMenu 
        //Adds a skin to the selected character
        private void menu_skin_add(object sender, EventArgs e)
        {
            //If there is at least one selected item
            if (listview_characters.SelectedIndices.Count > 0)
            {
                //Adds a skin
                if (selected_char.add_skin())
                {
                    //Reloads the skin list and checks for the status
                    skin_ListBox_reload();
                    state_check();

                    //Selects the last skin
                    focus_skin(listview_skins.Items.Count - 1);

                    //Writes operation status
                    logg.log("Skin added for " + selected_char.fullname + " in slot " + (listview_skins.Items.Count + 1));
                    write("Skin added for " + selected_char.fullname + " in slot " + (listview_skins.Items.Count + 1), 2);
                }
                else
                {
                    write("An error has happened", 0);
                }


            }
            else
            {
                //Warns User about misuse of the function
                write("Please select a Character first", 1);
            }
        }
        //Resets the pack session
        private void menu_packer_reset(object sender, EventArgs e)
        {
            packer_reset();
        }
        #endregion
        #region WorkspaceMenu
        #region MSL
        //Scans the workspace for differences and then imports any missing file found
        private void menu_refresh_workspace_missing(object sender, EventArgs e)
        {
            this.workspace_select = 2;
            //Processing 
            process_start("Importing missing files in workspace", true);
            if (listview_characters.SelectedIndices.Count > 0)
            {
                workspace_char = listview_characters.SelectedItems[0].Text;
            }
            import_worker.RunWorkerAsync();
        }
        //Scans the workspace for differences
        private void menu_refresh_workspace(object sender, EventArgs e)
        {
            //Processing 
            process_start("Refreshing file list", true);
            if (listview_characters.SelectedIndices.Count > 0)
            {
                workspace_char = listview_characters.SelectedItems[0].Text;
            }
            refresh_worker.RunWorkerAsync();
        }
        private void menu_s4e_export(object sender, EventArgs e)
        {
            if (MessageBox.Show("Doing this will erase fighter/[name]/model for every character that has mods and ui/replace/chr and ui/replace/append/chr from Smash Explorer's workspace. Are you sure you've made a backup? If yes, you can validate these changes and replace S4E's content by MSL's content", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                process_start("Exporting to Sm4sh Explorer", true);
                if (listview_characters.SelectedIndices.Count > 0)
                {
                    workspace_char = listview_characters.SelectedItems[0].Text;
                }
                export_worker.RunWorkerAsync();
            }
        }
        #endregion
        #region S4E
        //Launches "Replace Workspace"
        private void menu_s4e_import(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will import all the skins from S4E. Doing this will erase your actual workspace, and library.", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //Processing 
                process_start("Importing from Sm4sh Explorer", true);
                if (reset_import())
                {
                    workspace_select = 0;
                    if (listview_characters.SelectedIndices.Count > 0)
                    {
                        last_char = listview_characters.SelectedItems[0].Text;
                    }
                    import_worker.RunWorkerAsync();
                }
                else
                {
                    message("import", 0, 0);
                }
            }
        }
        private void menu_s4e_import_missing(object sender, EventArgs e)
        {
            this.workspace_select = 1;

            //Processing 
            process_start("Importing missing files from S4E", true);
            if (listview_characters.SelectedIndices.Count > 0)
            {
                workspace_char = listview_characters.SelectedItems[0].Text;
            }
            import_worker.RunWorkerAsync();
        }

        #endregion
        #region Backup
        private void backupMSLsWorkspaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                backup_select = 1;
                this.steps = 1;
                process_start("Backuping MSL's Workspace", true);
                backup_worker.RunWorkerAsync();
            }
            catch (Exception)
            {

                write("MSL's backup failed", 0);
            }

        }
        private void backupS4EsWorkspaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                backup_select = 2;
                this.steps = 1;
                process_start("Backuping S4E's Workspace", true);
                backup_worker.RunWorkerAsync();
            }
            catch (Exception)
            {
                write("S4E's backup failed", 0);
            }
        }
        #endregion
        #region Folders
        //Opens the application startup path
        private void folder_open_startup_path(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath);
        }
        //Opens the folder mmsl_workspace
        private void folder_open_workspace(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "/mmsl_workspace");
        }
        //Opens the folder mmsl_packages
        private void folder_open_packages(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + "/mmsl_packages");
        }
        //Opens the folder mmsl_backups
        private void folder_open_backups(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Application.StartupPath + "/mmsl_backups");
            }
            catch
            {
                write("No backups yet", 0);
            }

        }
        #endregion

        #endregion
        #region Tools
        //Launch S4E
        private void tool_launch_s4e()
        {
            //Setting up variables
            ProcessStartInfo pro = new ProcessStartInfo();
            String s4path = properties.property_get("explorer_workspace");
            String path = Directory.GetParent(s4path).ToString() + "/Sm4shFileExplorer.exe";
            pro.FileName = path;
            pro.WorkingDirectory = Directory.GetParent(s4path).ToString();
            //Process start
            Process x = Process.Start(pro);
            //Logging operation
            logg.log("trying to launch S4E at " + path);
        }
        #endregion
        #region FileBank
        private void meteorFileBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //launching window
            FilebankWindow fbw = new FilebankWindow();
            fbw.ShowDialog();
        }
        #endregion
        #region OptionMenu 
        //Menu Config Function
        public void menu_config(object sender, EventArgs e)
        {
            //launching window
            config cnf = new config();
            cnf.ShowDialog();
            //Checking state after closing the config
            state_check();
        }
        //Menu Reset Library
        private void menu_reset_library(object sender, EventArgs e)
        {
            //Confirming the changes 
            if (MessageBox.Show("Doing this will erase all entries in the Library. Skins are still present in the mmsl_workspace folder. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    //deletes and recreates Library
                    File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");
                    //Warns the user
                    message("Reset", 2, 0);

                    //reloading character list
                    Characters = Library.get_character_list();
                    init_character_ListBox();
                    listview_characters.Items[0].Selected = true;
                    //Checks the state
                    state_check();
                }
                catch
                {
                    //Shows the error
                    message("Reset", 0, 0);
                }
            }
            else
            {
                message("Reset", 1, 0);
            }
        }
        //mmsl_workspace reset button
        private void menu_reset_workspace(object sender, EventArgs e)
        {
            //Confirming task
            if (MessageBox.Show("Doing this will erase all contents of the mmsl_workspace folder which contains every file you've added. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    Directory.Delete(Application.StartupPath + "/mmsl_workspace", true);
                    Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");

                    Characters = Library.get_character_list();
                    init_character_ListBox();
                    state_check();
                    message("Reset", 2, 1);

                }
                catch
                {
                    message("Reset", 0, 1);
                }
            }
            else
            {
                message("Reset", 1, 0);
            }
        }
        //Config Reset button
        private void menu_reset_config(object sender, EventArgs e)
        {
            if (MessageBox.Show("Doing this will erase all configuration changes. Continue with this destruction?", "Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    //Deleting previous config and creating one based on default
                    File.Delete(Application.StartupPath + "/mmsl_config/Config.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Config.xml", Application.StartupPath + "/mmsl_config/Config.xml");

                    config cnf = new config();

                    cnf.ShowDialog();

                    Characters = Library.get_character_list();
                    init_character_ListBox();
                    state_check();
                    message("Reset", 2, 2);
                }
                catch
                {
                    message("Reset", 0, 2);
                }
            }
            else
            {
                message("Reset", 1, 0);
            }
        }
        //Reset all button
        private void menu_reset_all(object sender, EventArgs e)
        {
            //Confirming process
            if (MessageBox.Show("Doing this will erase all configuration changes. It will erase all files of every mod you've added. The library containing skin information will be deleted. Continue with this Supermassive black-hole type destruction?", "Super Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    //Deleting previous config and creating one based on default
                    File.Delete(Application.StartupPath + "/mmsl_config/Config.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Config.xml", Application.StartupPath + "/mmsl_config/Config.xml");

                    config cnf = new config();

                    cnf.ShowDialog();

                    message("Reset", 2, 2);

                    try
                    {
                        //Deleting previous workspace and recreating the folder
                        Directory.Delete(Application.StartupPath + "/mmsl_workspace", true);
                        Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");

                        message("Reset", 2, 1);

                        try
                        {
                            //deletes and recreates Library
                            File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
                            File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");
                            //Warns the user
                            message("Reset", 2, 0);

                            //reloading character list
                            Characters = Library.get_character_list();
                            init_character_ListBox();
                            listview_characters.Items[0].Selected = true;
                            //Checks the state
                            state_check();
                        }
                        catch
                        {
                            //Shows the error
                            message("Reset", 0, 0);
                        }

                    }
                    catch
                    {
                        message("Reset", 0, 1);
                    }
                }
                catch
                {
                    message("Reset", 0, 2);
                }
            }
            else
            {
                message("Reset", 1, 0);
            }
        }
        //Resets library and workspace
        private Boolean reset_import()
        {
            try
            {
                //Deleting previous workspace and recreating the folder
                Directory.Delete(Application.StartupPath + "/mmsl_workspace", true);
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace");

                message("Reset", 2, 1);

                try
                {
                    //deletes and recreates Library
                    File.Delete(Application.StartupPath + "/mmsl_config/Library.xml");
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Library.xml", Application.StartupPath + "/mmsl_config/Library.xml");
                    //Warns the user
                    message("Reset", 2, 0);

                    try
                    {
                        //Deleting old meta
                        String[] metas = Directory.GetDirectories(Application.StartupPath + "/mmsl_config/meta");
                        if (metas.Length > 0)
                        {
                            foreach (String meta in metas)
                            {
                                Directory.Delete(meta, true);
                            }
                        }
                        //reloading character list
                        Characters = Library.get_character_list();
                        init_character_ListBox();
                        listview_characters.Items[0].Selected = true;
                        //Checks the state
                        state_check();

                        new UICharDBHandler(properties.property_get("explorer_workspace"), properties.property_get("datafolder"));

                        return true;
                    }
                    catch
                    {
                        message("Reset", 0, 3);
                    }
                }
                catch
                {
                    //Shows the error
                    message("Reset", 0, 0);
                }
            }
            catch
            {
                message("Reset", 0, 1);
            }
            return false;
        }
        #endregion<
        #region HelpMenu
        //Shows the about window
        private void about(object sender, EventArgs e)
        {
            if (MessageBox.Show("Segtendo Build: " + appversion + "\n\nCreator : Mowjoh \n\nHey, you ! Thanks for using Meteor Skin Library !\nYou da real MVP!", "Meteor Skin Library Beta", MessageBoxButtons.OK) == DialogResult.OK)
            {

            }
        }
        //Opens the wiki in the browser
        private void menu_wiki(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Mowjoh/Meteor-Skin-Library/wiki");
        }
        #endregion
        #endregion

        

        //Character tab
        #region Character Tab
        #region Interactions
        //When a character is selected
        private void character_selected(object sender, EventArgs e)
        {
            try
            {
                selected_char = new Character(listview_characters.SelectedItems[0].Text, Library, properties, uichar, logg);

                skin_ListBox_reload();

                state_check();
            }
            catch
            {
                write("Character select failed", 0);
            }

        }

        //When a character is selected NEW
        private void Characterlist2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listview_characters.SelectedItems.Count > 0)
                {
                    selected_char = new Character(listview_characters.SelectedItems[0].Text, Library, properties, uichar, logg);
                    skin_ListBox_reload();
                    state_check();
                    textBox_character_ui.Text = "";
                }

            }
            catch
            {
                write("Could not change selected character", 0);
            }
        }
        #endregion
        #region CSS Tab
        //UI char db override settings saved
        private void character_uichar_override(object sender, EventArgs e)
        {
            try
            {
                int val = 0;
                if (int.TryParse(textBox_character_ui.Text, out val))
                {
                    uichar.setFile(int.Parse(Library.get_ui_char_db_id(listview_characters.SelectedItems[0].Text)), 7, val);
                    write("Override ui_character_db settings saved", 2);
                }
                else
                {
                    write("What you entered wasn't a number", 1);
                }
            }
            catch
            {
                write("Could not update ui_character_db", 0);
            }
        }
        #endregion
        #region Action tab
        private Boolean reset_skins(int startindex, int endindex)
        {
            //If a character is selected
            if (listview_characters.SelectedIndices.Count > 0)
            {
                try
                {

                    for (int i = startindex; i <= endindex; i++)
                    {
                        try
                        {
                            Skin work_skin = (Skin)selected_char.skins[i];
                            if (work_skin.origin == "Default")
                            {
                                work_skin.clean_skin(0);
                                work_skin.reload_default_skin();
                            }
                            else
                            {
                                work_skin.delete_skin();
                            }
                        }
                        catch
                        {

                        }
                    }

                    write("Skins reset complete", 2);
                }
                catch (Exception)
                {
                    write("Skins reset presented errors", 0);
                }

                selected_char.getSkins();
                skin_ListBox_reload();

            }

            return true;
        }

        private void reset_default_skins(object sender, EventArgs e)
        {
            //If a character is selected
            if (listview_characters.SelectedIndices.Count > 0)
            {
                try
                {
                    int basecount = 0;
                    if (selected_char.fullname == "Little Mac")
                    {
                        basecount = 16;
                    }
                    else
                    {
                        basecount = 8;
                    }

                    reset_skins(1, basecount);

                }
                catch
                {

                }
            }
        }

        private void reset_extra_skins(object sender, EventArgs e)
        {
            //If a character is selected
            if (listview_characters.SelectedIndices.Count > 0)
            {
                try
                {
                    int basecount = 0;
                    if (selected_char.fullname == "Little Mac")
                    {
                        basecount = 16;
                    }
                    else
                    {
                        basecount = 8;
                    }

                    reset_skins(basecount, selected_char.skins.Count);
                }
                catch
                {

                }
            }
        }

        private void reset_all_skins(object sender, EventArgs e)
        {
            //If a character is selected
            if (listview_characters.SelectedIndices.Count > 0)
            {
                try
                {
                    reset_skins(0, selected_char.skins.Count);
                }
                catch
                {


                }
            }
        }
        #endregion
        #endregion

        //Skin tab
        #region Skin Tab 
        #region Interactions
        //When a skin is selected
        private void skin_selected(object sender, EventArgs e)
        {
            try
            {
                skin_details_reload();
                state_check();
                metadata_reload();
            }
            catch
            {
                write("Could not select skin", 0);
            }

        }

        //When the move up button is pressed
        private void move_up_skin(object sender, EventArgs e)
        {
            try
            {
                if (selected_skin.origin != "Default")
                {
                    selected_char.swap_skin(listview_skins.SelectedIndices[0], listview_skins.SelectedIndices[0] - 1);
                    skin_ListBox_reload();

                    focus_skin(selected_skin.modelslot);
                }
            }
            catch
            {
                write("the process messed up, you may be in trouble :o", 0);
            }


        }

        //When the move down button is pressed
        private void move_down_skin(object sender, EventArgs e)
        {
            try
            {
                if (selected_skin.origin != "Default")
                {
                    selected_char.swap_skin(listview_skins.SelectedIndices[0], listview_skins.SelectedIndices[0] + 1);
                    skin_ListBox_reload();
                    listview_skins.FocusedItem = listview_skins.Items[selected_skin.modelslot];
                    listview_skins.Items[selected_skin.modelslot].Selected = true;
                    listview_skins.Select();
                    listview_skins.Items[selected_skin.modelslot].EnsureVisible();
                }
            }
            catch
            {
                write("the process messed up, you may be in trouble :o", 0);
            }


        }
        #endregion
        #region File Info Tab
        //Skin Info Saved button is pressed
        private void save_info_button(object sender, EventArgs e)
        {
            set_skin_libraryname();
        }

        //Skin Info Saved by enter key press
        private void save_info_enter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                set_skin_libraryname();
            }
        }

        //Sets the info
        private void set_skin_libraryname()
        {
            try
            {
                int index = listview_skins.SelectedIndices[0];
                this.selected_skin.set_library_name(textbox_skin_libraryname.Text);
                skin_ListBox_reload();
                state_check();

                //Selects the last skin
                focus_skin(index);


            }
            catch
            {
                write("Could not set library name", 0);
            }
        }

        //When Delete is pressed
        private void skin_delete(object sender, EventArgs e)
        {
            int index = listview_skins.SelectedIndices[0] + 1;
            int saved_index = listview_skins.SelectedIndices[0];
            int max = listview_skins.Items.Count;

            try
            {
                if (selected_skin.origin == "Default" | selected_skin.origin == "Default Replaced")
                {
                    write("Thy cannot delete Default slots",1);
                }
                else
                {
                    try
                    {
                        selected_char.delete_skin(this.selected_skin.modelslot);
                        try
                        {
                            Library.reload_skin_order(selected_char.fullname);

                            try
                            {
                                selected_char.getSkins();

                                skin_ListBox_reload();
                                skin_details_reload();
                                write("Deleted slot " + index, 2);

                                try
                                {
                                    uichar.setFile(int.Parse(Library.get_ui_char_db_id(listview_characters.SelectedItems[0].Text)), 7, listview_skins.Items.Count);


                                }
                                catch
                                {
                                    write("Could not change ui_character_db", 0);
                                }
                            }
                            catch
                            {
                                write("Could not reload the skins info", 0);
                            }
                        }
                        catch
                        {
                            write("Could not reload the skin order", 0);
                        }
                    }
                    catch
                    {
                        write("Could not delete the skin files", 0);
                    }
                }

                //focus
                state_check();
                if (!(saved_index + 1 < listview_skins.Items.Count))
                {
                    //Selects the last skin
                    focus_skin(saved_index - 1);
                }
                else
                {
                    //Selects the last skin
                    focus_skin(listview_skins.Items.Count - 1);
                }
                
            }
            catch
            {
                write("Could not delete the skin", 0);
            }
        }

        //When Clean Files is pressed
        private void clean_files_clicked(object sender, EventArgs e)
        {
            try
            {
                this.selected_skin.clean_skin(0);
                try
                {
                    skin_details_reload();
                    skin_ListBox_reload();
                    state_check();

                }
                catch
                {
                    write("could not reload skin details", 0);
                }
            }
            catch
            {
                write("could not clean files", 0);
            }
        }

        //packages skin into meteor skin
        private void package_meteor(object sender, EventArgs e)
        {
            try
            {
                this.selected_skin.package_meteor();
                try
                {
                    pack_add_item(selected_skin.get_meteor_info());
                    write("This skin was added to the current pack session", 2);
                    started_pack = true;
                }
                catch
                {
                    write("This skin was not pushed to the file packer", 0);
                }
            }
            catch
            {
                write("This skin was not packed", 0);
            }

        }
        #endregion
        #region File Manager Tab
        //On model selected
        private void model_selected(object sender, EventArgs e)
        {
            try
            {
                if (listview_skin_models.SelectedItems.Count == 1)
                {
                    label_skin_selected_model.Text = "Selected Model : " + listview_skin_models.SelectedItems[0].Text;
                    button_skin_delete_model.Enabled = true;
                }
                state_check();
            }
            catch
            {
                write("could not select model", 0);
            }

        }
        //On model delete
        private void remove_selected_model_Click(object sender, EventArgs e)
        {
            try
            {
                selected_skin.delete_model(listview_skin_models.SelectedItems[0].Text);
                skin_details_reload();
                state_check();
            }
            catch (Exception)
            {

                write("The model could not be properly removed");
            }
        }
        //When a csp is selected
        private void csp_selected(object sender, EventArgs e)
        {
            try
            {
                if (listview_skin_csp.SelectedItems.Count == 1)
                {
                    selected_csp_name.Text = "Selected CSP : " + listview_skin_csp.SelectedItems[0].Text;
                    button_skin_delete_csp.Enabled = true;
                }
                state_check();
            }
            catch (Exception)
            {

                write("Could not select csp", 0);
            }
        }
        //When a csp is deleted
        private void remove_selected_csp_Click(object sender, EventArgs e)
        {
            try
            {
                this.selected_skin.delete_csp(listview_skin_csp.SelectedItems[0].Text);
                skin_details_reload();
                state_check();
            }
            catch (Exception)
            {

                write("The csp couldn't be properly removed", 0);
            }
        }

        //Contextual menus
        #region Contextual Menus
        private void model_click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listview_skin_models.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    model_menu_strip.Show(Cursor.Position);
                }
            }
        }

        private void model_context(object sender, EventArgs e)
        {
            String path = selected_skin.get_model_path(listview_skin_models.SelectedItems[0].Text) + "/";
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch
            {
                write("Could not open folder", 0);
            }
        }

        private void csps_click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listview_skin_csp.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    csps_menu_strip.Show(Cursor.Position);
                }
            }
        }

        private void csps_context(object sender, EventArgs e)
        {
            String path = Path.GetDirectoryName(selected_skin.get_csp_path(listview_skin_csp.SelectedItems[0].Text)) + "/";
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch
            {
                write("Could not open folder", 0);
            }
        }

        #endregion

        #endregion
        #region Metadata Tab
        //When you save all metadata
        void meta_save(object sender, EventArgs e)
        {
            try
            {
                String author = textbox_skin_meta_author.Text;
                String version = textbox_skin_meta_version.Text;
                String name = textbox_skin_meta_name.Text;
                String texidfix = textbox_skin_meta_texidfix.Text;
                this.selected_skin.saveMeta(author, version, name, texidfix);
            }
            catch (Exception)
            {

                write("Could not save meta", 0);
            }
        }
        #endregion
        #endregion

        //Skin detector 3000 tab
        #region Awesome Skin Detector tab

        #endregion

        //Skin Packing tab
        #region Skin Packer tab

        //Deletes the packed skins and resets the packer interface
        private void packer_reset()
        {
            try
            {
                //Deletes all directories
                foreach (String dir in Directory.GetDirectories(Application.StartupPath + "/mmsl_packages"))
                {
                    Directory.Delete(dir, true);
                }
                //Removes the meta if it exists
                if (File.Exists(Application.StartupPath + "/mmsl_packages/meta.xml"))
                {
                    File.Delete(Application.StartupPath + "/mmsl_packages/meta.xml");
                }

                //Resets the lists
                packer_dropzone.Items.Clear();
                packer_skinlist.Rows.Clear();

                //Enabling dropping of the manual folder
                packer_dropzone.Enabled = true;

                //Informing user
                write("Meteor skin pack session reset", 2);
            }
            catch (Exception)
            {
                write("Meteor skin pack reset unsuccessful", 0);
            }
        }

        //Archives the current skin pack session
        private void packer_archive(object sender, EventArgs e)
        {
            try
            {
                if (!processing)
                {
                    archive_worker.RunWorkerAsync();
                    loadingbox.Style = ProgressBarStyle.Marquee;
                    label_app_status.Text = "Archiving files...";
                    processing = true;
                    block_controls();
                }
            }
            catch (Exception)
            {
                write("The archiving process failed", 0);
                throw;
            }

        }

        public void pack_add_item(String[] values)
        {
            try
            {
                packer_skinlist.Rows.Add(values);
            }
            catch
            {
                write("Could not add the item", 0);
            }

        }

        private void manual_drop(object sender, DragEventArgs e)
        {
            try
            {
                this.manualfolder = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                packer_dropzone.Enabled = false;
                packer_dropzone.Items.Add("Manual folder detected / Skins-> Reset Package folder to restart");

                manual_worker.RunWorkerAsync();
            }
            catch (Exception)
            {

                write("could not process the dropped folder", 0);
            }
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
            try
            {
                if (started_pack)
                {
                    if (packer_skinlist.SelectedCells.Count > 0)
                    {
                        if (packer_skinlist.SelectedCells[0].ColumnIndex == 1)
                        {
                            selected_cell_charname = packer_skinlist.SelectedCells[0].Value.ToString();
                            selected_cell_row = packer_skinlist.SelectedCells[0].RowIndex;
                            selected_cell_column = 1;
                        }
                        if (packer_skinlist.SelectedCells[0].ColumnIndex == 2)
                        {
                            selected_cell_row = packer_skinlist.SelectedCells[0].RowIndex;
                            selected_cell_skin_name = packer_skinlist.Rows[selected_cell_row].Cells[2].Value.ToString();
                            selected_cell_column = 2;
                        }
                    }
                }
            }
            catch (Exception)
            {
                write("could not select another cell", 0);
            }
        }

        private void meteorpack_gridview_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (started_pack)
                {
                    if (selected_cell_column == 1)
                    {
                        String packname = packer_skinlist.Rows[selected_cell_row].Cells[2].Value.ToString();

                        String source = Application.StartupPath + "/mmsl_packages/" + selected_cell_charname + "";
                        String destination = Application.StartupPath + "/mmsl_packages/" + packer_skinlist.SelectedCells[0].Value.ToString() + "/";


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
                        selected_cell_charname = packer_skinlist.SelectedCells[0].Value.ToString();
                    }

                    if (selected_cell_column == 2)
                    {
                        String packname = packer_skinlist.Rows[selected_cell_row].Cells[2].Value.ToString();

                        String source = Application.StartupPath + "/mmsl_packages/" + packer_skinlist.Rows[selected_cell_row].Cells[1].Value.ToString() + "/";
                        String destination = Application.StartupPath + "/mmsl_packages/" + packer_skinlist.Rows[selected_cell_row].Cells[1].Value.ToString() + "/";


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
                        selected_cell_charname = packer_skinlist.SelectedCells[0].Value.ToString();
                    }

                }
            }
            catch (Exception)
            {
                write("Could not make the appropriate changes", 0);
                throw;
            }

        }

        private void meteor_pack(object sender, EventArgs e)
        {
            try
            {
                if (!processing)
                {
                    String author_name = textbox_packer_author.Text;
                    String pack_version = textbox_packer_version.Text;
                    File.Copy(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml", Application.StartupPath + "/mmsl_packages/meta.xml", true);

                    //Creating XML files
                    foreach (DataGridViewRow row in packer_skinlist.Rows)
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
                    process_start("Archiving files...", false);
                }
            }
            catch (Exception)
            {
                write("The packing went wrong", 0);
            }

        }

        #endregion



        //Interface functions
        #region Interface

        #region Focus
        //Focuses the UI on a skin
        private void focus_skin(int position)
        {
            try
            {
                listview_skins.FocusedItem = listview_skins.Items[position];
                listview_skins.Items[position].Selected = true;
                listview_skins.Select();
                listview_skins.Items[position].EnsureVisible();
            }
            catch
            {
                write("Could not focus the specified skin", 0);
            }
            
        }
        private void focus_character(String workspace_chars)
        {
            try
            {
                // Selects the character the last skin was added for
                listview_characters.FocusedItem = listview_characters.FindItemWithText(workspace_chars);
                listview_characters.FindItemWithText(workspace_chars).Selected = true;
                listview_characters.Select();
                listview_characters.FindItemWithText(workspace_chars).EnsureVisible();
                selected_char = new Character(workspace_chars, Library, properties, uichar, logg);
                skin_ListBox_reload();
            }
            catch
            {
                write("Could not focus the specified character", 0);
            }
            
        }

        #endregion
        #region Reloads 
        //Reloads Skin Details 
        private void skin_details_reload()
        {
            try
            {
                int slot = listview_skins.SelectedIndices.Count > 0 ? listview_skins.SelectedIndices[0] : -1;
                logg.log("-- attempting to reload skin details for slot " + slot);
                //emptying lists
                if (slot != -1)
                {
                    //Actualizing skins
                    selected_char.getSkins();
                    this.selected_skin = (Skin)selected_char.skins[slot];

                    //Clearing file boxes
                    listview_skin_csp.Clear();
                    listview_skin_models.Clear();
                    button_skin_delete_csp.Enabled = false;

                    //Setting library info
                    textbox_skin_slot.Text = this.selected_skin.slotstring;
                    textbox_skin_libraryname.Text = this.selected_skin.libraryname;
                    textbox_skin_origin.Text = this.selected_skin.origin;


                    logg.log("emptied lists, looking for csp");
                    //checking csps
                    if (this.selected_skin.csps.Count > 0)
                    {
                        //adding csps
                        foreach (String csp in this.selected_skin.csps)
                        {
                            listview_skin_csp.Items.Add(csp);
                            ListViewItem lvi = new ListViewItem(csp);
                        }
                    }
                    logg.log("looking for model");
                    //Checking models
                    if (this.selected_skin.models.Count > 0)
                    {
                        //adding models
                        foreach (String model in this.selected_skin.models)
                        {
                            listview_skin_models.Items.Add(model);
                        }
                    }
                    //setting delete button
                    if (this.selected_skin.origin != "Default")
                    {
                        logg.log("Setting delete option to enabled");
                        button_skin_delete.Enabled = true;
                    }
                    else
                    {
                        logg.log("Setting delete option to disabled");
                        button_skin_delete.Enabled = false;
                    }

                    button_skin_clean.Enabled = true;

                    logg.log("Setting skin to imported");

                    //Setting to imported to remove the new icon
                    this.selected_skin.set_imported();
                    if (this.selected_skin.missing)
                    {
                        logg.log("skin has missing files");
                        listview_skins.SelectedItems[0].ImageIndex = 1;
                    }
                    else
                    {
                        logg.log("skin has no missing files");
                        listview_skins.SelectedItems[0].ImageIndex = 3;
                        listview_skins.SelectedItems[0].ForeColor = Color.Black;
                    }

                }
            }
            catch (Exception)
            {

                write("Could not reload skin details", 0);
            }



        }
        //Reloads Skin List
        private void skin_ListBox_reload()
        {
            try
            {
                if (listview_characters.SelectedIndices.Count > 0)
                {
                    logg.log("-- Attempting listbox reload");
                    listview_skins.Items.Clear();
                    selected_char.getSkins();
                    foreach (Skin skin in selected_char.skins)
                    {
                        logg.log("cycling through skins");
                        ListViewItem item = new ListViewItem("Slot " + skin.slotstring + " - " + skin.libraryname);
                        if (skin.unknown)
                        {
                            logg.log("unknown skin found at slot " + skin.slotstring);
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
                        listview_skins.Items.Add(item);
                    }
                }
            }
            catch (Exception)
            {

                write("Could not reload skin list", 0);
            }
        }
        //Reloads MetaData
        private void metadata_reload()
        {
            try
            {
                int slot = listview_skins.SelectedIndices.Count > 0 ? listview_skins.SelectedIndices[0] : -1;
                if (slot != -1)
                {
                    //Assign values
                    textbox_skin_meta_author.Text = this.selected_skin.author;
                    textbox_skin_meta_version.Text = this.selected_skin.version;
                    textbox_skin_meta_name.Text = this.selected_skin.metaname;
                    textbox_skin_meta_texidfix.Text = this.selected_skin.texidfix;
                }
            }
            catch (Exception)
            {

                write("Could not reload metadata", 0);
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
            try
            {
                logg.log("-- dropped a model folder");
                this.model_folder_list = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                batch_copy_model(this.model_folder_list, this.selected_skin);
                state_check();
                skin_details_reload();
            }
            catch
            {
                write("Could not add the model", 0);
            }
           
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
            try
            {
                string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (Directory.Exists(FileList[0]))
                {
                    logg.log("-- dropped a csp dir");
                    this.csp_file_list = FileList;
                    batch_copy_csp(FileList, listview_skins.SelectedIndices[0]);
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
                            logg.log("detected a csp file " + file);
                            write("Detected files were moved to the selected slot");
                            skin_details_reload();
                        }
                    }
                }
            }
            catch
            {
                write("Could not add model", 0);
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
            try
            {
                logg.log("-- dropped something in the meteor zone");
                this.slot_file_list = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                batch_add_slot(listview_skins.Items.Count + 1);
                state_check();
                skin_details_reload();
            }
            catch
            {
                write("could not install the meteor skin(s)", 0);
            }
            
        }
        #endregion
        #region Inits 
        //Filling Character list
        public void init_character_ListBox()
        {
            try
            {
                listview_characters.Items.Clear();
                ImageList images = new ImageList();
                images.ImageSize = new Size(24, 24);
                listview_characters.View = View.Details;
                for (int j = 0; j < Characters.Count; j++)
                {
                    String chars = (String)Characters[j];

                    if (File.Exists(Application.StartupPath + "/mmsl_img/icons/" + chars + ".png"))
                    {
                        images.Images.Add(Image.FromFile(Application.StartupPath + "/mmsl_img/icons/" + chars + ".png"));
                    }
                    ListViewItem item = new ListViewItem(chars);
                    item.ImageIndex = j;
                    listview_characters.Items.Add(item);
                    Column4.Items.Add(chars);
                }
                listview_characters.SmallImageList = images;
            }
            catch
            {
                write("Could not init the characters", 0);
            }
            


        }
        //Region selecter
        public void region_select()
        {
            try
            {
                if (!properties.property_check("datafolder"))
                {
                    config cnf = new config();
                    cnf.ShowDialog();
                }

                state_check();
            }
            catch (Exception)
            {

                write("Could not properly make region select", 0);
            }
        }
        #endregion
        #region Messages 
        //Writes string to console
        private void write(String s)
        {
            textConsole.Text = s + "\n" + textConsole.Text;
        }
        //Wirtes a string to the console with a specific status 
        private void write(String s, int code)
        {
            switch (code)
            {
                case 0:
                    write("Error: " + s);
                    break;
                case 1:
                    write("Warning: " + s);
                    break;
                case 2:
                    write("Success: " + s);
                    break;

            }
        }
        //Writes a specific message to the console:
        private void message(String processus, int statuscode, int messagecode)
        {
            switch (processus)
            {
                default:

                    break;
                case "Archiving":

                    break;
                case "Reset":
                    write(messages_reset[statuscode][messagecode], statuscode);
                    break;
            }
        }
        #endregion
        #region Process 
        //State Checker
        private void state_check()
        {
            try
            {
                int character = listview_characters.SelectedIndices.Count > 0 ? listview_characters.SelectedIndices[0] : -1;
                int skin = 0;
                if (listview_skins.SelectedIndices.Count > 0)
                {
                    skin = listview_skins.SelectedIndices[0];
                }
                else
                {
                    skin = -1;
                }

                int model = listview_skin_models.SelectedIndices.Count;
                int csp = listview_skin_csp.SelectedIndices.Count;
                String origin = textbox_skin_origin.Text;

                if (character == -1)
                {
                    //Interactions
                    listview_skin_models.AllowDrop = false;
                    listview_skin_csp.AllowDrop = false;
                    meteorbox.AllowDrop = false;
                    button_skin_save_info.Enabled = false;
                    button1.Enabled = false;

                    //State
                }
                else
                {
                    //Interactions
                    meteorbox.AllowDrop = true;
                    button1.Enabled = true;
                }

                if (skin == -1)
                {
                    //Interactions
                    listview_skin_models.AllowDrop = false;
                    listview_skin_csp.AllowDrop = false;
                    textbox_skin_libraryname.Enabled = false;
                    button_skin_save_info.Enabled = false;
                    button_skin_clean.Enabled = false;
                    button_skin_delete.Enabled = false;
                    button_skin_meta_save.Enabled = false;
                    button_skin_package_meteor.Enabled = false;
                    textbox_skin_meta_author.ReadOnly = true;
                    textbox_skin_meta_version.ReadOnly = true;
                    textbox_skin_meta_name.ReadOnly = true;
                    textbox_skin_meta_texidfix.ReadOnly = true;

                    textbox_skin_slot.Text = "";
                    textbox_skin_origin.Text = "";
                    textbox_skin_libraryname.Text = "";

                    listview_skin_models.Items.Clear();
                    listview_skin_csp.Items.Clear();

                    textbox_skin_meta_author.Text = "";
                    textbox_skin_meta_version.Text = "";
                    textbox_skin_meta_name.Text = "";
                    textbox_skin_meta_texidfix.Text = "";
                    //State
                }
                else
                {
                    //Interactions
                    listview_skin_models.AllowDrop = true;
                    listview_skin_csp.AllowDrop = true;
                    textbox_skin_libraryname.Enabled = true;
                    button_skin_save_info.Enabled = true;
                    button_skin_meta_save.Enabled = true;
                    button_skin_package_meteor.Enabled = true;
                    textbox_skin_meta_author.ReadOnly = false;
                    textbox_skin_meta_version.ReadOnly = false;
                    textbox_skin_meta_name.ReadOnly = false;
                    textbox_skin_meta_texidfix.ReadOnly = false;
                    if (origin == "default")
                    {
                        button_skin_clean.Enabled = true;
                        button_skin_delete.Enabled = true;
                    }
                    else
                    {
                        button_skin_clean.Enabled = true;
                    }

                }
                if (model == 0)
                {
                    button_skin_delete_model.Enabled = false;
                }
                else
                {
                    button_skin_delete_model.Enabled = true;
                }
                if (csp == 0)
                {
                    button_skin_delete_csp.Enabled = false;
                }
                else
                {
                    button_skin_delete_csp.Enabled = true;
                }

            }
            catch (Exception)
            {

                write("Could not check state", 0);
            }


        }
        
        //starts the processing state, disable controls
        private void process_start(String process_text, Boolean bar)
        {
            try
            {
                block_controls();
                loadingbox.Value = 0;
                if (bar)
                {
                    loadingbox.Style = ProgressBarStyle.Continuous;
                }
                else
                {
                    loadingbox.Style = ProgressBarStyle.Marquee;
                }

                label_app_status.Text = process_text;
            }
            catch (Exception)
            {
                write("could not start process", 0);
            }
        }
        //stops the processing state, enables controls
        private void process_stop()
        {
            try
            {
                loadingbox.Value = 100;
                processbox.Value = 100;
                enable_controls();
                processing = false;
            }
            catch (Exception)
            {

                write("could not stop process", 0);
            }
        }
        //Disables controls
        private void block_controls()
        {
            //Menu controls
            adOptionsToolStripMenuItem.Enabled = false;
            tsmi_config_config.Enabled = false;
            tsmi_help.Enabled = false;
            tsmi_skin.Enabled = false;
            tsmi_tools.Enabled = false;
            s4EsWorkspaceToolStripMenuItem.Enabled = false;
            backupToolStripMenuItem.Enabled = false;
            tsmi_workspace_refresh.Enabled = false;

            //Character controls
            listview_characters.Enabled = false;
            listview_skins.Enabled = false;


            //  Skin tab
            textbox_skin_libraryname.Enabled = false;
            textbox_skin_slot.Text = "";
            textbox_skin_origin.Text = "";
            textbox_skin_libraryname.Text = "";

            button_skin_save_info.Enabled = false;
            button_skin_clean.Enabled = false;
            button_skin_delete.Enabled = false;
            button_skin_package_meteor.Enabled = false;

            //  File Manager tab
            listview_skin_models.AllowDrop = false;
            listview_skin_csp.AllowDrop = false;
            listview_skin_models.Items.Clear();
            listview_skin_csp.Items.Clear();

            //  Meta tab
            textbox_skin_meta_author.ReadOnly = true;
            textbox_skin_meta_version.ReadOnly = true;
            textbox_skin_meta_name.ReadOnly = true;
            textbox_skin_meta_texidfix.ReadOnly = true;

            textbox_skin_meta_author.Text = "";
            textbox_skin_meta_version.Text = "";
            textbox_skin_meta_name.Text = "";
            textbox_skin_meta_texidfix.Text = "";

            button_skin_meta_save.Enabled = false;

            //Meteor box
            meteorbox.Enabled = false;

            //Moving controls
            textbox_skin_move_up.Enabled = false;
            textbox_skin_move_down.Enabled = false;

            //Override tab
            textBox_character_ui.Enabled = false;
            button1.Enabled = false;

            //Packer tab
            button_packer_pack.Enabled = false;
            packer_dropzone.Enabled = false;
            packer_skinlist.Enabled = false;
            textbox_packer_author.Enabled = false;
            textbox_packer_version.Enabled = false;
        }
        //Enables controls
        private void enable_controls()
        {
            //Menu Controls
            adOptionsToolStripMenuItem.Enabled = true;
            tsmi_config_config.Enabled = true;
            tsmi_help.Enabled = true;
            tsmi_skin.Enabled = true;
            tsmi_tools.Enabled = true;
            s4EsWorkspaceToolStripMenuItem.Enabled = true;
            backupToolStripMenuItem.Enabled = true;
            tsmi_workspace_refresh.Enabled = true;

            //Character controls
            listview_characters.Enabled = true;

            //Skin Controls
            listview_skins.Enabled = true;
            textbox_skin_move_up.Enabled = true;
            textbox_skin_move_down.Enabled = true;


            //Meteorbox
            meteorbox.Enabled = true;

            //Override tab
            textBox_character_ui.Enabled = true;
            button1.Enabled = true;

            //Packer tab
            button_packer_pack.Enabled = true;
            packer_dropzone.Enabled = true;
            packer_skinlist.Enabled = true;
            textbox_packer_author.Enabled = true;
            textbox_packer_version.Enabled = true;

            //Override tab
            textBox_character_ui.Enabled = true;
        }

        private void process_update()
        {

            try
            {
                double val = process / steps + (100 / steps) * current_step;
                if (val > 100)
                {
                    val = 100;
                }
                loadingbox.Value = Convert.ToInt32(val);
                processbox.Value = process;
                label_process_status.Text = process_text;
            }
            catch (Exception)
            {

                write("Could not update the processing value", 0);
            }
        }

        #endregion

        #region Updates

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
            try
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
            catch (Exception)
            {

                write("Could not check updater version", 0);
            }

        }

        //Updates the updater
        private void update_updater()
        {
            try
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
                                write("Updaterception : Updater updated, please relaunch the app");
                            });

                        webClient.DownloadFileAsync(new Uri("http://mmsl.lunaticfox.com/updater.zip"), Application.StartupPath + "/updater.zip");
                    }
                }
            }
            catch (Exception)
            {

                write("update for the updater failed",0);
            }
        }

        private void proper_update()
        {
          
            try
            {
                //Loading local manifest
                XmlDocument xml2 = new XmlDocument();
                xml2.Load(Application.StartupPath + "/Meteor Skin Library.exe.manifest");
                XmlNode nodes2 = xml2.SelectSingleNode("//*[local-name()='assembly']/*[local-name()='assemblyIdentity']");
                String version2 = nodes2.Attributes[1].Value;
                String local_version = version2.Replace('.', '_');

                //Searching for last update
                String last_version = search_update();

                //Loading remote manifest
                XmlDocument xml = new XmlDocument();
                xml.Load("http://lunaticfox.com/MSL/Application Files/Meteor Skin Library_" + last_version + "/Meteor Skin Library.exe.manifest");
                XmlNode nodes = xml.SelectSingleNode("//*[local-name()='assembly']/*[local-name()='assemblyIdentity']");
                String version = nodes.Attributes[1].Value;
                String remote_version = version.Replace('.', '_');

                if (new_version(local_version, remote_version))
                {
                    update();
                }
            }
            catch
            {
                write("The update couldn't be checked", 0);
            }
            

            

            
        }

        private string search_update()
        {
            try
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
            catch (Exception)
            {
                write("the update search failed", 0);
                return "";
            }
        }

        private Boolean tryxml(String full_dest)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(full_dest);
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        //Tells if the remoteversion is newer
        private Boolean new_version(String localversion, String remoteversion)
        {
            try
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
                if (r_major > l_major)
                {
                    return true;
                }
                else
                {
                    if (r_minor > l_minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (r_build > l_build)
                        {
                            return true;
                        }
                        else
                        {
                            if (r_revision > l_revision)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                write("could not compare versions", 0);
                return false;
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
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("- An error happened during the installation");
                    write("Installation Status:");
                    write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
                case 1:
                    label_app_status.Text = "Skins installed";
                    label_process_status.Text = "Success";
                    write("----------------------------------------------------------------------------------------------------");
                    write("- All the skins where installed");
                    write("Installation Status:");
                    write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
                case 2:
                    label_app_status.Text = "No skins found";
                    write("----------------------------------------------------------------------------------------------------");
                    write("- Please check that the meteor generated folders ( like \"Mario\" ) are at the root (or base folder) of the archive");
                    write("Installation Status: No meteor skins found");
                    write("----------------------------------------------------------------------------------------------------");
                    meteorcode = 0;
                    break;
            }
        }
        private void exportstatus()
        {
            switch (exportcode)
            {
                case 1:
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("Directory error: \n Could not delete the fighter/[name]/model folders. Please remove them manually and retry.");
                    write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 2:
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("Directory error: \n Could not delete the data/ui/replace/chr folder. Please remove it manually and retry.");
                    write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
                case 3:
                    label_app_status.Text = "Export Success";
                    label_process_status.Text = "Success";
                    write("----------------------------------------------------------------------------------------------------");
                    write("The export was successful");
                    write("----------------------------------------------------------------------------------------------------");
                    exportcode = 0;
                    break;
            }
        }
        private void importstatus()
        {
            switch (importcode)
            {
                case 1:
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("Directory error: \n Could not delete the fighter/[name]/model folders. Please remove them manually and retry.");
                    write("----------------------------------------------------------------------------------------------------");
                    importcode = 0;
                    break;
                case 2:
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("Directory error: \n Could not delete the data/ui/replace/chr folder. Please remove it manually and retry.");
                    write("----------------------------------------------------------------------------------------------------");
                    importcode = 0;
                    break;
                case 3:
                    label_app_status.Text = "Success";
                    label_process_status.Text = "Operation complete";
                    write("----------------------------------------------------------------------------------------------------");
                    switch (workspace_select)
                    {
                        case 0:
                            write("MSL's library and workspace were successfully replaced.");
                            break;
                        case 1:
                            write("MSL's library was refreshed and updated with missing files found in S4E.");
                            break;
                        case 2:
                            write("MSL's library was refreshed and updated with missing files found in it's workspace.");
                            break;
                    }

                    write("----------------------------------------------------------------------------------------------------");
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
                    label_app_status.Text = "Error";
                    write("----------------------------------------------------------------------------------------------------");
                    write("Download error: \n The meteor link is invalid");
                    write("----------------------------------------------------------------------------------------------------");
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
                    label_app_status.Text = "Downloading";
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
                                        label_app_status.Text = "Importing Meteor Skins";
                                        label_process_status.Text = "Importing Meteor Skins";
                                        meteor_worker.RunWorkerAsync();
                                    }
                                    catch (Exception e3)
                                    {
                                        label_app_status.Text = "Error";
                                        write("----------------------------------------------------------------------------------------------------");
                                        write("Download error: \n An error has appened during the extraction of the archive");
                                        write("----------------------------------------------------------------------------------------------------");
                                    }

                                }
                                catch (Exception e2)
                                {
                                    label_app_status.Text = "Error";
                                    write("----------------------------------------------------------------------------------------------------");
                                    write("Download error: \n The previous archive couldn't be deleted");
                                    write("----------------------------------------------------------------------------------------------------");
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
                                        label_app_status.Text = "Importing Meteor Skins";
                                        meteor_worker.RunWorkerAsync();
                                    }
                                    catch (Exception e3)
                                    {
                                        label_app_status.Text = "Error";
                                        write("----------------------------------------------------------------------------------------------------");
                                        write("Archive error: \n An error has appened during the extraction of the archive");
                                        write("----------------------------------------------------------------------------------------------------");
                                    }
                                }
                                catch (Exception e2)
                                {
                                    label_app_status.Text = "Error";
                                    write("----------------------------------------------------------------------------------------------------");
                                    write("Download error: \n The previous archive couldn't be deleted");
                                    write("----------------------------------------------------------------------------------------------------");
                                }
                            }
                        }
                        else
                        {
                            label_app_status.Text = "Error";
                            write("----------------------------------------------------------------------------------------------------");
                            write("Download error: \n Either the ressource is missing or the download process failed.");
                            write("----------------------------------------------------------------------------------------------------");
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
                            label_app_status.Text = "Error";
                            write("----------------------------------------------------------------------------------------------------");
                            write("Download error: \n The meteor link is invalid");
                            write("----------------------------------------------------------------------------------------------------");
                        }
                    }
                    else
                    {
                        label_app_status.Text = "Error";
                        write("----------------------------------------------------------------------------------------------------");
                        write("Download error: \n The archive is in an unsupported format");
                        write("----------------------------------------------------------------------------------------------------");
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
                    write("File Detected :" + Path.GetFileName(file));
                    selected_skin.add_csp(file);
                }
                write("All detected CSP were moved to slot " + selected_skin.slot);
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
                    write("Slot Detected : " + Path.GetFileName(file));
                    String skin_name = Path.GetFileName(file).Split('_')[2];
                    if (skin_name == "")
                    {
                        skin_name = "empty";
                    }

                    int skin_slot = listview_skins.Items.Count + 1;

                    Skin meteor_skin = new Skin(listview_characters.SelectedItems[0].Text, listview_skins.Items.Count + 1, skin_name, "Custom", Library, properties, logg);

                    //Model files check
                    if (Directory.Exists(file + "/model"))
                    {
                        write("Slot model folder detected");
                        batch_copy_model(Directory.GetDirectories(file + "/model"), meteor_skin);
                    }
                    else
                    {
                        write("Slot model folder missing");
                    }
                    //CSP Files check
                    if (Directory.Exists(file + "/csp/"))
                    {
                        write("Slot csp folder detected");
                        String[] folder = new string[] { file + "/csp/" };
                        batch_copy_csp(folder, meteor_skin);
                    }
                    else
                    {
                        write("Slot csp folder missing");
                    }
                    if (Directory.Exists(file + "/meta"))
                    {
                        write("meta folder detected");
                        meteor_skin.addMeta(file + "/meta/meta.xml");
                    }

                }
            }
            skin_ListBox_reload();
            listview_skins.FocusedItem = listview_skins.Items[listview_skins.Items.Count - 1];
            listview_skins.Items[listview_skins.Items.Count - 1].Selected = true;
            listview_skins.Select();
            listview_skins.Items[listview_skins.Items.Count - 1].EnsureVisible();
            uichar.setFile(int.Parse(Library.get_ui_char_db_id(listview_characters.SelectedItems[0].Text)), 7, listview_skins.Items.Count);
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
            String se_mmsl_workspace_path = properties.property_get("explorer_workspace");
            String se_model_path = se_mmsl_workspace_path + "/content/patch/data/fighter/";
            String se_csp_path = se_mmsl_workspace_path + "/content/patch/data/ui/replace/chr/";
            String datafolder = properties.property_get("datafolder");
            String se_csp_path_dlc = se_mmsl_workspace_path + "/content/patch/" + datafolder + "/ui/replace/append/chr/";

            String slot_model = (listview_skins.Items.Count + 1).ToString();



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
                }
                else
                {
                    write("fighter folder not found in S4E's workspace");
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


                    }
                    else
                    {
                        if (z == 0)
                        {
                            write("ui/replace/chr folder not found");
                        }
                        else
                        {
                            write("ui/replace/append/chr folder not found");
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
                String destination = properties.property_get("explorer_workspace") + "/content/patch/data";
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
                                foreach (String file in Directory.GetFiles(destination + "/fighter/" + Library.get_modelfolder_fullname(c) + "/model", "*", SearchOption.AllDirectories))
                                {
                                    FileInfo fInfo = new FileInfo(file);
                                    if (fInfo.IsReadOnly)
                                    {
                                        fInfo.IsReadOnly = false;
                                    }
                                }

                                Directory.Delete(destination + "/fighter/" + Library.get_modelfolder_fullname(c) + "/model", true);
                            }
                        }

                        try
                        {
                            if (Directory.Exists(destination + "/ui/replace/chr"))
                            {
                                foreach(String file in Directory.GetFiles(destination + "/ui/replace/chr", "*", SearchOption.AllDirectories))
                                {
                                    FileInfo fInfo = new FileInfo(file);
                                    if (fInfo.IsReadOnly)
                                    {
                                        fInfo.IsReadOnly = false;
                                    }
                                }
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
                                float val = (current / count) * 100;
                                process = Convert.ToInt32(Math.Truncate(val));
                                double val2 = process / steps + (100 / steps) * current_step;
                                status = Convert.ToInt32(val2 > 100 ? 100 : val2);
                                export_worker.ReportProgress(process);
                                current++;
                            }

                            current_step = 1;
                            process_text = "Copying localised Data folder";
                            if (properties.property_get("datafolder") != "data")
                            {
                                if (properties.property_get("unlocalised") == "1")
                                {
                                    source = Application.StartupPath + "/mmsl_workspace/data";
                                }
                                else
                                {
                                    source = Application.StartupPath + "/mmsl_workspace/" + properties.property_get("datafolder");
                                }


                                destination = properties.property_get("explorer_workspace") + "/content/patch/" + properties.property_get("datafolder");
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
                                if (properties.property_get("datafolder") == "data")
                                {
                                    if (properties.property_get("unlocalised") == "0")
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
                                    if (properties.property_get("unlocalised") == "0")
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/" + properties.property_get("datafolder") + "/param/ui/ui_character_db.bin";
                                    }
                                    else
                                    {
                                        source = Application.StartupPath + "/mmsl_workspace/data/param/ui/ui_character_db.bin";
                                    }
                                }

                                if (properties.property_get("datafolder") == "data")
                                {
                                    if (properties.property_get("unlocalised") == "0")
                                    {
                                        destination = properties.property_get("explorer_workspace") + "/content/patch/data(us_en)";
                                    }
                                    else
                                    {
                                        destination = properties.property_get("explorer_workspace") + "/content/patch/data";
                                    }
                                }
                                else
                                {
                                    if (properties.property_get("unlocalised") == "0")
                                    {
                                        destination = properties.property_get("explorer_workspace") + "/content/patch/" + properties.property_get("datafolder");
                                    }
                                    else
                                    {
                                        destination = properties.property_get("explorer_workspace") + "/content/patch/data";
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
                    catch (Exception e)
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

        private void backup_folder(String source, String destination)
        {

            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }

            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(source, destination));
            current_step = 0;
            process_text = "Backuping folder";
            //Copy all the files & Replaces any files with the same name
            float count = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Length;

            float current = 1;
            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(source, destination), true);
                float val = (current / count) * 100;
                process = Convert.ToInt32(Math.Truncate(val));
                double val2 = process / steps + (100 / steps) * current_step;
                status = Convert.ToInt32(val2 > 100 ? 100 : val2);
                backup_worker.ReportProgress(process);
                current++;
            }


        }
        #endregion

        #region Workers
        //Launches import
        private void workspace_worker_work(object sender, DoWorkEventArgs e)
        {
            if (workspace_select == 0)
            {
                update_files(workspace_select, properties.property_get("explorer_workspace"));
            }
            if (workspace_select == 1)
            {
                update_files(workspace_select, properties.property_get("explorer_workspace"));
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
            label_process_status.Text = process_text;
        }
        //Reports completion of import
        private void workspace_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            process_stop();
            if (listview_characters.SelectedIndices.Count > 0)
            {
                // Selects the character the last skin was added for
                focus_character(workspace_char);
            }
            importstatus();
        }

        //Launches Export
        private void export_worker_work(object sender, DoWorkEventArgs e)
        {
            batch_export_SE();
        }
        //Reports export progress
        private void export_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            process_update();
        }
        //Reports completion of export
        private void export_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            exportstatus();
            process_stop();
            if (MessageBox.Show("Export Finished, do you want to launch Sm4sh Explorer?", " Segtendo WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                tool_launch_s4e();
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
            label_process_status.Text = process_text;

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
            process_stop();

            //Checks status about the import
            meteorstatus();

            //Selects the character the last skin was added for
            focus_character(last_char);
            try
            {
                //Selects the last skin
                focus_skin(listview_skins.Items.Count - 1);
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
            write("Archive Created in mmsl_packages");
            label_app_status.Text = "Archive complete";
            processing = false;
            packer_reset();
            enable_controls();
            System.Diagnostics.Process.Start(Application.StartupPath + "/mmsl_packages/");
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
            label_process_status.Text = process_text;
        }
        //extract worker complete
        private void refresh_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            process_stop();
            if (listview_characters.SelectedIndices.Count > 0)
            {
                // Selects the character the last skin was added for
                focus_character(workspace_char);
            }
            
            label_app_status.Text = "Success";
            write("The files were checked and their status updated",2);
        }

        //Launches manual worker
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            manual_meteors.Clear();
            //Getting csp files
            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_([0-9]{2}|xx|[0-9]x|x[0-9]).nut$");
            String[] files = Directory.GetFiles(this.manualfolder[0], "*.nut", SearchOption.AllDirectories);
            ArrayList manual_csps = new ArrayList();
            foreach (String file in files)
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
                if (dirname.Length == 3)
                {
                    int slot;
                    //If slot can be parsed
                    if (int.TryParse(dirname.Substring(1, 2), out slot))
                    {
                        manual_models.Add(dir);
                    }
                }
            }

            //Moving the files to the appropriate location
            foreach (String csp in manual_csps)
            {
                String csp_parsed_slot = csp.Split('_')[csp.Split('_').Length - 1].Split('.')[0];
                int slot;
                if (int.TryParse(csp_parsed_slot, out slot))
                {
                    String destination = Application.StartupPath + "/mmsl_packages/unselected/meteor_xx_slot_" + slot + "/csp/";
                    if (!Directory.Exists(destination))
                    {
                        Directory.CreateDirectory(destination);
                    }
                    String destination_file = destination + Path.GetFileName(csp);
                    File.Copy(csp, destination_file, true);
                }
            }
            //Copying models
            foreach (String model in manual_models)
            {
                String model_parsed_slot = Path.GetFileName(model).Substring(1, 2);
                int slot;
                if (int.TryParse(model_parsed_slot, out slot))
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
                    foreach (String file in Directory.GetFiles(model))
                    {
                        File.Copy(file, model_destination + "/" + Path.GetFileName(file), true);
                    }
                }
            }
            String filelist = "";
            //Listing new meteor folders
            String[] meteors = Directory.GetDirectories(Application.StartupPath + "/mmsl_packages/unselected/");
            if (meteors.Length > 0)
            {
                foreach (String meteor in meteors)
                {
                    String slot = meteor.Split('_')[meteor.Split('_').Length - 1];
                    String name = meteor.Split('_')[3];
                    foreach (String csp in Directory.GetFiles(meteor + "/csp"))
                    {
                        String csp_name = Path.GetFileName(csp).Split('_')[0] + "_" + Path.GetFileName(csp).Split('_')[1];
                        filelist += csp_name + " | ";
                    }
                    manual_meteors.Add(slot + ";unselected;slot_" + slot + "; " + filelist);
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
            foreach (String val in manual_meteors)
            {
                packer_skinlist.Rows.Add(val.Split(';'));
            }
            started_pack = true;
        }

        //Launches a backup process
        private void backup_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if(backup_select != 0)
            {
                switch (backup_select)
                {
                    case 1:
                        Boolean test = false;
                        int i = 1;
                        String destination = "";
                        while (!test)
                        {
                            destination = Application.StartupPath + "/mmsl_backups/msl_workspace_" + i;
                            if (!Directory.Exists(destination))
                            {
                                test = true;
                            }
                            i++;
                        }

                        backup_folder(Application.StartupPath + "/mmsl_workspace", destination);
                        File.Copy(Application.StartupPath + "/mmsl_config/Library.xml", destination + "/Library.xml");
                        break;
                    case 2:
                        Boolean test2 = false;
                        int i2 = 1;
                        String destination2 = "";
                        while (!test2)
                        {
                            destination2 = Application.StartupPath + "/mmsl_backups/S4E_workspace_" + i2;
                            if (!Directory.Exists(destination2))
                            {
                                test2 = true;
                            }
                            i2++;
                        }

                        backup_folder(properties.property_get("explorer_workspace"), destination2);
                        
                        break;
                }
            }
        }

        //When the backup progress continues
        private void backup_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double val = process / steps + (100 / steps) * current_step;
            if (val > 100)
            {
                val = 100;
            }
            loadingbox.Value = Convert.ToInt32(val);
            processbox.Value = process;
            label_process_status.Text = process_text;

        }

        //When the backup progress finishes
        private void backup_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            process_stop();
            label_app_status.Text = "Success";
            write("The backup process was successful", 2);
        }
        #endregion

        #region Workspace
        //Grabs files for the library
        private void update_files(int mode, String update_path)
        {
            logg.log("-- starting the update process");
            //Setting base paths
            String datafolder = properties.property_get("datafolder");
            String source_model_path = "";
            String source_csp_path = "";
            String source_csp_dlc_path = "";

            if (mode == 2)
            {
                logg.log("Refreshing workspace");
                source_model_path = update_path + "/mmsl_workspace/data/fighter/";
                source_csp_path = update_path + "/mmsl_workspace/data/ui/replace/chr/";
                if (properties.property_get("unlocalised") == "1")
                {
                    source_csp_dlc_path = update_path + "/mmsl_workspace/data/ui/replace/append/chr/";
                }
                else
                {
                    source_csp_dlc_path = update_path + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
                }

            }
            else
            {
                logg.log("S4E import/update");
                source_model_path = update_path + "/content/patch/data/fighter/";
                source_csp_path = update_path + "/content/patch/data/ui/replace/chr/";
                logg.log("source csp path is :" + source_csp_path);
                source_csp_dlc_path = "";

                if (properties.property_get("unlocalised") == "1")
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


            if (properties.property_get("unlocalised") == "1")
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
            if (mode != 0)
            {
                steps = 4;
                process_text = "Checking differences";
                logg.log("-- Checking difference");
                double current = 1;
                foreach (String character in Characters)
                {
                    logg.log("Checking character: " + character);
                    selected_char = new Character(character, Library, properties, uichar, logg);
                    selected_char.check_all_files();
                    logg.log("Files checked");
                    double val = current / Convert.ToDouble(Characters.Count) * 100;
                    this.process = Convert.ToInt32(Math.Truncate(val));
                    import_worker.ReportProgress(Convert.ToInt32(Math.Truncate(val)));
                    current++;

                }
                current_step++;

            }
            else
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
            logg.log("source: " + source);
            logg.log("destination: " + destination);
            //Checking source existence
            if (Directory.Exists(source))
            {
                logg.log("model source exists :" + source);
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
                        foreach (String c in Characters)
                        {
                            try
                            {
                                if (c == Library.get_fullname_modelfolder(Path.GetFileName(character_folder)))
                                {
                                    test = true;
                                    logg.log("exists");
                                }
                            }
                            catch (Exception e)
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
                            logg.log("model folder:" + character_model_list);
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
        private void update_model(String source, String destination, String character, int mode)
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

                    if (int.TryParse(Path.GetFileName(source).Substring(1, 2), out slot))
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
                        new Skin(Library.get_fullname_modelfolder(Path.GetFileName(character)), slot + 1, "Imported skin", "Sm4sh Explorer", Library, properties, logg).add_model(source, Directory.GetParent(source).Name);

                    }
                    break;
                //Add missing files from S4E
                case 1:
                    logg.log("-- Update model");
                    //Parsing destination slot
                    int slot2;
                    if (int.TryParse(Path.GetFileName(source).Substring(1, 2), out slot2))
                    {
                        logg.log("slot parsed " + slot2);
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
                    if (int.TryParse(Path.GetFileName(source).Substring(1, 2), out slot3))
                    {
                        logg.log("slot parsed " + slot3);
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
                    logg.log("- current cspformat: " + cspformat);
                    double val = currentcount / Convert.ToDouble(csps.Length) * 100;
                    this.process = Convert.ToInt32(Math.Truncate(val));
                    import_worker.ReportProgress(process);
                    currentcount++;
                    String[] files = Directory.GetFiles(cspformat);
                    //check if cspformat contains files
                    if (files.Length > 0)
                    {
                        //foreach file
                        foreach (String csp in files)
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
                selected_char = new Character(character, Library, properties, uichar, logg);
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
