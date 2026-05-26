using System;

namespace NotesApp.Models
{
    public class WatcherDeviceRecord
    {
        public int Id { get; set; }
        public string DeviceUid { get; set; }
        public string NetworkAddress { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastContactAt { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
