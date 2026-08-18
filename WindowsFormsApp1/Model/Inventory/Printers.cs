using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.Model.Inventory.Espis;

namespace WindowsFormsApp1.Model.Inventory
{
    public class Printer : Ewaste
    {
        public string PrinterType { get; set; }
        public string PrintingColor { get; set; }
        public string CartridgeType { get; set; }
        public int NumberOfCartridges { get; set; }
        public bool DuplexCapable { get; set; }
        public bool ScannerIncluded { get; set; }


        public Printer(
            int id,
            string brand,
            string model,
            string condition,
            double weight,
            string printerType,
            string printingColor,
            string cartridgeType,
            int numberOfCartridges,
            bool duplexCapable,
            bool scannerIncluded
)
            : base(id, "Printer", brand, model, condition, weight)
        {
            PrinterType = printerType;
            PrintingColor = printingColor;
            CartridgeType = cartridgeType;
            NumberOfCartridges = numberOfCartridges;
            DuplexCapable = duplexCapable;
            ScannerIncluded = scannerIncluded;

        }

        public override void DisplayInfo()
        {

        }
    }
}
