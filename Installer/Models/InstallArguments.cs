using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer.Models
{
    public class InstallArguments
    {
        public string ArchivePath { get; set; }
        public string ApplicationFolder { get; set; }
        public string RestartExeName { get; set; }
    }
}
