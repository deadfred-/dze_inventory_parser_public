using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using dze_inventory_parser.classes;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySql.Data;
using MySql.Data.Common;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;


namespace dze_inventory_parser
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Dictionary<string, string> ArgPairs = new Dictionary<string, string>();



            // Draw some neat graphics!
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("*******************************************************************************");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("DayZ Epoch Inventory Analysis Tool copyright (c) 2014 Hambeast aka Deadfred666");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY!  Support Free,Open Software!");
            Console.WriteLine("Usage: dze_inventory_parser.exe --configfile=config.xml");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("*******************************************************************************");
            Console.ForegroundColor = ConsoleColor.Gray;

            // load data from config file
            Config cfg = new Config();
#if DEBUG
            if (!cfg.GetSettingsFromXML("config.xml"))
            {
                // error
                return 1;
            }
#else
            if (args.Length == 1)
            {
                string cfgFile = string.Empty;
                string arg1 = args[0];
                // validate we have arguments
                if (arg1.Contains("--configfile=") && arg1.Contains(".xml"))
                {
                    string[] ArgSplit = arg1.Split('=');
                    if (ArgSplit.Length == 2)
                    {
                        // get cfg file
                        cfgFile = ArgSplit[1];

                        // get data from cfg file
                        if (!cfg.GetSettingsFromXML(cfgFile))
                        {
                            // error
                            return 1;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Bad args... try again...");
                return 1;
            }
#endif

            // master list of objects
            List<DZObject> masterObjectList = new List<DZObject>();

            // set up some flags to catch bad stuff
            List<Item> badItems = new List<Item>();
            badItems = cfg.redFlagItems;

            string connStr = string.Format("server={0};user={1};database={2};port={3};password={4};", cfg.dbIP, cfg.dbUserName, cfg.dbDatabase, cfg.dbPort, cfg.dbPassword);
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader rdr;

            string queryString = "SELECT objectUID, ObjectID, worldspace, classname, characterID, inventory FROM object_data OD WHERE 1=1 and OD.inventory NOT LIKE '[[[],[]],[[],[]],[[],[]]]' AND OD.inventory NOT LIKE '[]'";

            if (cfg.useRedFlagList)
            {
                // ad our bad items to query string. We don't need to query for items not in the bad list.
                string appendQuery = " AND ( ";
                int queryCounter = 0;
                foreach (Item badItem in badItems)
                {
                    if (queryCounter < 1)
                    {
                        appendQuery += string.Format(" od.inventory like '%{0}%' ", badItem.className.ToString());
                    }
                    else
                    {
                        appendQuery += string.Format(" OR od.inventory like '%{0}%' ", badItem.className.ToString());
                    }
                    queryCounter++;
                }
                queryString += appendQuery;
                queryString += ")";
            }


            try
            {
                // open connection
                conn.Open();

                // set cmd up
                cmd.Connection = conn;
                cmd.CommandText = queryString;
                cmd.CommandType = CommandType.Text;

                int a = 0;
                // get data
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    DZObject record = new DZObject();

                    record.className = rdr["classname"].ToString();
                    record.characterID = Convert.ToInt32(rdr["characterID"]);
                    record.rawInventory = rdr["inventory"].ToString();
                    record.objectID = Convert.ToInt32(rdr["objectID"]);
                    record.objectUID = Convert.ToInt64(rdr["objectUID"]);
                    record.worldSpace = rdr["worldspace"].ToString();

                    masterObjectList.Add(record);

                    a++;
                    Console.Title = (string.Format("Record scanned {0}", a));
                }
                // close connection
                conn.Close();
            }
            catch (Exception ex)
            {
                //throw ex;
                Console.WriteLine("Something is wrong with your DB connection string...");
                Console.WriteLine(ex);
                return 1;
                // do nothing
            }

            // if we have objects parse them
            Console.WriteLine("DB Objects Found:\t\t{0}", masterObjectList.Count());

            if (masterObjectList.Count() > 0)
            {
                // iterate thru our objs
                int b = 0;
                foreach (DZObject record in masterObjectList)
                {
                    record.inventory = new Inventory();

                    b++;
                    Console.Title = (string.Format("Converting object: {0}", b));
                    record.inventory.ConvertInventoryString(record.rawInventory);
                }
            }

            // Lets loop through our recorded items and figure out if any of our 
            // parsed data meets the criteria.  If it does, save it

            List<DZObject> suspiciousObjects = new List<DZObject>();

            int c = 0;

            if (cfg.useRedFlagList)
            {

                foreach (DZObject obj in masterObjectList)
                {
                    c++;
                    Console.Title = (string.Format("Checking parsed item: {0}", c));

                    // make sure we have stuff in our obj
                    if (obj.inventory.magazineItems.Count() > 0 && obj.inventory.weaponItems.Count() > 0)
                    {
                        // create temp holding obj
                        DZObject tmpObj = new DZObject();

                        tmpObj.characterID = obj.characterID;
                        tmpObj.className = obj.className;
                        tmpObj.objectID = obj.objectID;
                        tmpObj.objectUID = obj.objectUID;
                        tmpObj.worldSpace = obj.worldSpace;
                        if (cfg.preserveRawInventory)
                        {
                            tmpObj.rawInventory = obj.rawInventory;
                        }

                        // Check to see if our mags show up in the bad list and if they are above the limit
                        var badMagItems = from m in obj.inventory.magazineItems
                                          join b in badItems on m.className equals b.className
                                          where m.count >= b.count
                                          select m;

                        // add our badMagItems to our temp obj
                        foreach (Item record in badMagItems)
                        {
                            tmpObj.inventory.magazineItems.Add(record);
                        }

                        // Check to see if our weapons show up in the bad list and if they are above the limit
                        var badWeapItems = from m in obj.inventory.weaponItems
                                           join b in badItems on m.className equals b.className
                                           where m.count >= b.count
                                           select m;

                        // add our badWeapons to our temp obj
                        foreach (Item record in badWeapItems)
                        {
                            tmpObj.inventory.weaponItems.Add(record);
                        }

                        // if our tmp obj contains item records, then there are flags, add it to our master list
                        if (tmpObj.inventory.magazineItems.Count() > 0 || tmpObj.inventory.weaponItems.Count() > 0)
                        {
                            suspiciousObjects.Add(tmpObj);
                        }
                    }
                }

            }
            else
            {
                // grab everything...         
                suspiciousObjects = masterObjectList;
            }

            Console.WriteLine("Suspicious DB Objects:\t{0}", suspiciousObjects.Count());

            // check to see if we have suspicious items and log to file
            if (suspiciousObjects.Count() > 0)
            {
                Console.Title = "Writing Output File";
                switch (cfg.selectedOutputType)
                {
                    case Config.outputTypes.XML:
                        try
                        {
                            // for now we are going to XML
                            XmlSerializer xs = new XmlSerializer(suspiciousObjects.GetType());
                            StreamWriter writer = new StreamWriter("output.xml");
                            xs.Serialize(writer, suspiciousObjects);
                            writer.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.Format("Something Went Wrong Writing the Output File to XML!: {0}", ex));
                        }
                        break;
                    case Config.outputTypes.CSV:
                        try
                        {
                            List<string> csvLines = ConvertDZObjectsToString(suspiciousObjects, cfg);
                            // open stream
                            StreamWriter writer = new StreamWriter("output.csv");

                            foreach (string line in csvLines)
                            {
                                // write lines
                                writer.WriteLine(line);
                            }
                            // close stream
                            writer.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.Format("Something Went Wrong Writing the Output File to CSV!: {0}", ex));
                        }
                        break;
                }
            }


            
            if (cfg.drawGraph)
            {
                // generate ds
                DataSet ds = new DataSet();
                DataTable dt = new DataTable();

                dt.Columns.Add("ClassName", typeof(string));
                dt.Columns.Add("Quantity", typeof(int));

                Dictionary<string, int> countsOfObjs = new Dictionary<string, int>();
                countsOfObjs = GetCountsOfAllObjects(suspiciousObjects);

                // order our list.
                var sortedObjs = from entry in countsOfObjs 
                                 orderby entry.Value descending 
                                 select entry;

                // get the top 10 records
                var top10Objs = sortedObjs.Take(cfg.drawGraphLimitRecords);


                foreach (KeyValuePair<string, int> obj in top10Objs)
                {
                    DataRow dr = dt.NewRow();

                    dr[0] = obj.Key + "\n" + obj.Value.ToString();
                    dr[1] = obj.Value;

                    dt.Rows.Add(dr);
                }

                ds.Tables.Add(dt);

                // create the chart
                Chart chart = new Chart();
                chart.DataSource = ds.Tables[0];
                chart.Width = 1024;
                chart.Height = 768;
                    

                // define series
                Series serie1 = new Series();
                serie1.Name = "Serie1";
                serie1.XValueMember = "ClassName";
                serie1.YValueMembers = "Quantity";
                serie1.ChartType = SeriesChartType.Pie;

                

                // define legend
                Legend legend = new Legend();
                legend.Name = "Legend1";

                serie1.Legend = "Legend1";
                //serie1.LegendText = serie1.XValueMember;

                // add legend
                chart.Legends.Add(legend);

                // add series to chart
                chart.Series.Add(serie1);
                
                // define chartarea
                ChartArea ca = new ChartArea();
                ca.Name = "CA1";
                ca.AxisX = new Axis();
                ca.AxisY = new Axis();                
                ca.Area3DStyle.Enable3D = true;
                

                //add ca
                chart.ChartAreas.Add(ca);

                // databind 
                chart.DataBind();

                // save result
                string chartName = string.Format("{0}.png",cfg.dbDatabase);
                chart.SaveImage(chartName, ChartImageFormat.Png);
            }
            // program is done.
            return 0;
        }


        public static Dictionary<string, int> GetCountsOfAllObjects(List<DZObject> objList)
        {
            Dictionary<string, int> returnValue = new Dictionary<string, int>();

            int objCounter = 0;
            foreach (DZObject dzObj in objList)
            {
                objCounter++;
                // combine the items into one list to simplify processing
                List<Item> items = new List<Item>();
                items.AddRange(dzObj.inventory.weaponItems);
                items.AddRange(dzObj.inventory.magazineItems);
                items.AddRange(dzObj.inventory.backpackItems);

                // parse lines for combined items
                int itemCounter = 0;
                foreach (Item dzItem in items)
                {
                    Console.Title = string.Format("Counting Items.  Obj: {0} item: {1}",objCounter.ToString(), itemCounter.ToString());

                    try
                    {
                        returnValue.Add(dzItem.className, dzItem.count);
                    }
                    catch (Exception ex)
                    {
                        returnValue[dzItem.className] = returnValue[dzItem.className] + dzItem.count;
                    }
                   itemCounter++;
                }
            }
            return returnValue;
        }


        public static List<string> ConvertDZObjectsToString(List<DZObject> objList, Config cfg)
        {
            List<string> returnValue = new List<string>();

            // make header line
            string headerLine = "ClassName\tCharacterID\tObjectUID\tObjectID\tWorldSpace\tItem\tItemCount\tRawInventory";
            if (cfg.preserveRawInventory)
            {
                headerLine += ",rawInventory";
            }
            // add header to our list
            returnValue.Add(headerLine);

            foreach (DZObject dzObj in objList)
            {
                // combine the items into one list to simplify processing
                List<Item> items = new List<Item>();
                items.AddRange(dzObj.inventory.weaponItems);
                items.AddRange(dzObj.inventory.magazineItems);
                items.AddRange(dzObj.inventory.backpackItems);

                // parse lines for combined items
                foreach (Item dzItem in items)
                {
                    string line = string.Empty;
                    if (cfg.preserveRawInventory)
                    {
                        line += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                            dzObj.className.ToString()
                            , dzObj.characterID.ToString()
                            , dzObj.objectUID.ToString()
                            , dzObj.objectID.ToString()
                            , dzObj.worldSpace.ToString()
                            , dzItem.className.ToString()
                            , dzItem.count.ToString()
                            , dzObj.rawInventory.ToString()
                            );
                        // add to list
                        returnValue.Add(line);
                    }
                    else
                    {
                        line += string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                            dzObj.className.ToString()
                            , dzObj.characterID.ToString()
                            , dzObj.objectUID.ToString()
                            , dzObj.objectID.ToString()
                            , dzObj.worldSpace.ToString()
                            , dzItem.className.ToString()
                            , dzItem.count.ToString()
                            , ""
                            );
                        // add to list
                        returnValue.Add(line);
                    }
                }
            }
            return returnValue;
        }
    }
}
