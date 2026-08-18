using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.Model.Inventory.SCRAP;
using WindowsFormsApp1.Model.Inventory.Espis;
namespace WindowsFormsApp1.Model.Inventory
{
    public class Phone : Ewaste
    {
        public string OS { get; set; }
        public double ScreenSize { get; set; }
        public string RAMCapacity { get; set; }
        public string StorageCapacity { get; set; }
        public string BatteryType { get; set; }
        public string BatteryCapacity { get; set; }
        public string NetworkType { get; set; }
        public string IMEI { get; set; }

        public Phone(
            int id,
            string brand,
            string model,
            string condition,
            double weight,
            string os,
            double screenSize,
            string ramCapacity,
            string storageCapacity,
            string batteryType,
            string batteryCapacity,
            string networkType,
            string imei)
            : base(id, "Phone", brand, model, condition, weight)
        {
            OS = os;
            ScreenSize = screenSize;
            RAMCapacity = ramCapacity;
            StorageCapacity = storageCapacity;
            BatteryType = batteryType;
            BatteryCapacity = batteryCapacity;
            NetworkType = networkType;
            IMEI = imei;

        }

        public override void DisplayInfo()
        {
            base.DisplayInfo();

            Console.WriteLine($"OS: {OS}");
            Console.WriteLine($"Screen Size: {ScreenSize}\"");
            Console.WriteLine($"RAM: {RAMCapacity}");
            Console.WriteLine($"Storage: {StorageCapacity}");
            Console.WriteLine($"Battery: {BatteryType}");
            Console.WriteLine($"Battery Capacity: {BatteryCapacity}");
            Console.WriteLine($"Network: {NetworkType}");
            Console.WriteLine($"IMEI: {IMEI}");
        }
    }
}
