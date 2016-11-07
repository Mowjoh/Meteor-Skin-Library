using MeteorSkinLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Meteor_Skin_Library
{
    class Melee
    {

        PropertyHandler properties;
        String explorer_workspace;
        String datafolder;
        Boolean imported = false;
        //Constructor
        public Melee()
        {
            //Setting up property handler
            properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Config.xml");
            //Getting base paths
            explorer_workspace = properties.get("explorer_workspace");
            datafolder = properties.get("datafolder");

        }

        #region File Handling
        public Boolean check_file()
        {
            //Setting specific paths

            String extractfolder = Directory.GetParent(explorer_workspace)+ "/extract/" + datafolder + "/ui/message";
            String explorerfolder = explorer_workspace + "/content/patch/" + datafolder + "/ui/message";
            String workspace = Application.StartupPath+"/mmsl_workspace/"+datafolder+ "/ui/message";
            if (File.Exists(workspace+"/melee.msbt")){
                imported = true;
            }
            else
            {
                if (File.Exists(explorerfolder))
                {
                    imported = true;
                    if (!Directory.Exists(workspace))
                    {
                        Directory.CreateDirectory(workspace);
                    }
                    File.Copy(explorerfolder+"/melee.msbt", workspace+"/melee.msbt");
                }
                else
                {
                    if (File.Exists(extractfolder + "/melee.msbt"))
                    {
                        imported = true;
                        if (!Directory.Exists(workspace))
                        {
                            Directory.CreateDirectory(workspace);
                        }
                        File.Copy(extractfolder + "/melee.msbt", workspace + "/melee.msbt");

                    }
                    else
                    {
                        imported = false;
                    }
                }
            }
            return imported;
        }

        #endregion


    }
}
