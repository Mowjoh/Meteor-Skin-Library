using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MeteorSkinLibrary
{
    class FileManager
    {
        LibraryHandler Library;
        PropertyHandler properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Default_Config.xml");
        MetaHandler meta = new MetaHandler(Application.StartupPath + "/mmsl_config/meta/Default_Meta.xml");
        UICharDBHandler uichar;

        public FileManager(LibraryHandler lib, UICharDBHandler ui)
        {
            this.Library = lib;
            this.uichar = ui;

        }

        //Assuming that the filemanager is at the root of all file placement, 
        //I got a lot to do well. First off : add_skin



        #region Skins
        public Boolean add_skin(String meteorpath,String character)
        {

            return false;
        }

        public Boolean delete_skin()
        {

            return false;
        }

        public Boolean clean_skin()
        {

            return false;
        }
        public Boolean reset_skin()
        {

            return false;
        }
        #endregion

    }
}
