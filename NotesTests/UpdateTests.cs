using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotesApp.Models;
using NotesApp.Services;

namespace NotesTests
{
    [TestClass]
    public class UpdateTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Проверяет решение об обновлении по версиям из XML.
        /// </summary>
        [TestMethod]
        [TestCategory("Updates")]
        [DataSource(
            "Microsoft.VisualStudio.TestTools.DataSource.XML",
            "TestData\\update-test-data.xml",
            "Case",
            DataAccessMethod.Sequential)]
        public void VersionComparison_DetectsUpdateDecision()
        {
            string currentVersionText = TestDataReader.GetString(TestContext, "CurrentVersion");
            string releaseTag = TestDataReader.GetString(TestContext, "ReleaseTag");
            string expected = TestDataReader.GetString(TestContext, "Expected");

            if (!TryParseReleaseVersion(releaseTag, out Version latestVersion))
            {
                Assert.AreEqual("invalid_tag", expected, Failure());
                return;
            }

            Version currentVersion = AppVersionProvider.Normalize(Version.Parse(currentVersionText));
            bool hasUpdate = latestVersion.CompareTo(currentVersion) > 0;
            string actual = hasUpdate ? "update" : "skip";

            Assert.AreEqual(expected, actual, Failure());
        }

        /// <summary>
        /// Проверяет чтение настроек обновления из notes.yml.
        /// </summary>
        [TestMethod]
        [TestCategory("Updates")]
        public void UpdateSettings_AreLoadedFromYaml()
        {
            UpdateSettings settings = new UpdateSettingsProvider().Load();

            Assert.AreEqual("rexy133", settings.Owner);
            Assert.AreEqual("Notes", settings.Repo);
            Assert.AreEqual(".zip", settings.AssetExtension);
            Assert.AreEqual(15, settings.HttpTimeoutSeconds);
        }

        /// <summary>
        /// Преобразует тег релиза GitHub в объект версии.
        /// </summary>
        /// <param name="tagName">Тег релиза, например v1.0.1.</param>
        /// <param name="version">Распознанная версия приложения.</param>
        private static bool TryParseReleaseVersion(string tagName, out Version version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            string versionText = tagName.Trim();
            if (versionText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionText = versionText.Substring(1);
            }

            int suffixIndex = versionText.IndexOf('-');
            if (suffixIndex > 0)
            {
                versionText = versionText.Substring(0, suffixIndex);
            }

            if (!Version.TryParse(versionText, out Version parsedVersion))
            {
                return false;
            }

            version = AppVersionProvider.Normalize(parsedVersion);
            return true;
        }

        /// <summary>
        /// Формирует сообщение об ошибке для текущего XML-сценария.
        /// </summary>
        private string Failure()
        {
            return "Ошибка в XML-сценарии обновлений: " + TestDataReader.GetCaseName(TestContext);
        }
    }
}
