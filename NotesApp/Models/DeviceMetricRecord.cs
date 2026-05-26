using System;

namespace NotesApp.Models
{
    public class DeviceMetricRecord
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public decimal CpuLoad { get; set; }
        public decimal RamLoad { get; set; }
        public decimal DiskLoad { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
