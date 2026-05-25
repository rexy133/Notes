using System;
using System.IO;
using System.Management;
using Watcher.Models;

namespace Watcher.Services
{
    public class MetricCollector
    {
        public WatcherMetric Collect()
        {
            string computerName = Environment.MachineName;

            return new WatcherMetric
            {
                DeviceUid = computerName,
                DisplayName = computerName,
                CpuLoad = ReadCpuLoad(),
                RamLoad = ReadRamLoad(),
                DiskLoad = ReadDiskLoad(),
                CapturedAt = DateTime.Now
            };
        }

        private static decimal ReadCpuLoad()
        {
            decimal totalLoad = 0;
            int processorCount = 0;

            using (ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"))
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    object value = item["LoadPercentage"];
                    if (value == null)
                    {
                        continue;
                    }

                    totalLoad += Convert.ToDecimal(value);
                    processorCount++;
                }
            }

            if (processorCount == 0)
            {
                return 0;
            }

            return NormalizePercent(totalLoad / processorCount);
        }

        private static decimal ReadRamLoad()
        {
            using (ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    decimal totalMemory = Convert.ToDecimal(item["TotalVisibleMemorySize"]);
                    decimal freeMemory = Convert.ToDecimal(item["FreePhysicalMemory"]);

                    if (totalMemory <= 0)
                    {
                        return 0;
                    }

                    return NormalizePercent((totalMemory - freeMemory) * 100 / totalMemory);
                }
            }

            return 0;
        }

        private static decimal ReadDiskLoad()
        {
            decimal totalSize = 0;
            decimal freeSize = 0;

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                {
                    continue;
                }

                totalSize += drive.TotalSize;
                freeSize += drive.AvailableFreeSpace;
            }

            if (totalSize <= 0)
            {
                return 0;
            }

            return NormalizePercent((totalSize - freeSize) * 100 / totalSize);
        }

        private static decimal NormalizePercent(decimal value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return Math.Round(value, 2);
        }
    }
}
