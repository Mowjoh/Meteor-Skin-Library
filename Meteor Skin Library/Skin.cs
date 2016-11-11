using Meteor_Skin_Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeteorSkinLibrary
{
    class Skin
    {

        #region ClassVariables

        //Configuration informations 
        public String fullname;
        public String modelfolder;
        public String cspfolder;
        public int modelslot;
        public int slot;
        public String slotstring;
        public String modelslotstring;
        public Boolean dlc;
        public String datafolder;
        public Boolean missing;
        public Boolean unknown;
        public Boolean new_files;

        //Skin information
        public String libraryname;
        public String origin;


        //Metadata information or do that in an array so it's simpler to edit, maybe later
        public String metaname;
        public String author;
        public String version;
        public String texidfix;

        //Model & CSP lists
        public ArrayList csps;
        public ArrayList models;
        

        //Folders yay !
        String modelpath;
        String csppath;
        String dlc_csppath;
        String metapath;

        Boolean temped;
        

        //Handlers
        LibraryHandler Library;
        PropertyHandler Properties;
        MetaHandler Meta;
        Logger logger;
        #endregion

        #region Constructors
        // basic Constructor
        public Skin(String fullname,int slot,String libraryname,String origin, LibraryHandler global_library, PropertyHandler global_properties, Logger global_log)
        {
            //Gathering parameters into class variables
            this.fullname = fullname;
            this.slot = slot;
            this.modelslot = slot - 1;
            this.libraryname = libraryname;
            this.origin = origin;

            logger = global_log;

            //Creating lists
            csps = new ArrayList();
            models = new ArrayList();

            //Instanciating handlers to get info
            Library = global_library;
            Properties = global_properties;

            check_skin();

            //Getting the info I talked about earlier
            this.dlc = Library.get_dlc_status(this.fullname);
            this.modelfolder = Library.get_modelfolder_fullname(this.fullname);
            this.cspfolder = Library.get_cspfolder_fullname(this.fullname);
            this.datafolder = Properties.get("datafolder");

            //Now setting folders, easy ones
            this.modelpath = Application.StartupPath + "/mmsl_workspace/data/fighter/" + modelfolder + "/model/";
            this.csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
            if (dlc)
            {
                
                this.dlc_csppath = Application.StartupPath + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
                if (Properties.get("unlocalised") == "1")
                {
                    this.dlc_csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/append/chr/";
                }
            }
            //Config meta
            this.metapath = Application.StartupPath + "/mmsl_config/meta/" + this.modelfolder + "/slot_" + slot+"/";

            loadMeta();
            load_models();
            load_csp();

            //setting slot texts
            this.modelslotstring = (this.modelslot < 10 ? "0" + this.modelslot.ToString() : this.modelslot.ToString());
            this.slotstring = (this.slot < 10 ? "0" + this.slot.ToString() : this.slot.ToString());
            temped = false;
            

        }

        public Skin()
        {
            csps = new ArrayList();
            models = new ArrayList();

            Library = new LibraryHandler(Application.StartupPath + "/mmsl_config/Library.xml");
            Properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Config.xml");
        }
        #endregion

        #region SkinOperators
        public void clean_skin(int val)
        {
            if(models.Count > 0)
            {
                ArrayList modeles = this.models;

                while(this.models.Count != 0)
                {
                    delete_model(this.models[0].ToString());
                }
                    
                
                
            }
            if(csps.Count > 0)
            {
                foreach (String csp in this.csps)
                {
                    delete_csp(csp);
                }
            }
            load_csp();
            load_models();
            if(val == 0)
            {
                recreateMeta();
            }else
            {
                if (File.Exists(this.metapath + "meta.xml"))
                {
                    File.Delete(this.metapath + "meta.xml");
                }
            }
            
        }

        public void delete_skin()
        {
            clean_skin(1);
            Library.delete_skin(fullname, slot);
        }

        public void set_library_name(String libraryname)
        {
            Library.set_libraryname(this.fullname, this.slot, libraryname);
        }

        public void check_skin()
        {
            if(!Library.check_skin(fullname, slot))
            {
                Library.add_skin(fullname, slot,libraryname,origin);
            }
        }

        //Moves the file to a temp slot
        public void set_temp()
        {
            if (models.Count > 0)
            {
                ArrayList modeles = this.models;

               foreach(String modele in modeles)
                {
                    String folder = modele.Split('/')[1];
                    String parent = modele.Split('/')[0];
                    String dest="";
                    String source = "";
                    if(folder.Substring(0,1) == "c")
                    {
                        dest = "cxx";
                        source = "c" + modelslotstring;
                    }
                    if (folder.Substring(0, 1) == "l")
                    {
                        dest = "lxx";
                        source = "l" + modelslotstring;
                    }
                    if (Directory.Exists(modelpath+parent+"/"+dest))
                    {
                        Directory.Delete(modelpath + parent + "/" + dest, true);
                    }

                    Directory.CreateDirectory(modelpath + parent + "/" + dest);
                    foreach (String file in Directory.GetFiles(modelpath + parent + "/" + source))
                    {
                        File.Copy(file, modelpath + parent + "/" + dest + "/" + Path.GetFileName(file));
                    }
                    Directory.Delete(modelpath + parent + "/" + source, true);

                }




            }
        }

        public void move(int slot)
        {
            foreach(String model in models)
            {
                rename_model(model, slot);
            }

            foreach(String csp in csps)
            {
                rename_csp(csp, slot);
            }

            renameMeta(slot);

            if(slot == -1)
            {
                temped = true;
            }
            else
            {
                temped = false;
                set_new_slot(slot);
            }
            

        }

        public void reload_default_skin()
        {
            Library.get_skin_default(fullname,slot);
        }

        public void set_new_slot(int slot)
        {
            this.modelslot = slot;
            this.modelslotstring = (slot < 10 ? "0" + slot.ToString() : slot.ToString());
            this.slot = slot+1;
            this.slotstring = ((slot + 1) < 10 ? "0" + (slot + 1).ToString() : (slot + 1).ToString());
        }

        public void set_origin(String origins)
        {
            Library.set_origin(fullname, slot, origins);
            this.origin = origins;
        }

        public void check_missing_files_status()
        {
            logger.log("started check for " + fullname);
            //foreach csp
            foreach(String csp_name in csps)
            {
                String FilePath;
                String FilePath2;
                if (dlc)
                {
                    logger.log("char is dlc");
                    FilePath = dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                    logger.log("dlc csp path : "+ FilePath);
                    if (Library.get_moved_dlc_status(fullname))
                    {
                        logger.log("char is moved");
                        FilePath2 = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                        if (!File.Exists(FilePath2))
                        {
                            logger.log("file doesn't exist for csp path, set missing to true");
                            Library.set_csp_workspace_status(fullname, slot, csp_name, "missing");
                            missing = true;
                        }
                    }
                }
                else
                {
                    logger.log("char ins't dlc");
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                    logger.log("csp path : " + FilePath);
                }

                if (File.Exists(FilePath))
                {
                    logger.log("file exists");
                    if (Library.get_csp_workspace_status(fullname, slot, csp_name) == "unknown")
                    {
                        logger.log("csp is unknown");
                        unknown = true;
                        logger.log("setting global to unknown");
                    }
                    else
                    {
                        logger.log("csp is known");
                        if (Library.get_csp_workspace_status(fullname, slot, csp_name) == "new")
                        {
                            logger.log("setting global to new");
                            new_files = true;
                        }
                    }
                }
                else
                {
                    logger.log("file doesn't exist");
                    Library.set_csp_workspace_status(fullname, slot, csp_name, "missing");
                    missing = true;
                    logger.log("setting global to missing");
                }

            }
            foreach(String model_name in models)
            {
                String folder_path = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + modelslotstring;
                if (Directory.Exists(folder_path))
                {
                    if(Library.get_model_workspace_status(fullname,slot,model_name) == "unknown")
                    {
                        unknown = true;
                    }
                    else
                    {
                        if (Library.get_model_workspace_status(fullname, slot, model_name) == "new")
                        {
                            new_files = true;
                        }
                    }
                }
                else
                {
                    Library.set_model_workspace_status(fullname, slot, model_name, "missing");
                    missing = true;
                }
            }

        }

        public void set_imported()
        {
            //foreach csp
            foreach (String csp_name in csps)
            {
                String FilePath;
                if (dlc)
                {
                    FilePath = dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }
                else
                {
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }
                if (File.Exists(FilePath))
                {
                    if (Library.get_csp_workspace_status(fullname, slot, csp_name) == "new")
                    {
                        Library.set_csp_workspace_status(fullname, slot, csp_name, "imported");
                    }
                }
                else
                {
                    Library.set_csp_workspace_status(fullname, slot, csp_name, "missing");
                    missing = true;
                }
            }
            foreach (String model_name in models)
            {
                String folder_path = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + modelslotstring;
                if (Directory.Exists(folder_path))
                {
                    if (Library.get_model_workspace_status(fullname, slot, model_name) == "new")
                    {
                        Library.set_model_workspace_status(fullname, slot, model_name, "imported");
                    }
                }
                else
                {
                    Library.set_model_workspace_status(fullname, slot, model_name, "missing");
                    missing = true;
                }
            }
        }

        #endregion

        #region ModelOperators
        public void add_model(String source_model_path, String parent_name)
        {
            String folder_name = Path.GetFileName(source_model_path);

            Regex cXX = new Regex("^[c]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
            Regex lXX = new Regex("^[l]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);

            if (cXX.IsMatch(Path.GetFileName(folder_name)))
            {
                logger.log("folder is cXX/lXX match");

                //If source != destination
                if (source_model_path != modelpath)
                {
                    if (Directory.Exists(modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring))
                    {
                        Directory.Delete(modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring, true);
                        logger.log("deleting destination");
                    }

                    Directory.CreateDirectory(modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring);
                    logger.log("creating destination");
                    foreach (String file in Directory.GetFiles(source_model_path))
                    {
                        File.Copy(file, modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring + "/" + Path.GetFileName(file));
                    }
                }
                logger.log("files copied in : " + modelpath + Path.GetFileName(parent_name));
                this.models.Add(Path.GetFileName(parent_name) + "/cXX");
                Library.add_skin_model(this.fullname, this.slot, Path.GetFileName(parent_name) + "/cXX");
                set_model_workspace_status(Path.GetFileName(parent_name), "cXX", "new");
                logger.log("setting to new");

            }
            if (lXX.IsMatch(Path.GetFileName(folder_name)))
            {

                logger.log("folder is cXX/lXX match");
                if (Directory.Exists(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring))
                    {
                        Directory.Delete(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring, true);
                    logger.log("deleting destination");
                }

                logger.log("creating destination");
                Directory.CreateDirectory(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring);
                 logger.log("files copied in : " + modelpath + Path.GetFileName(parent_name));
                    foreach (String file in Directory.GetFiles(source_model_path))
                    {
                        File.Copy(file, modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring + "/" + Path.GetFileName(file));
                    }
                logger.log("files copied in : " + modelpath + Path.GetFileName(parent_name));
                this.models.Add(Path.GetFileName(parent_name) + "/lXX");
                    Library.add_skin_model(this.fullname, this.slot, Path.GetFileName(parent_name) + "/lXX");
                    set_model_workspace_status(Path.GetFileName(parent_name), "lXX", "new");
                logger.log("setting to new");
            }


        }
        public void delete_model(String model_name)
        {
            String folder_path = modelpath+ model_name.Split('/')[0]+"/"+ model_name.Split('/')[1].Substring(0,1)+modelslotstring;

            if (Directory.Exists(folder_path))
            {
                Directory.Delete(folder_path, true);
            }
            Library.delete_skin_model(this.fullname, this.slot, model_name);
            this.models.Remove(model_name);
        }
        public void rename_model(String model_name, int slot)
        {
            String new_modelslotstring = "";
            if (slot == -1)
            {
                new_modelslotstring = "xx";
            }else
            {
                new_modelslotstring = slot < 10 ? "0" + slot : slot.ToString();
            }
            String source;
            if (!temped)
            {
                source = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + modelslotstring;
            }else
            {
                source = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + "xx";
            }
            
            String destination = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + new_modelslotstring;

            if (Directory.Exists(source))
            {
                if (Directory.Exists(destination)) { Directory.Delete(destination,true); }
                Directory.CreateDirectory(destination);
                foreach(String file in Directory.GetFiles(source))
                {
                    File.Copy(file, destination+"/"+Path.GetFileName(file));
                }
            }
            if (Directory.Exists(source))
            {
                Directory.Delete(source, true);
            }
            
        }
        public void load_models()
        {
            models = Library.get_models(fullname, slot);
        }
        public String get_model_path(String model_name)
        {
            String folder_path = modelpath + model_name.Split('/')[0] + "/" + model_name.Split('/')[1].Substring(0, 1) + modelslotstring;
            return folder_path;
        }

        public void set_model_workspace_status(String parent_name,String model_name, String status)
        {
            Library.set_model_workspace_status(this.fullname, this.slot, Path.GetFileName(parent_name) + "/"+ model_name, status);
        }
        #endregion

        #region CspOperators
        public void add_csp(String csp_path)
        {
            logger.log("- Add CSP");
            String csp_name = Path.GetFileName(csp_path).Split('_')[0] + "_" + Path.GetFileName(csp_path).Split('_')[1];
            
            
            //Action for certain types

            //If DLC
            if (dlc)
            {
                logger.log("DLC");
                //If moved, include a copy in regular data folder
                if (Library.get_moved_dlc_status(fullname))
                {
                    logger.log("moved");
                    //Copy to regular folder
                    if (csp_path != csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut")
                    {
                        if (!Directory.Exists(csppath))
                        {
                            Directory.CreateDirectory(csppath + csp_name);
                            logger.log("create destination directory");
                        }
                        if (!File.Exists(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                        {
                            logger.log("copy to :"+csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                            //Copy to regular folder
                            try
                            {
                                File.Copy(csp_path, csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);
                            }
                            catch(Exception e)
                            {
                                logger.log("DLC COPY ERRORs");
                                logger.log(e.Message);
                                logger.log(e.StackTrace);

                            }
                            
                            Library.set_csp_workspace_status(fullname, slot, csp_name, "new");
                        }
                    }
                    
                }

                if (!Directory.Exists(dlc_csppath + csp_name))
                {
                    Directory.CreateDirectory(dlc_csppath + csp_name);
                    logger.log("create dlc destination directory");

                }
                if (!File.Exists(dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                {
                    logger.log("copy to :" + dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                    //Copy to append folder
                    try
                    {
                        File.Copy(csp_path, dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);
                    }
                    catch (Exception e)
                    {
                        logger.log("DLC COPY ERRORs");
                        logger.log(e.Message);
                        logger.log(e.StackTrace);
                    }
                    
                    Library.set_csp_workspace_status(fullname, slot, csp_name, "new");
                }
            }
            else
            {

                if (!Directory.Exists(csppath + csp_name))
                {
                    Directory.CreateDirectory(csppath + csp_name);
                    logger.log("create destination directory");
                }
                if (!File.Exists(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                {
                    logger.log("copy to :" + csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                    //Copy to regular folder
                    try
                    {
                        File.Copy(csp_path, csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);
                    }
                    catch (Exception e)
                    {
                        logger.log("NON DLC COPY ERRORs");
                        logger.log(e.Message);
                        logger.log(e.StackTrace);
                    }
                    
                    Library.set_csp_workspace_status(fullname, slot, csp_name, "new");
                }



            }

            this.csps.Add(csp_path);
            Library.add_skin_csp(this.fullname, this.slot, csp_name);

        }

        public void delete_csp(String csp_name)
        {
            this.models.Remove(csp_name);
            Library.delete_skin_csp(this.fullname, this.slot, csp_name);

            //If DLC
            if (dlc)
            {
                //If moved, include delete a copy in regular data folder
                if (Library.get_moved_dlc_status(fullname))
                {
                    //Delete to regular folder
                    if(File.Exists(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                    {
                        File.Delete(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                    }
                }
                else
                {
                    //Delete to append folder
                    if (File.Exists(dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                    {
                        File.Delete(dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                    }
                    
                }
            }
            else
            {
                //Delete to regular folder
                if (File.Exists(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut"))
                {
                    File.Delete(csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut");
                }
                
            }
        }

        public void rename_csp(String csp_name,int slot)
        {
            String FilePath;
            String new_slot = slot == -1 ? "xx" : (slot +1) < 10 ? "0" + (slot + 1) : (slot + 1).ToString();
            String new_name = csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
            String source_slot = temped ? "xx" : slotstring;
            if (dlc)
            {
                if (Library.get_moved_dlc_status(fullname))
                {
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + source_slot + ".nut";
                    new_name = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
                    if (File.Exists(FilePath))
                    {
                        File.Move(FilePath, new_name);
                    }
                }
                
                FilePath = dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + source_slot + ".nut";
                new_name = dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
                if (File.Exists(FilePath))
                {
                    File.Move(FilePath, new_name);
                }
            }
            else
            {
                FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + source_slot + ".nut";
                new_name = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
            }
            if (File.Exists(FilePath))
            {
                File.Move(FilePath, new_name);
            }
                
            
        }

        public void load_csp()
        {
            this.csps = Library.get_csps(fullname, slot);
        }

        public String get_csp_path(String csp_name)
        {
            String FilePath;
            if (dlc)
            {
                if (Library.get_moved_dlc_status(fullname))
                {
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }
                else
                {
                    FilePath = dlc_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }

            }
            else
            {
                FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
            }

            return FilePath;
        }

        public void set_csp_workspace_status(String csp_path, String status)
        {
            String csp_name = Path.GetFileName(csp_path).Split('_')[0] + "_" + Path.GetFileName(csp_path).Split('_')[1];
            Library.set_csp_workspace_status(this.fullname, this.slot, csp_name, status);
        }


        #endregion

        #region Meta
        internal void saveMeta(string author, string version, string name, string texidfix)
        {
            Meta.set("author", author);
            Meta.set("version", version);
            Meta.set("name", name);
            Meta.set("texidfix", texidfix);

            loadMeta();
        }

        internal void recreateMeta()
        {
            if (File.Exists(this.metapath + "meta.xml"))
            {
                File.Delete(this.metapath + "meta.xml");
                File.Copy(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml", this.metapath + "meta.xml", true);
            }
            loadMeta();
        }

        internal void loadMeta()
        {
            if (!Directory.Exists(this.metapath))
            {
                Directory.CreateDirectory(this.metapath);
            }
            if(!File.Exists(this.metapath + "meta.xml"))
            {
                File.Copy(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml", this.metapath + "meta.xml", true);
            }
            Meta = new MetaHandler(this.metapath + "meta.xml");

            //Setting metas
            this.metaname = Meta.get("name");
            this.version = Meta.get("version");
            this.author = Meta.get("author");
            this.texidfix = Meta.get("texidfix");
        }

        internal void addMeta(String meta_source_path)
        {
            File.Copy(meta_source_path, metapath + "meta.xml", true);
            loadMeta();
        }

        internal void renameMeta(int newslot)
        {
            String dest = newslot == -1 ? "xx" : (newslot + 1).ToString();
            String source = temped ? "xx" : this.slot.ToString();
            String source_path = Application.StartupPath + "/mmsl_config/meta/" + modelfolder + "/slot_" + source;
            String destination_path = Application.StartupPath + "/mmsl_config/meta/" + modelfolder + "/slot_" + dest;
            if (!Directory.Exists(destination_path))
            {
                Directory.CreateDirectory(destination_path);
            }
            File.Copy(source_path + "/meta.xml", destination_path + "/meta.xml",true);
            this.metapath = destination_path + "/meta.xml";
            Directory.Delete(source_path,true);

        }
        #endregion

        #region MeteorSkins
        public void package_meteor()
        {
            
            String package_destination = Application.StartupPath + "/mmsl_packages/"+fullname+"/meteor_xx_" + libraryname+"/";
            if (Directory.Exists(package_destination + "meta"))
            {
                Directory.Delete(package_destination + "meta", true);
            }
            if (Directory.Exists(package_destination + "csp"))
            {
                Directory.Delete(package_destination + "csp", true);
            }
            if (Directory.Exists(package_destination + "model"))
            {
                Directory.Delete(package_destination + "model", true);
            }
            Directory.CreateDirectory(package_destination + "meta");
            Directory.CreateDirectory(package_destination + "csp");
            Directory.CreateDirectory(package_destination + "model");

            foreach(String model in this.models)
            {
                String folder_path = modelpath + model.Split('/')[0] + "/" + model.Split('/')[1].Substring(0, 1) + modelslotstring;
                Directory.CreateDirectory(package_destination + "model/" + model.Split('/')[0] + "/" + model.Split('/')[1].Substring(0, 1) + modelslotstring);
                if (Directory.GetFiles(folder_path).Length > 0)
                {
                    foreach (String file in Directory.GetFiles(folder_path))
                    {
                        File.Copy(file, package_destination + "model/" + model.Split('/')[0] + "/" + model.Split('/')[1].Substring(0, 1) + modelslotstring + "/" + Path.GetFileName(file));
                    }
                }
                
            }

            foreach(String csp in this.csps)
            {

                String FilePath="";


                if (dlc)
                {
                    FilePath = dlc_csppath;
                }else
                {
                    FilePath = csppath;
                }
                FilePath += csp + "/" + csp + "_" + cspfolder + "_" + slotstring + ".nut";
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, package_destination + "csp/" + Path.GetFileName(FilePath));
                }
            }

            File.Copy(metapath + "meta.xml", package_destination + "meta/meta.xml");
            
        }

        public String[] get_meteor_info()
        {
            String files = "";
            foreach(String model in models)
            {
                files += model+" | ";
            }
            foreach (String csp in csps)
            {
                files += csp + " | ";
            }
            String[] values = new String[] { "xx",fullname, libraryname,files};

            return values;
        }
        #endregion

    }
}
