using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1.Model.Inventory
{
    namespace Espis
    {
        public class Ewaste
        {
            public int EwasteID { get; set; }
            public string DeviceType { get; set; } 
            public string Brand { get; set; }
            public string Model { get; set; }
            public string Condition { get; set; }
            public double Weight { get; set; }

            public string ScrapStatus { get; set; }
            public DateTime? ScrapDate { get; set; }

            public bool ContainsStorage { get; set; }
            public string DataDestructionStatus { get; set; }

            public Ewaste(
                int ewasteID,
                string deviceType,
                string brand,
                string model,
                string condition,
                double weight)
            {
                EwasteID = ewasteID;
                DeviceType = deviceType;
                Brand = brand;
                Model = model;
                Condition = condition;
                Weight = weight;

                ScrapStatus = "Received";
                ScrapDate = null;
                ContainsStorage = false;
                DataDestructionStatus = "Not Required";
            }



            public virtual void DisplayInfo()
            {

            }




        }

    }
}
    

