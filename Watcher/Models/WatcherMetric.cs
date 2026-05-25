using System;

namespace Watcher.Models
{
    public class WatcherMetric
    {
        public string DeviceUid { get; set; }
        public string DisplayName { get; set; }
        public decimal CpuLoad { get; set; }
        public decimal RamLoad { get; set; }
        public decimal DiskLoad { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
