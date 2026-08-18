using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.Model.Inventory.Espis;
namespace WindowsFormsApp1.Model.Inventory
{
    public class Computer : Ewaste
    {
        public string FormFactor { get; set; }
        public string ProcessorType { get; set; }
        public string RAMCapacity { get; set; }
        public string StorageType { get; set; }
        public string StorageCapacity { get; set; }
        public string GPUType { get; set; }
        public int PowerSupplyWattage { get; set; }
        public string MotherboardType { get; set; }

        public Computer(
            int id,
            string brand,
            string model,
            string condition,
            double weight,
            string formFactor,
            string processorType,
            string ramCapacity,
            string storageType,
            string storageCapacity,
            string gpuType,
            int powerSupplyWattage,
            string motherboardType)
            : base(id, "Computer", brand, model, condition, weight)
        {
            FormFactor = formFactor;
            ProcessorType = processorType;
            RAMCapacity = ramCapacity;
            StorageType = storageType;
            StorageCapacity = storageCapacity;
            GPUType = gpuType;
            PowerSupplyWattage = powerSupplyWattage;
            MotherboardType = motherboardType;



        }

        public override void DisplayInfo()
        {
        }
    }
}
