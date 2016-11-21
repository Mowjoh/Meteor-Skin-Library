using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace MeteorSkinLibrary
{
    class UICharDBHandler
    {

        #region Class Variables
        //S4E
        String ui_file_path_source;
        //MMSL
        String ui_file_path_destination;
        int[] offsets = new int[] { 0, 5, 7, 9, 11, 16, 21, 26, 31, 36, 38, 40, 42, 44, 46, 48, 50, 55, 57, 59, 61, 63, 65, 67, 69, 71, 73, 75, 77, 79, 81, 83, 85, 87, 89, 91, 93, 95, 97, 99, 101, 103, 105, 107, 109, 111, 113, 115, 117, 119, 121, 123, 125 };

        public Boolean imported;
        public Boolean sourcepresent;
        #endregion

        #region Constructors
        //Basic Constructor
        public UICharDBHandler(String uipath, String datafolder)
        {
            PropertyHandler properties = new PropertyHandler();
            properties.set_config_path(Application.StartupPath + "/mmsl_config/Config.xml");
            imported = false;
            sourcepresent = false;

            if (uipath != "" && datafolder != "")
            {
                if (datafolder == "data")
                {
                    datafolder = "data(us_en)";
                }
                if (properties.property_get("unlocalised") == "1")
                {
                    datafolder = "data";
                }
                //Source definition
                ui_file_path_source = uipath + "/content/patch/" + datafolder + "/param/ui/ui_character_db.bin";
                //Destination definition
                this.ui_file_path_destination = Application.StartupPath + "/mmsl_workspace/" + datafolder + "/param/ui/ui_character_db.bin";

                //Checks if the param/ui folder exists so it knows if a backup may be here
                if (!Directory.Exists(Application.StartupPath + "/mmsl_workspace/" + datafolder + "/param/ui/"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "/mmsl_workspace/" + datafolder + "/param/ui/");
                    //If source exists
                    if (File.Exists(ui_file_path_source))
                    {
                        File.Copy(ui_file_path_source, ui_file_path_destination);
                        Console.WriteLine("Copied ui_char_db");
                        sourcepresent = true;
                        imported = true;
                    }
                    else
                    {
                        Console.WriteLine("No source ui_char_db");
                    }
                }
                else
                {

                    //There is a copy
                    if (File.Exists(ui_file_path_destination))
                    {
                        Console.WriteLine("current ui_char_db was found");
                        imported = true;
                    }
                    else
                    {
                        //If source exists
                        if (File.Exists(ui_file_path_source))
                        {
                            File.Copy(ui_file_path_source, ui_file_path_destination);
                            Console.WriteLine("Copied ui_char_db");
                            imported = true;
                        }
                        else
                        {
                            Console.WriteLine("No source ui_char_db");
                        }
                    }
                }





            }
        }
        #endregion

        #region FileActions
        //returns a String arraylist of ui_char_db values for that character
        public String fileRead(Int64 characterposition, int position)
        {
            Console.WriteLine(characterposition);
            String val = "";
            if (imported == true)
            {
                Stream stream = File.Open(ui_file_path_destination, FileMode.Open);
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long pose = 13 + (127 * characterposition) + offsets[position];
                    stream.Seek(pose, SeekOrigin.Begin);
                    int bit = br.ReadByte();
                    switch (bit)
                    {
                        case 2:
                            val = br.ReadByte().ToString();
                            break;
                        case 5:
                            Byte[] bytes = br.ReadBytes(4);
                            Array.Reverse(bytes);
                            val = BitConverter.ToUInt32(bytes, 0).ToString();
                            break;
                        case 6:
                            Byte[] bytes2 = br.ReadBytes(4);
                            Array.Reverse(bytes2);
                            val = BitConverter.ToUInt32(bytes2, 0).ToString();
                            break;
                    }
                }
            }
            return val;
        }

        //Sets a value in the file
        public void setFile(int characterposition, int slot, int value)
        {
            if (imported == true)
            {

                if (imported == true)
                {
                    Stream stream = File.Open(ui_file_path_destination, FileMode.Open);
                    using (BinaryReader br = new BinaryReader(stream))
                    {
                        long pose = 13 + (127 * characterposition) + offsets[slot];
                        Console.WriteLine(characterposition);
                        stream.Seek(pose, SeekOrigin.Begin);
                        int bit = br.ReadByte();
                        switch (bit)
                        {
                            case 2:
                                stream.WriteByte(BitConverter.GetBytes(value)[0]);
                                break;

                            case 5:
                                Byte[] bytes = BitConverter.GetBytes(value);
                                Array.Reverse(bytes);
                                stream.Write(bytes, 0, 4);
                                break;
                            case 6:
                                Byte[] bytes2 = BitConverter.GetBytes(value);
                                Array.Reverse(bytes2);
                                stream.Write(bytes2, 0, 4);
                                break;
                        }
                    }
                }
            }
        }
        #endregion

    }
}
