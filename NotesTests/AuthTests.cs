using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotesApp.Models;
using NotesApp.Services;

namespace NotesTests
{
    [TestClass]
    public class AuthTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Подготавливает тестовую базу перед каждым сценарием авторизации.
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            TestDatabase.Reset();
        }

        /// <summary>
        /// Проверяет вход пользователей по наборам данных из XML.
        /// </summary>
        [TestMethod]
        [TestCategory("Authorization")]
        [DataSource(
            "Microsoft.VisualStudio.TestTools.DataSource.XML",
            "TestData\\auth-test-data.xml",
            "Case",
            DataAccessMethod.Sequential)]
        public void Login_ChecksUserCredentialsInPostgreSql()
        {
            string username = TestDataReader.GetString(TestContext, "Username");
            string password = TestDataReader.GetString(TestContext, "Password");
            bool expectedSuccess = TestDataReader.GetBoolean(TestContext, "ExpectedSuccess");
            string expectedRole = TestDataReader.GetString(TestContext, "ExpectedRole");

            AuthService authService = new AuthService(new DbConnectionProvider());
            AuthResult result = authService.Login(username, password);

            Assert.AreEqual(expectedSuccess, result.Success, Failure());
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), Failure());

            if (expectedSuccess)
            {
                Assert.IsNotNull(result.User, Failure());
                Assert.AreEqual(username, result.User.Username, Failure());
                Assert.AreEqual(expectedRole, result.User.RoleCode, Failure());
            }
            else
            {
                Assert.IsNull(result.User, Failure());
            }
        }

        /// <summary>
        /// Формирует сообщение об ошибке для текущего XML-сценария.
        /// </summary>
        private string Failure()
        {
            return "Ошибка в XML-сценарии авторизации: " + TestDataReader.GetCaseName(TestContext);
        }
    }
}
