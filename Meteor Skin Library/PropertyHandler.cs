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
    public class PropertyHandler
    {
        private string LibraryPath;

        #region Constructors
        //basic
        public PropertyHandler()
        {
            LibraryPath = "";
        }
        //With folderpath
        public PropertyHandler(string custom_LibraryPath)
        {
            LibraryPath = custom_LibraryPath;
        }
        #endregion

        #region Properties

        //Gets a property
        internal String property_get(string property_name)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(LibraryPath);
            XmlNode property = xml.SelectSingleNode("/config/property[attribute::name='" + property_name + "']");
            if(property == null)
            {
                return "";
            }else
            {
                return property.InnerText;
            }
        }
        //Sets a property
        internal void property_set(string property_name, string property_value)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(LibraryPath);
            XmlNode property = xml.SelectSingleNode("/config/property[attribute::name='" + property_name + "']");
            property.InnerText = property_value;

            xml.Save(LibraryPath);
        }
        //Adds a property
        internal void property_add(string property_name, string property_value)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(LibraryPath);
            XmlNode properties = xml.SelectSingleNode("/config");

            XmlNode verify = xml.SelectSingleNode("/config/property[attribute::name='" + property_name + "']");
            if(verify == null)
            {
                XmlElement property = xml.CreateElement("property");
                property.SetAttribute("name", property_name);
                property.InnerText = property_value;
                properties.AppendChild(property);
            }
            else
            {
                XmlNode config = xml.SelectSingleNode("/config");
                config.RemoveChild(verify);

                XmlElement property = xml.CreateElement("property");
                property.SetAttribute("name", property_name);
                property.InnerText = property_value;
                properties.AppendChild(property);

            }
            
            xml.Save(LibraryPath);
        }
        //Checks if property exists
        internal Boolean property_check(string property_name)
        {
            Boolean test = false;

            //Loads the XML
            XmlDocument xml = new XmlDocument();
            xml.Load(LibraryPath);
            
            //Testing the existence of the property
            test = xml.SelectSingleNode("/config/property[attribute::name='" + property_name + "']") != null ? true : false;

            return test;
        }

        #endregion

        #region Path
        //Sets a specific Library path
        public void set_library_path(String path)
        {
            this.LibraryPath = path;
        }
        #endregion



    }
}