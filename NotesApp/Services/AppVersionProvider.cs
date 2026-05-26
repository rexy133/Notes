using System;
using System.Reflection;

namespace NotesApp.Services
{
    /// <summary>
    /// Возвращает версию текущей сборки NotesApp.
    /// </summary>
    public class AppVersionProvider
    {
        public Version GetCurrentVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            if (version == null)
            {
                return new Version(1, 0, 0, 0);
            }

            return Normalize(version);
        }

        public string GetCurrentVersionText()
        {
            Version version = GetCurrentVersion();
            return version.Major + "." + version.Minor + "." + version.Build;
        }

        public static Version Normalize(Version version)
        {
            int build = version.Build < 0 ? 0 : version.Build;
            int revision = version.Revision < 0 ? 0 : version.Revision;

            return new Version(version.Major, version.Minor, build, revision);
        }
    }
}
