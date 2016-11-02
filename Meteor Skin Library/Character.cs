using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeteorSkinLibrary
{
    class Character
    {
        //Handlers
        LibraryHandler Library;
        PropertyHandler properties;
        UICharDBHandler ui;
        //Character Variables
        public String fullname;
        public ArrayList skins;

        //Character Constructor
        public Character(String full_name)
        {
            //Setting variables
            this.fullname = full_name;

            //Instanciating handlers to get info
            Library = new LibraryHandler(Application.StartupPath+ "/mmsl_config/Library.xml");
            properties = new PropertyHandler(Application.StartupPath + "/mmsl_config/Config.xml");
            ui = new UICharDBHandler(properties.get("explorer_workspace"), properties.get("datafolder"));

            //Getting skins
            skins = new ArrayList();
            getSkins();
        }

        //Retreives the list of skins from the library
        public void getSkins()
        {
            skins.Clear();
            ArrayList skinlist = Library.get_skin_list(fullname);
            foreach (String slot in skinlist)
            {
                Skin skin = new Skin(fullname, int.Parse(slot), Library.get_skin_libraryname(fullname, int.Parse(slot)), Library.get_skin_origin(fullname, int.Parse(slot)));
                skins.Add(skin);
            }
        }
        //Adds a skin to the library
        public void add_skin()
        {
            Skin newe = new Skin(fullname,skins.Count + 1, "New Skin", "Custom");
            
            skins.Add(newe);
            //Seventh slot of ui_char_db is skin slot count
            ui.setFile(int.Parse(Library.get_ui_char_db_id(fullname)), 7, skins.Count);
        }

        //To edit for swapping
        public void delete_skin(int slot)
        {
            Skin deleted = (Skin)skins[slot];
            deleted.delete_skin();
            skins.Remove(deleted);
            remake_skinlist();
        }

        //Swaps two skins
        public void swap_skin(int origin, int destination)
        {
            Skin ori = (Skin)skins[origin];
            if(!(skins.Count == (origin + 1) && destination == origin +1))
            {
                Skin dest = (Skin)skins[destination];
                int action = 0;

                //Checking what action needs to be done

                //No swapping for default slots
                if (ori.origin == "Default")
                {
                    action = -1;
                }
                //Means inserting a skin up
                if (ori.origin == "Default Replaced" && dest.origin == "Custom")
                {
                    action = 1;
                }
                //Means overwriting a default slot
                if (ori.origin == "Custom" && dest.origin == "Default")
                {
                    action = 2;
                }
                //Means swapping a Default replaced ont a default slot
                if (ori.origin == "Default Replaced" && dest.origin == "Default")
                {
                    action = 3;
                }
                switch (action)
                {
                    case -1:
                        break;
                    case 0:
                        swap(ori, dest);
                        break;
                    case 1:
                        Skin new_skin = new Skin(fullname, ori.slot, "Default", "Default");
                        Library.insert_skin(fullname, new_skin.slot, new_skin.libraryname, new_skin.origin);
                        skins.Insert(ori.slot, new_skin);
                        Library.reload_skin_order(fullname);
                        new_skin.reload_default_skin();
                        dest = (Skin)skins[destination + 1];
                        dest.set_origin("Custom");
                        remake_skinlist_inverted();

                        break;
                    case 2:
                        swap(ori, dest);
                        dest.delete_skin();
                        skins.Remove(dest);
                        ori.set_origin("Default Replaced");
                        remake_skinlist();
                        Library.reload_skin_order(fullname);
                        break;
                    case 3:
                        swap(ori, dest);
                        dest.reload_default_skin();
                        break;
                }
            }else
            {
               
            }
            
        }

        //Swaps skin files and library info
        void swap(Skin s1, Skin s2)
        {
            int s1slot = s1.modelslot;
            int s2slot = s2.modelslot;
            //Moving origin files to temp
            s1.move(-1);
            //moving destination to origin slot
            s2.move(s1slot);
            //Moving origin to destination slot
            s1.move(s2slot);

            //Swapping in library
            Library.swap_skin(fullname, s1slot +1, s2slot +1);

        }

        //moves skin to first empty slot it finds
        public void remake_skinlist()
        {
            for(int i = 0; i < skins.Count; i++)
            {
                Skin current = (Skin)skins[i];
                if(current.modelslot != i)
                {
                    current.move(i);
                }
            }
        }

        //moves skin to first empty slot it finds
        public void remake_skinlist_inverted()
        {
            for (int i = (skins.Count-1); i >= 0; i--)
            {
                Skin current = (Skin)skins[i];
                if (current.modelslot != i)
                {
                    current.move(i);
                }
            }
        }

    }
}
