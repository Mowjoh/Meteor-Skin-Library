using Meteor_Skin_Library.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Meteor_Skin_Library
{
    public class Filebank
    {

        #region Class Variables
        String filebank_xmlpath = Application.StartupPath + "/mmsl_config/Filebank.xml";
        #endregion

        #region Constructors
        public Filebank()
        {
            check_filebank_folder();
            check_filebank_library();
        }
        #endregion

        #region Skins

        
        #region Get
        public String[] get_skin_info(int id, String fullname)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            String[] skin_info = new String[3];

            XmlNode skin = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id.ToString() + "']");

            if (skin == null)
            {
                return skin_info;
            }
            else
            {
                skin_info[0] = skin.Attributes["libraryname"].Value + ";" + skin.Attributes["id"].Value;

                XmlNode models = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id.ToString() + "']/models");
                XmlNode csps = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id.ToString() + "']/csps");

                if (models != null)
                {
                    skin_info[1] = models.InnerText;
                }
                else
                {
                    skin_info[1] = "";
                }

                if (csps != null)
                {
                    skin_info[2] = csps.InnerText;
                }
                else
                {
                    skin_info[2] = "";
                }


                return skin_info;
            }


        }
        public ArrayList get_skins(String fullname)
        {
            ArrayList Skins_array = new ArrayList();

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNodeList nodes = xml.SelectNodes("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin");

            foreach (XmlElement node in nodes)
            {
                Skins_array.Add(get_skin_info(int.Parse(node.Attributes["id"].Value),fullname));
            }

            return Skins_array;

        }
        #endregion

        #region Set
        public void add_skin(NewSkin skin, String model_list, String csp_list)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(skin.fullname);

            #region Xml Loads
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + skin.fullname + "']/skins");
            #endregion

            #region Element Creation
            //Element
            XmlElement xmlskin = xml.CreateElement("skin");

            //ID
            XmlAttribute attr = xml.CreateAttribute("id");
            attr.Value = skin.id.ToString();
            xmlskin.Attributes.Append(attr);

            //Name
            XmlAttribute attr2 = xml.CreateAttribute("libraryname");
            attr2.Value = skin.libraryname;
            xmlskin.Attributes.Append(attr2);

            //Files
            XmlElement models = xml.CreateElement("models");
            models.InnerText = model_list;
            XmlElement csps = xml.CreateElement("csps");
            csps.InnerText = csp_list;
            xmlskin.AppendChild(models);
            xmlskin.AppendChild(csps);

            #endregion

            skins.AppendChild(xmlskin);
            xml.Save(filebank_xmlpath);

        }
        public void add_skin(NewSkin skin)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(skin.fullname);

            #region Xml Loads
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + skin.fullname + "']/skins");
            #endregion

            #region Element Creation
            //Element
            XmlElement xmlskin = xml.CreateElement("skin");

            //ID
            XmlAttribute attr = xml.CreateAttribute("id");
            attr.Value = skin.id.ToString();
            xmlskin.Attributes.Append(attr);

            //Name
            XmlAttribute attr2 = xml.CreateAttribute("libraryname");
            attr2.Value = skin.libraryname;
            xmlskin.Attributes.Append(attr2);

            //Files
            XmlElement models = xml.CreateElement("models");
            models.InnerText = "";
            XmlElement csps = xml.CreateElement("csps");
            csps.InnerText = "";
            xmlskin.AppendChild(models);
            xmlskin.AppendChild(csps);

            #endregion

            skins.AppendChild(xmlskin);
            xml.Save(filebank_xmlpath);

        }
        public void add_skin_model(String fullname, int id, String model_name)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(fullname);

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']/models");

            //Checking if model tag exists
            if (skins != null)
            {
                //getting it's text
                String text = skins.InnerText;
                //If there is some text, splitting it
                if (text != "")
                {
                    String[] values = text.Split(';');
                    String newvalues = "";

                    Boolean present = false;
                    //Parsing the values for reconstruct
                    foreach (String s in values)
                    {
                        if (s != "")
                        {
                            if (s == model_name)
                            {
                                present = true;
                            }
                            newvalues += s + ";";
                        }
                    }
                    if (!present)
                    {
                        newvalues += model_name;
                    }

                    skins.InnerText = newvalues;

                }
                else
                {
                    skins.InnerText = model_name + ";";
                }
            }
            else
            {

            }
            xml.Save(filebank_xmlpath);

        }
        public void add_skin_csp(String fullname, int id, String csp_name)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(fullname);

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']/csps");

            //Checking if model tag exists
            if (skins != null)
            {
                //getting it's text
                String text = skins.InnerText;
                //If there is some text, splitting it
                if (text != "")
                {
                    String[] values = text.Split(';');
                    String newvalues = "";

                    Boolean present = false;
                    //Parsing the values for reconstruct
                    foreach (String s in values)
                    {
                        if (s != "")
                        {
                            if (s == csp_name)
                            {
                                present = true;
                            }
                            newvalues += s + ";";
                        }
                    }
                    if (!present)
                    {
                        newvalues += csp_name;
                    }

                    skins.InnerText = newvalues;

                }
                else
                {
                    skins.InnerText = csp_name + ";";
                }
            }
            else
            {

            }
            xml.Save(filebank_xmlpath);
        }
        public void delete_skin(String fullname, int id)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins");
            XmlNode skin = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']");
            skins.RemoveChild(skin);
            xml.Save(filebank_xmlpath);
        }
        public void delete_skin_model(String fullname, int id, String model_name)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(fullname);

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']/models");
            String text = skins.InnerText;
            if(text != "")
            {
                String[] values = text.Split(';');
                String newvalues = "";
                foreach (String s in values)
                {
                    if(s != "" && s != model_name)
                    {
                        newvalues += s + ";";
                    }
                }

                skins.InnerText = newvalues;
            }

            xml.Save(filebank_xmlpath);

        }
        public void delete_skin_csp(String fullname, int id, String csp_name)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(fullname);

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']/csps");
            String text = skins.InnerText;
            if (text != "")
            {
                String[] values = text.Split(';');
                String newvalues = "";
                foreach (String s in values)
                {
                    if (s != "" && s != csp_name)
                    {
                        newvalues += s + ";";
                    }
                }

                skins.InnerText = newvalues;
            }

            xml.Save(filebank_xmlpath);
        }
        public void set_skin_name(String fullname, int id, String newlibraryname)
        {
            //Checks if the skin tag is present for the character
            check_skin_tag(fullname);

            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);
            XmlNode skins = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id + "']");
            skins.Attributes["libraryname"].Value = newlibraryname;

            xml.Save(filebank_xmlpath);


        }
        #endregion

        #region Check
        public Boolean check_skin_id(int id, String fullname)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);

            XmlNode skin = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins/skin[attribute::id='" + id.ToString() + "']");
            if (skin == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
        

        #endregion

        #region Folders
        public void check_filebank_folder()
        {
            if (!Directory.Exists(Application.StartupPath + "/mmsl_filebank"))
            {
                Directory.CreateDirectory(Application.StartupPath + "/mmsl_filebank");
            }
        }

        #endregion

        #region Library
        //Checks file
        public void check_filebank_library()
        {
            if (!File.Exists(filebank_xmlpath))
            {
                if (File.Exists(Application.StartupPath + "/mmsl_config/Default_Filebank.xml"))
                {
                    File.Copy(Application.StartupPath + "/mmsl_config/Default_Filebank.xml", filebank_xmlpath, true);
                }
            }
        }

        #region Checks
        private void check_skin_tag(String fullname)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filebank_xmlpath);

            XmlNode skin_tag = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']/skins");
            if(skin_tag == null)
            {
                XmlElement skins = xml.CreateElement("skins");
                XmlNode character = xml.SelectSingleNode("/Filebank/Character[attribute::name='" + fullname + "']");
                character.AppendChild(skins);
            }

            xml.Save(filebank_xmlpath);
        }
        #endregion

        #endregion

    }
}
