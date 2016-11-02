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
        String edited_csppath;
        String metapath;

        Boolean temped;
        

        //Handlers
        LibraryHandler Library;
        PropertyHandler Properties;
        MetaHandler Meta;
        #endregion

        #region Constructors
        // basic Constructor
        public Skin(String fullname,int slot,String libraryname,String origin)
        {
            //Gathering parameters into class variables
            this.fullname = fullname;
            this.slot = slot;
            this.modelslot = slot - 1;
            this.libraryname = libraryname;
            this.origin = origin;

            //Creating lists
            csps = new ArrayList();
            models = new ArrayList();

            //Instanciating handlers to get info
            Library = new LibraryHandler(Application.StartupPath + "/mmsl_config/Library.xml");
            Properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Config.xml");

            check_skin();

            //Getting the info I talked about earlier
            this.dlc = Library.get_dlc_status(this.fullname);
            this.modelfolder = Library.get_modelfolder_fullname(this.fullname);
            this.cspfolder = Library.get_cspfolder_fullname(this.fullname);
            this.datafolder = Properties.get("datafolder");

            //Now setting folders, easy ones
            this.modelpath = Application.StartupPath + "/mmsl_workspace/data/fighter/" + modelfolder + "/model/";
            if (dlc)
            {
                if(Library.get_moved_dlc_status(fullname))
                {
                    this.csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
                    if(Properties.get("unlocalised") == "0")
                    {
                        this.edited_csppath = Application.StartupPath + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
                    }else
                    {
                        this.edited_csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/append/chr/";
                    }
                   
                }else
                {
                    if (Properties.get("unlocalised") == "0")
                    {
                        this.csppath = Application.StartupPath + "/mmsl_workspace/" + datafolder + "/ui/replace/append/chr/";
                    }
                    else
                    {
                        this.edited_csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/append/chr/";
                    }
                }
            }else
            {
                this.csppath = Application.StartupPath + "/mmsl_workspace/data/ui/replace/chr/";
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

        #endregion

        #region ModelOperators
        public void add_model(String source_model_path, String parent_name)
        {
            String folder_name = Path.GetFileName(source_model_path);

            Regex cXX = new Regex("^[c]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
            Regex lXX = new Regex("^[l]([0-9]{2}|xx|[0-9]x|x[0-9])$", RegexOptions.IgnoreCase);
            
            if (cXX.IsMatch(Path.GetFileName(folder_name)))
            {
                if (Directory.Exists(modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring))
                {
                    Directory.Delete(modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring, true);
                }

                Directory.CreateDirectory(modelpath+ Path.GetFileName(parent_name) + "/c" + this.modelslotstring);

                foreach (String file in Directory.GetFiles(source_model_path))
                {
                    File.Copy(file, modelpath + Path.GetFileName(parent_name) + "/c" + this.modelslotstring + "/" + Path.GetFileName(file));
                }
                this.models.Add(Path.GetFileName(parent_name) + "/cXX");
                Library.add_skin_model(this.fullname, this.slot, Path.GetFileName(parent_name) + "/cXX");

            }
            if (lXX.IsMatch(Path.GetFileName(folder_name)))
            {
                if (Directory.Exists(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring))
                {
                    Directory.Delete(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring, true);
                }

                Directory.CreateDirectory(modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring);

                foreach (String file in Directory.GetFiles(source_model_path))
                {
                    File.Copy(file, modelpath + Path.GetFileName(parent_name) + "/l" + this.modelslotstring + "/" + Path.GetFileName(file));
                }

                this.models.Add(Path.GetFileName(parent_name) + "/lXX" );
                Library.add_skin_model(this.fullname, this.slot, Path.GetFileName(parent_name) + "/lXX");
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

        #endregion

        #region CspOperators
        public void add_csp(String csp_path)
        {
            String csp_name = Path.GetFileName(csp_path).Split('_')[0] + "_" + Path.GetFileName(csp_path).Split('_')[1];
            this.csps.Add(csp_name);
            Library.add_skin_csp(this.fullname, this.slot, csp_name);
            
            //Checking special cases
            if(csp_name == "chr_13")
            {
                //If moved DLC
                if (Library.get_moved_dlc_status(fullname))
                {
                    if (!Directory.Exists(edited_csppath + csp_name))
                    {
                        Directory.CreateDirectory(edited_csppath + csp_name);
                        
                    }
                    File.Copy(csp_path, edited_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);

                }
                else
                {
                    if (!Directory.Exists(csppath + csp_name))
                    {
                        Directory.CreateDirectory(csppath + csp_name);
                        
                    }
                    File.Copy(csp_path, csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);

                }
                
            }else
            {   
                //Checking dlc
                if (!Directory.Exists(csppath + csp_name))
                {
                    Directory.CreateDirectory(csppath + csp_name);
                    
                }
                File.Copy(csp_path, csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut", true);
            }
        }

        public void delete_csp(String csp_name)
        {
            String FilePath;
            if(csp_name == "chr_13")
            {
                if (Library.get_moved_dlc_status(fullname))
                {
                    FilePath = edited_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }else
                {
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
                }
                
            }else
            {
                FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + slotstring + ".nut";
            }
            

            this.models.Remove(csp_name);

            Library.delete_skin_csp(this.fullname, this.slot, csp_name);

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        public void rename_csp(String csp_name,int slot)
        {
            String FilePath;
            String new_slot = slot == -1 ? "xx" : (slot +1) < 10 ? "0" + (slot + 1) : (slot + 1).ToString();
            String new_name = csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
            String source_slot = temped ? "xx" : slotstring;
            if (csp_name == "chr_13")
            {
                if (Library.get_moved_dlc_status(fullname))
                {
                    FilePath = edited_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + source_slot + ".nut";
                    new_name = edited_csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
                }
                else
                {
                    FilePath = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + source_slot + ".nut";
                    new_name = csppath + csp_name + "/" + csp_name + "_" + cspfolder + "_" + new_slot + ".nut";
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

                if(csp == "chr_13")
                {
                    if (Library.get_moved_dlc_status(fullname))
                    {
                        FilePath = edited_csppath + csp + "/" + csp + "_" + cspfolder + "_" + slotstring + ".nut";
                    }else
                    {
                        FilePath = csppath + csp + "/" + csp + "_" + cspfolder + "_" + slotstring + ".nut";
                    }
                    
                }else
                {
                    FilePath = csppath + csp + "/" + csp + "_" + cspfolder + "_" + slotstring + ".nut";
                }

                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, package_destination + "csp/" + Path.GetFileName(FilePath));
                }
            }

            File.Copy(metapath + "meta.xml", package_destination + "meta/meta.xml");
            
        }
        #endregion

    }
}
