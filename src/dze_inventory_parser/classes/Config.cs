using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace dze_inventory_parser.classes
{
    public class Config
    {
        // TODO: add multi db support
        // track our db's we wish to use 
        public string serverName { get; set; }

        // db config properties
        public string dbIP { get; set; }
        public string dbPort { get; set; }
        public string dbDatabase { get; set; }
        public string dbUserName { get; set; }
        public string dbPassword { get; set; }

        // output config properties        
        public enum outputTypes
        {
            XML,
            CSV            
        }
        public bool preserveRawInventory { get; set; }
        
        public outputTypes selectedOutputType { get; set; }
        public bool useRedFlagList { get; set; }
        public bool drawGraph { get; set; }
        public int drawGraphLimitRecords { get; set; }

        // red flag item properties
        public List<Item> redFlagItems = new List<Item>();

        
        public Boolean GetSettingsFromXML(string cfgFilePath)
        {
            Boolean returnValue = true;
            XmlDocument xd = new XmlDocument();

            // load XML file
            try
            {
                xd.Load(cfgFilePath);
                XmlNodeList dbNodes = xd.SelectNodes("config/DB");

                // itterate through db nodes
                foreach (XmlNode dbNode in dbNodes)
                {
                    foreach (XmlNode optNode in dbNode)
                    {
                        switch (optNode.Name)
                        {
                            case "ip":
                                this.dbIP = optNode.InnerText.ToString();
                                break;
                            case "port":
                                this.dbPort = optNode.InnerText.ToString();
                                break;
                            case "database":
                                this.dbDatabase = optNode.InnerText.ToString();
                                break;
                            case "username":
                                this.dbUserName = optNode.InnerText.ToString();
                                break;
                            case "password":
                                this.dbPassword = optNode.InnerText.ToString();
                                break;

                        }
                    }
                }

                XmlNodeList outputNodes = xd.SelectNodes("config/Output");

                // itterate through db nodes
                foreach (XmlNode outputNode in outputNodes)
                {
                    foreach (XmlNode optNode in outputNode)
                    {
                        switch (optNode.Name)
                        {

                            case "UseRedFlagList":
                                this.useRedFlagList = Convert.ToBoolean(optNode.InnerText.ToString());
                                break;
                            case "PreserveRawInventory":
                                this.preserveRawInventory = Convert.ToBoolean(optNode.InnerText.ToString());
                                break;
                            case "DrawGraph":
                                this.drawGraph = Convert.ToBoolean(optNode.InnerText.ToString());
                                break;
                            case "DrawGraphLimitRecords":
                                this.drawGraphLimitRecords = Convert.ToInt32(optNode.InnerText.ToString());
                                break;
                            case "OutputType":
                                switch (optNode.InnerText.ToUpper().ToString())
                                { 
                                    case "XML":
                                        this.selectedOutputType = outputTypes.XML;
                                        break;
                                    case "CSV":
                                        this.selectedOutputType = outputTypes.CSV;
                                        break;
                                }
                                break;

                        }
                    }
                }

                XmlNodeList redFlagNodes = xd.SelectNodes("config/RedFlagList");
                // itterate through red flag nodes
                foreach (XmlNode redFlagNode in redFlagNodes)
                {
                    foreach (XmlNode itemNodes in redFlagNode)
                    {
                        foreach (XmlNode itemNode in itemNodes)
                        {
                            switch (itemNode.Name)
                            {
                                case "Item":
                                    try
                                    {
                                        // attempt to create a new item based on CFG
                                        Item rfItem = new Item();
                                        rfItem.className = itemNode.Attributes["ClassName"].Value.ToString();
                                        rfItem.count = Convert.ToInt32(itemNode.InnerText.ToString());

                                        // add to our collection
                                        redFlagItems.Add(rfItem);

                                    }
                                    catch (Exception ex)
                                    {
                                        // we have a bad node here... throw an error.
                                        Console.WriteLine(string.Format("Error parsing item node: {0}", ex));
                                        return false;
                                    }
                                    break;
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error loading CFG File: {0}", ex));
                return false;
            }
            return returnValue;
        }
    }
}
