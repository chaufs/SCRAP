using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.Model.Inventory.Espis;

namespace WindowsFormsApp1.Model.Inventory
{

    public class Laptop : Ewaste
    {
        public double ScreenSize { get; set; }
        public string RAMCapacity { get; set; }
        public string StorageType { get; set; }
        public string StorageCapacity { get; set; }
        public string BatteryType { get; set; }
        public string ProcessorType { get; set; }
        public string GPUType { get; set; }
        public bool ChargerIncluded { get; set; }

        public Laptop(
            int id,
            string brand,
            string model,
            string condition,
            double weight,
            double screenSize,
            string ramCapacity,
            string storageType,
            string storageCapacity,
            string batteryType,
            string processorType,
            string gpuType,
            bool chargerIncluded)
            : base(id, "Laptop", brand, model, condition, weight)
        {
            ScreenSize = screenSize;
            RAMCapacity = ramCapacity;
            StorageType = storageType;
            StorageCapacity = storageCapacity;
            BatteryType = batteryType;
            ProcessorType = processorType;
            GPUType = gpuType;
            ChargerIncluded = chargerIncluded;

        }

        public override void DisplayInfo()
        {

        }
    }
}

