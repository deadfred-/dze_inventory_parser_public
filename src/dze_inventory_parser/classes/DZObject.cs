using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace dze_inventory_parser.classes
{
    public class DZObject
    {
        public string className { get; set; }
        public int characterID { get; set; }
        public Int64 objectUID { get; set; }
        public int objectID { get; set; }
        public string worldSpace { get; set; }

        public string rawInventory { get; set; }
        public Inventory inventory { get; set; }

        // construct

        public DZObject()
        {
            this.inventory = new Inventory();
        }

    }

   public class Item
    {
        public string className { get; set; }
        public int count { get; set; }                     
    }

    public class Inventory
    {
        public List<Item> weaponItems { get; set; }
        public List<Item> magazineItems { get; set; }
        public List<Item> backpackItems { get; set; }

        // construct
        public Inventory()
        {
            weaponItems = new List<Item>();
            magazineItems = new List<Item>();
            backpackItems = new List<Item>();
        }

        public void ConvertInventoryString(string input)
        {
            try
            {
                string[] splits = Regex.Split(input, "]],", RegexOptions.IgnoreCase);
                string weapons = splits[0];
                string magazines = splits[1];
                string backpacks = splits[2];

                List<string> weaponClassNames = new List<string>();
                List<int> weaponCounts = new List<int>();

                List<string> magazineClassNames = new List<string>();
                List<int> magazineCounts = new List<int>();

                List<string> backpackClassNames = new List<string>();
                List<int> backpackCounts = new List<int>();

                // split out weapons
                string[] weaponSplits = Regex.Split(weapons, "],");
                foreach (string str in weaponSplits)
                {
                    // sanitize our input data
                    string tmpstr = str.Trim(new char[] { '[' });
                    tmpstr = tmpstr.Replace("\"", "");

                    // determine if we are dealing with classnames or counts
                    string[] tmpstrArr = tmpstr.Split(',');

                    // did we get results?
                    if (tmpstrArr.Count() > 0)
                    {
                        foreach (string val in tmpstrArr)
                        {
                            // attempt to convert to int.  If we fail, we have a classname
                            try
                            {
                                int count = Convert.ToInt32(val);
                                weaponCounts.Add(count);
                            }
                            catch (Exception)
                            {
                                // we got a string!
                                weaponClassNames.Add(val);
                            }
                        }
                    }
                }

                // split out magazines
                string[] magazineSplits = Regex.Split(magazines, "],");
                foreach (string str in magazineSplits)
                {
                    // sanitize our input data
                    string tmpstr = str.Trim(new char[] { '[' });
                    tmpstr = tmpstr.Replace("\"", "");

                    // determine if we are dealing with classnames or counts
                    string[] tmpstrArr = tmpstr.Split(',');

                    // did we get results?
                    if (tmpstrArr.Count() > 0)
                    {
                        foreach (string val in tmpstrArr)
                        {
                            // attempt to convert to int.  If we fail, we have a classname
                            try
                            {
                                int count = Convert.ToInt32(val);
                                magazineCounts.Add(count);
                            }
                            catch (Exception)
                            {
                                // we got a string!
                                magazineClassNames.Add(val);
                            }
                        }
                    }
                }

                // split out backpacks
                string[] backpackSplits = Regex.Split(backpacks, "],");
                foreach (string str in backpackSplits)
                {
                    // sanitize our input data
                    string tmpstr = str.Trim(new char[] { '[', ']' });
                    tmpstr = tmpstr.Replace("\"", "");

                    // determine if we are dealing with classnames or counts
                    string[] tmpstrArr = tmpstr.Split(',');

                    // did we get results?
                    if (tmpstrArr.Count() > 0)
                    {
                        foreach (string val in tmpstrArr)
                        {
                            // attempt to convert to int.  If we fail, we have a classname
                            try
                            {
                                int count = Convert.ToInt32(val);
                                backpackCounts.Add(count);
                            }
                            catch (Exception)
                            {
                                // we got a string!
                                backpackClassNames.Add(val);
                            }
                        }
                    }
                }

                // now lets use our objects to build a list of inventory items and their counts
                // do we have good data
                if (weaponClassNames.Count() == weaponCounts.Count())
                {
                    // add records to our obj
                    for (int a = 0; a < weaponClassNames.Count(); a++)
                    {
                        Item record = new Item();
                        record.className = weaponClassNames[a];
                        record.count = weaponCounts[a];

                        this.weaponItems.Add(record);
                    }
                }

                if (magazineClassNames.Count() == magazineCounts.Count())
                {
                    // add records to our obj
                    for (int a = 0; a < magazineClassNames.Count(); a++)
                    {
                        Item record = new Item();
                        record.className = magazineClassNames[a];
                        record.count = magazineCounts[a];

                        this.magazineItems.Add(record);
                    }
                }

                if (backpackClassNames.Count() == backpackCounts.Count())
                {
                    // add records to our obj
                    for (int a = 0; a < backpackClassNames.Count(); a++)
                    {
                        Item record = new Item();
                        record.className = backpackClassNames[a];
                        record.count = backpackCounts[a];

                        this.backpackItems.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
