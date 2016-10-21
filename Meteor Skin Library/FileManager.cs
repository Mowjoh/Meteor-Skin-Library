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

    }
}
