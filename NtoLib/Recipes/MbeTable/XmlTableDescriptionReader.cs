using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NtoLib.Recipes.MbeTable
{
    internal class XmlTableDescriptionReader
    {
        private Dictionary<string, TableEnumType> enumMap = new Dictionary<string, TableEnumType>();

        //private void ReadTableDescriptionFromXML()
        //{
        //    make_table_msg += "Загрузка структуры таблицы. ";
        //    enumMap.Clear();
        //    columns.Clear();
        //    _float_colum_num = 0;
        //    _int_colum_num = 0;
        //    _bool_colum_num = 0;

        //    XmlDocument xmlDocument = new XmlDocument();
        //    try
        //    {
        //        xmlDocument.Load(this._pathToXmlTableDefinition);
        //    }
        //    catch (Exception ex)
        //    {
        //        this.make_table_msg += "load error ";
        //        this.make_table_msg += ex.Message;
        //        return;
        //    }
        //    XmlElement documentElement = xmlDocument.DocumentElement;
        //    foreach (XmlNode childNode1 in documentElement.ChildNodes)
        //    {
        //        if (childNode1 is XmlElement)
        //        {
        //            XmlElement xmlElement1 = (XmlElement)childNode1;
        //            if (xmlElement1.Name == "EnumType")
        //            {
        //                string str1 = "";
        //                foreach (XmlAttribute attribute in (XmlNamedNodeMap)xmlElement1.Attributes)
        //                {
        //                    if (attribute.Name == "Name")
        //                        str1 = attribute.Value;
        //                }
        //                if (!string.IsNullOrEmpty(str1) && !(str1 == "int") && !(str1 == "bool") && !(str1 == "float"))
        //                {
        //                    TableEnumType tableEnumType = new TableEnumType(str1);
        //                    foreach (XmlNode childNode2 in xmlElement1.ChildNodes)
        //                    {
        //                        if (childNode2 is XmlElement)
        //                        {
        //                            XmlElement xmlElement2 = (XmlElement)childNode2;
        //                            if (!(xmlElement2.Name != "Enum"))
        //                            {
        //                                string str2 = "";
        //                                string s = "";
        //                                foreach (XmlAttribute attribute in (XmlNamedNodeMap)xmlElement2.Attributes)
        //                                {
        //                                    if (attribute.Name == "Str")
        //                                        str2 = attribute.Value;
        //                                    if (attribute.Name == "Val")
        //                                        s = attribute.Value;
        //                                }
        //                                int result;
        //                                if (!string.IsNullOrEmpty(str2) && !string.IsNullOrEmpty(s) && int.TryParse(s, out result))
        //                                    tableEnumType.AddEnum(str2, result);
        //                            }
        //                        }
        //                    }
        //                    if (this.enumMap.ContainsKey(str1))
        //                        this.enumMap[str1] = tableEnumType;
        //                    else
        //                        this.enumMap.Add(str1, tableEnumType);
        //                }
        //            }
        //        }
        //    }
        //    foreach (XmlNode childNode in documentElement.ChildNodes)
        //    {
        //        if (childNode is XmlElement)
        //        {
        //            XmlElement xmlElement = (XmlElement)childNode;
        //            if (xmlElement.Name == "Colum")
        //            {
        //                string Name = "";
        //                string key = "";
        //                foreach (XmlAttribute attribute in (XmlNamedNodeMap)xmlElement.Attributes)
        //                {
        //                    if (attribute.Name == "Name")
        //                        Name = attribute.Value;
        //                    if (attribute.Name == "Type")
        //                        key = attribute.Value;
        //                }
        //                if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(key))
        //                {
        //                    switch (key)
        //                    {
        //                        case "int":
        //                            this.columns.Add(new TableColumn(Name, CellType._int));
        //                            ++this._int_colum_num;
        //                            continue;
        //                        case "float":
        //                            this.columns.Add(new TableColumn(Name, CellType._float));
        //                            ++this._float_colum_num;
        //                            continue;
        //                        case "bool":
        //                            this.columns.Add(new TableColumn(Name, CellType._bool));
        //                            ++this._bool_colum_num;
        //                            continue;
        //                        default:
        //                            if (this.enumMap.ContainsKey(key))
        //                            {
        //                                this.columns.Add(new TableColumn(Name, this.enumMap[key]));
        //                                ++this._int_colum_num;
        //                                continue;
        //                            }
        //                            continue;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
