using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MeteorSkinLibrary
{
    class MetaHandler
    {
        #region ClassVariables
            String MetaPath = "";
        #endregion

        #region Constructors
        //basic
        public MetaHandler()
        {
            MetaPath = "";
        }
        //With folderpath
        public MetaHandler(string custom_LibraryPath)
        {
            MetaPath = custom_LibraryPath;
        }
        #endregion

        #region Values
        //gets a property value
        internal String get(string meta_name)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(MetaPath);
            XmlNode property = xml.SelectSingleNode("/metadata/meta[attribute::name='" + meta_name + "']");
            return property.InnerText;
        }
        //Sets a property value
        internal void set(string meta_name, string meta_value)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(MetaPath);
            XmlNode property = xml.SelectSingleNode("/metadata/meta[attribute::name='" + meta_name + "']");
            property.InnerText = meta_value;

            xml.Save(MetaPath);
        }
        //Adds a property and it's value
        internal void add(string meta_name, string meta_value)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(MetaPath);
            XmlNode properties = xml.SelectSingleNode("/metadata");

            XmlNode verify = xml.SelectSingleNode("/metadata/meta[attribute::name='" + meta_name + "']");
            if (verify == null)
            {
                XmlElement property = xml.CreateElement("meta");
                property.SetAttribute("name", meta_name);
                property.InnerText = meta_value;
                properties.AppendChild(property);
            }
            else
            {
                XmlNode config = xml.SelectSingleNode("/metadata");
                config.RemoveChild(verify);

                XmlElement property = xml.CreateElement("meta");
                property.SetAttribute("name", meta_name);
                property.InnerText = meta_value;
                properties.AppendChild(property);

            }

            xml.Save(MetaPath);
        }
        #endregion

        #region Path
        public void set_meta_path(String path)
        {
            this.MetaPath = path;
        }
        #endregion 
    }
}
