using MeteorSkinLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Meteor_Skin_Library.Items
{
    public class NewSkin
    {
        #region Class Variables
        //Extract paths
        String extract_model_folder;
        String extract_csp_folder;
        String extract_dlc_csp_folder;
        String extract_sound_folder;
        String extract_nameplate_folder;
        String extract_meta_folder;

        //Filebank Path
        public String filebank_folder;

        //Skin Info
        public String fullname;
        public String libraryname;
        public String model_foldername;
        public String csp_foldername;
        public int id;
        public Boolean dlc;

        //Slot info
        public int cspslot;
        public int modelslot;
        public String cspslot_text;
        public String modelslot_text;

        //Localisation info
        String dlc_datafolder;

        #endregion

        #region Handlers
        LibraryHandler library;
        PropertyHandler config;
        Filebank filebank;
        Logger logger;
        #endregion

        #region Constructors

        //New Skin Constructor

            //Constructor that creates info for storage
        public NewSkin(int id, String fullname, int cspslot, String libraryname, LibraryHandler global_library, PropertyHandler global_properties, Filebank golabl_filebank)
        {
            #region Handlers
            this.library = global_library;
            this.config = global_properties;
            this.filebank = golabl_filebank;
            #endregion

            #region SkinInfo

            #region Basic info
            this.fullname = fullname;
            this.libraryname = libraryname;
            this.csp_foldername = library.get_cspfolder_fullname(this.fullname);
            this.model_foldername = library.get_modelfolder_fullname(this.fullname);

            this.dlc = library.get_dlc_status(this.fullname);

            #endregion

            #region localisation settings
            if (config.property_get("unlocalised") == "1")
            {
                this.dlc_datafolder = "data";
            }
            else
            {
                this.dlc_datafolder = config.property_get("datafolder");
            }
            #endregion

            #region ID
            if (id == -1)
            {
                this.id = generate_id();
            }
            else
            {
                this.id = id;
            }
            #endregion

            #region Slots
            //Setting slots
            this.cspslot = cspslot;
            this.modelslot = cspslot - 1;

            //Setting slot texts
            this.modelslot_text = (this.modelslot < 10 ? "0" + this.modelslot.ToString() : this.modelslot.ToString());
            this.cspslot_text = (this.cspslot < 10 ? "0" + this.cspslot.ToString() : this.cspslot.ToString());

            #endregion

            #endregion

            #region Library Folders
            //Filebank folders
            this.filebank_folder = Application.StartupPath + "/mmsl_filebank/Skins/" + this.fullname + "/meteor_" + this.id + "_" + this.libraryname;

            //Extract folders
            this.extract_model_folder = Application.StartupPath + "/mmsl_workspace/data/fighter/" + this.model_foldername + "/model";
            this.extract_csp_folder = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
            this.extract_dlc_csp_folder = Application.StartupPath + "/mmsl_workspace/" + this.dlc_datafolder + "/ui/replace/append/chr/";
            this.extract_meta_folder = Application.StartupPath + "/mmsl_config/meta/" + model_foldername + "/slot_" + cspslot;

            #endregion

        }

            //Constructor that gets info from filebank
        public NewSkin(int id, String fullname, int cspslot, LibraryHandler global_library, PropertyHandler global_properties, Filebank golabl_filebank)
        {
            #region Handlers
            this.library = global_library;
            this.config = global_properties;
            this.filebank = golabl_filebank;
            #endregion

            #region SkinInfo

            #region Basic info
            this.fullname = fullname;
            this.libraryname = filebank.get_skin_info(id,fullname)[0].Split(';')[0];
            this.csp_foldername = library.get_cspfolder_fullname(this.fullname);
            this.model_foldername = library.get_modelfolder_fullname(this.fullname);

            this.dlc = library.get_dlc_status(this.fullname);

            #endregion

            #region localisation settings
            if (config.property_get("unlocalised") == "1")
            {
                this.dlc_datafolder = "data";
            }
            else
            {
                this.dlc_datafolder = config.property_get("datafolder");
            }
            #endregion

            #region ID
            if (id == -1)
            {
                this.id = generate_id();
            }
            else
            {
                this.id = id;
            }
            #endregion

            #region Slots
            //Setting slots
            this.cspslot = cspslot;
            this.modelslot = cspslot -1;

            //Setting slot texts
            this.modelslot_text = (this.modelslot < 10 ? "0" + this.modelslot.ToString() : this.modelslot.ToString());
            this.cspslot_text = (this.cspslot < 10 ? "0" + this.cspslot.ToString() : this.cspslot.ToString());

            #endregion

            #endregion

            #region Library Folders
            //Filebank folders
            this.filebank_folder = Application.StartupPath + "/mmsl_filebank/Skins/" + this.fullname + "/meteor_" + this.id + "_" + this.libraryname;

            //Extract folders
            this.extract_model_folder = Application.StartupPath + "/mmsl_workspace/data/fighter/" + this.model_foldername + "/model";
            this.extract_csp_folder = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
            this.extract_dlc_csp_folder = Application.StartupPath + "/mmsl_workspace/" + this.dlc_datafolder + "/ui/replace/append/chr/";
            this.extract_meta_folder = Application.StartupPath + "/mmsl_config/meta/" + model_foldername + "/slot_" + cspslot;

            #endregion

        }

        #endregion

        #region File Interactions
        //Gets the files from the workspace and puts the files into the filebank
        public void get_workspace_skin()
        {
            #region Variables
            String csp_list = "";
            String model_list = "";
            #endregion

            #region Directories
            //Creating destination directories
            Directory.CreateDirectory(filebank_folder + "/meta");
            Directory.CreateDirectory(filebank_folder + "/csp");
            Directory.CreateDirectory(filebank_folder + "/model");
            Directory.CreateDirectory(filebank_folder + "/sounds");
            Directory.CreateDirectory(filebank_folder + "/nameplate");
            #endregion

            #region Model getter
            ArrayList models = library.get_models(fullname, cspslot);

            foreach (String model in models)
            {
                model_list += model+";";
                String folder_path = extract_model_folder +"/"+ model.Split('/')[0] + "/" + model.Split('/')[1].Substring(0, 1) + modelslot_text;

                if(!Directory.Exists(filebank_folder + "/model/" + model.Split('/')[0] + "/" + model.Split('/')[1]))
                {
                    Directory.CreateDirectory(filebank_folder + "/model/" + model.Split('/')[0] + "/" + model.Split('/')[1]);
                }
                

                if (Directory.GetFiles(folder_path).Length > 0)
                {
                    foreach (String file in Directory.GetFiles(folder_path))
                    {
                        File.Copy(file, filebank_folder + "/model/" + model.Split('/')[0] + "/" + model.Split('/')[1] + "/" + Path.GetFileName(file),true);
                    }
                }
            }
            #endregion

            #region Csp Getter
            ArrayList csps = library.get_csps(fullname, cspslot);

            foreach (String csp in csps)
            {
                
                String FilePath = "";

                if (dlc)
                {
                    FilePath = extract_dlc_csp_folder;
                }
                else
                {
                    FilePath = extract_csp_folder;
                }
                FilePath += csp + "/" + csp + "_" + csp_foldername + "_" + cspslot_text + ".nut";
                if (File.Exists(FilePath))
                {
                    if(csp == "chrn_11" | csp == "chr_10")
                    {
                        if(csp == "chrn_11")
                        {
                            File.Copy(FilePath, filebank_folder + "/nameplate/" + Path.GetFileName(FilePath));
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        File.Copy(FilePath, filebank_folder + "/csp/" + Path.GetFileName(FilePath));
                        csp_list += csp + ";";
                    }
                }
            }
            #endregion

            #region Meta
           if(File.Exists(extract_meta_folder + "/meta.xml"))
            {
                File.Copy(extract_meta_folder + "/meta.xml", filebank_folder + "/meta/meta.xml", true);
            }
            else
            {
                File.Copy(Application.StartupPath+"/mmsl_config/meta/Default_Meta.xml", filebank_folder + "/meta/meta.xml", true);
            }
                
            
            
            #endregion

            #region EmptyCheck
            if(Directory.GetFiles(filebank_folder + "/csp").Length == 0
                && Directory.GetFiles(filebank_folder + "/model").Length == 0 
                && Directory.GetFiles(filebank_folder + "/nameplate").Length == 0
                && Directory.GetFiles(filebank_folder + "/sounds").Length == 0)
            {
                Directory.Delete(filebank_folder, true);
                this.id = 0;
            }else
            {
                filebank.add_skin(this, model_list, csp_list);
                
            }

            #endregion

            library.set_id(this);
        }

        public void get_meteor_skin(String folder)
        {
            #region Variables
            String csp_list = "";
            String model_list = "";
            #endregion

            #region Directories
            //Creating destination directories
            Directory.CreateDirectory(filebank_folder + "/meta");
            Directory.CreateDirectory(filebank_folder + "/csp");
            Directory.CreateDirectory(filebank_folder + "/model");
            Directory.CreateDirectory(filebank_folder + "/sounds");
            Directory.CreateDirectory(filebank_folder + "/nameplate");
            #endregion

            #region Model getter
            //Get the model files
            

            //Getting model folders
            String[] dirs = Directory.GetDirectories(folder + "/model", "*", SearchOption.AllDirectories);
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
                        model_list += Path.GetFileName(Directory.GetParent(dir).FullName) + "/" + dirname.Substring(0, 1) + "XX"+";";
                    }else
                    {
                        if (dirname == "cXX" || dirname == "lXX")
                        {
                            model_list += Path.GetFileName(Directory.GetParent(dir).FullName) + "/" + dirname+";";
                        }
                    }
                }
            }

            
            
            #endregion

            #region Csp Getter
            //Getting csp files
            Regex cspr = new Regex("^((?:chrn|chr|stock)_[0-9][0-9])_([a-zA-Z]+)_([0-9]{2}|xx|[0-9]x|x[0-9]).nut$");
            String[] files = Directory.GetFiles(folder + "/csp", "*.nut", SearchOption.AllDirectories);
            foreach (String file in files)
            {
                if (cspr.IsMatch(Path.GetFileName(file)))
                {
                    csp_list += Path.GetFileName(file).Split('_')[0] + "_" + Path.GetFileName(file).Split('_')[1]+";";
                }
            }

            #endregion

            #region Meta
            if (File.Exists(folder + "/meta/meta.xml"))
            {
                File.Copy(folder + "/meta/meta.xml", filebank_folder + "/meta/meta.xml", true);
            }
            else
            {
                File.Copy(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml", filebank_folder + "/meta/meta.xml", true);
            }

            filebank.add_skin(this, model_list, csp_list);

            #endregion
        }

        //Puts the skin in the desired positon in the workspace
        public void extract(String fullname,int position)
        {

            library.set_changed_status(fullname, cspslot, "build");
        }

        public void copy_package()
        {
            String package_destination = Application.StartupPath + "/mmsl_packages/" + fullname + "/meteor_xx_" + libraryname + "/";

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(filebank_folder, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(filebank_folder, package_destination));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(filebank_folder, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(filebank_folder, package_destination), true);
        }

        #region SkinFiles
        public void add_model(String model_directory, String model_parent)
        {

            #region Varsetup
            String destination = "";
            String library_name = "";
            Regex cXX = new Regex("^[c]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
            Regex lXX = new Regex("^[l]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
            #endregion

            if (cXX.IsMatch(Path.GetFileName(model_directory)))
            {
                #region Foldersetup
                destination = filebank_folder + "/model/" + model_parent + "/cXX";
                library_name = model_parent + "/cXX";
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                else
                {
                    Directory.Delete(destination, true);
                    Directory.CreateDirectory(destination);
                }


                #endregion
            }

            if (lXX.IsMatch(Path.GetFileName(model_directory)))
            {
                #region Foldersetup
                destination = filebank_folder + "/model/" + model_parent + "/lXX";
                library_name = model_parent + "/lXX";
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }
                else
                {
                    Directory.Delete(destination, true);
                    Directory.CreateDirectory(destination);
                }
                #endregion
            }

            #region Copy
            foreach (String file in Directory.GetFiles(model_directory))
            {
                File.Copy(file, destination+"/" + Path.GetFileName(file));
            }
            #endregion

            #region Libraries
            filebank.add_skin_model(fullname, id, library_name);
            library.set_changed_status(fullname, cspslot, "changed");
            #endregion


        }
        public void add_csp(String csp_file)
        {
            String csp_name = Path.GetFileName(csp_file).Split('_')[0] + "_" + Path.GetFileName(csp_file).Split('_')[1];
            String destination = filebank_folder + "/csp/";
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }
            if (File.Exists(csp_file))
            {
                File.Copy(csp_file, destination + Path.GetFileName(csp_file), true);
            }

            filebank.add_skin_csp(fullname, id, csp_name);
            library.set_changed_status(fullname, cspslot, "changed");
        }
        public void delete_model(String model_entry)
        {
           String destination = filebank_folder + "/model/" +model_entry;
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination,true);
                filebank.delete_skin_model(fullname,id,model_entry);
            }else
            {
                filebank.delete_skin_model(fullname, id, model_entry);
            }
            library.set_changed_status(fullname, cspslot, "changed");
        }
        public void delete_csp(String csp_entry)
        {
            String destination = filebank_folder + "/csp/";
            String[] files = Directory.GetFiles(destination, csp_entry + "*");
            if (files.Length > 0)
            {
                if (File.Exists(files[0]))
                {
                    File.Delete(files[0]);
                }
            }
            filebank.delete_skin_csp(fullname, id, csp_entry);

            library.set_changed_status(fullname, cspslot, "changed");
        }

        #endregion


        #endregion

        #region Informations

        #region Skin info
        public void set_libraryname(String libraryname)
        {
            filebank.set_skin_name(fullname, id, libraryname);
            library.set_libraryname(fullname, cspslot, libraryname);
            if (Directory.Exists(filebank_folder))
            {
                String destination = Application.StartupPath + "/mmsl_filebank/Skins/" + this.fullname + "/meteor_" + this.id + "_" + libraryname;
                Directory.Move(filebank_folder, destination);
                this.filebank_folder = destination;
            }

            library.set_changed_status(fullname, cspslot, "changed");

        }

        public String[] get_meteor_info()
        {
            String files = "";
            String[] skin_info = filebank.get_skin_info(id, fullname);
            files += skin_info[1].Replace(';', '|');
            files += skin_info[2].Replace(';', '|');
            String[] values = new String[] { "xx", fullname, libraryname, files };

            return values;
        }

        public void save_skin_filebank()
        {
            filebank.add_skin(this);
        }
        #endregion

        #region ID Code
        public Boolean check_id(int id, String fullname)
        {
            return filebank.check_skin_id(id, fullname);
        }

        public int generate_id()
        {
            Random rnd = new Random();
            int id = 1;
            while (check_id(id, this.fullname))
            {
                id++;
            }
            return id;
        }
        #endregion

        #region Meta
        public String get_skin_meta()
        {
            String meta_text = "";
            if (File.Exists(filebank_folder + "/meta/meta.xml"))
            {

                MetaHandler Meta = new MetaHandler(filebank_folder + "/meta/meta.xml");

                //Setting metas
                meta_text += Meta.get("author") + ";";
                meta_text += Meta.get("version") + ";";
                meta_text += Meta.get("name") + ";";
                meta_text += Meta.get("texidfix");
            }
            return meta_text;
        }
        public void set_skin_meta(String author, String version, String name, String texidfix)
        {
            if(!File.Exists(filebank_folder + "/meta/meta.xml"))
            {
                if(!Directory.Exists(filebank_folder + "/meta/"))
                {
                    Directory.CreateDirectory(filebank_folder + "/meta/");
                }
                File.Copy("mmsl_config/meta/Default_Meta.xml", filebank_folder + "/meta/meta.xml", true);
            }

            MetaHandler Meta = new MetaHandler(filebank_folder + "/meta/meta.xml");
            Meta.set("author", author);
            Meta.set("version", version);
            Meta.set("name", name);
            Meta.set("texidfix", texidfix);

            library.set_changed_status(fullname, cspslot, "changed");
        }
        #endregion

        #endregion

    }

}
