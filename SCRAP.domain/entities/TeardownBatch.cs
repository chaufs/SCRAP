using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities
{
    public class TeardownBatch
    {
        public int Id { get; set; }
        public int ProcessedByUserId { get; set; }
        public int DeviceCategoryId { get; set; }
        public int QuantityDismantled { get; set; }
        public DateTime DateProcessed { get; set; }
        public int BranchId { get; set; }

        // Navigation
        public DeviceCategory? DeviceCategory { get; set; }
        public List<TeardownYield>? Yields { get; set; }
        public Branch? Branch { get; set; }
    }
}
