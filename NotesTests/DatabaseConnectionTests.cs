using System;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace NotesTests
{
    [TestClass]
    public class DatabaseConnectionTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Проверяет успешные и ошибочные сценарии подключения к PostgreSQL.
        /// </summary>
        [TestMethod]
        [TestCategory("Database")]
        [DataSource(
            "Microsoft.VisualStudio.TestTools.DataSource.XML",
            "TestData\\db-connection-test-data.xml",
            "Case",
            DataAccessMethod.Sequential)]
        public void OpenConnection_ChecksPostgreSqlConnectionScenarios()
        {
            string expected = TestDataReader.GetString(TestContext, "Expected");
            string expectedSqlState = TestDataReader.GetString(TestContext, "ExpectedSqlState");
            string connectionString = BuildConnectionString();

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    Assert.AreEqual("success", expected, Failure());
                    Assert.AreEqual(System.Data.ConnectionState.Open, connection.State, Failure());
                }
            }
            catch (PostgresException ex)
            {
                Assert.AreEqual("postgres_error", expected, Failure());
                Assert.AreEqual(expectedSqlState, ex.SqlState, Failure());
            }
            catch (Exception ex)
            {
                Assert.AreEqual("network_error", expected, Failure() + " Фактическая ошибка: " + ex.GetType().Name);
                Assert.IsTrue(IsNetworkError(ex), Failure());
            }
        }

        /// <summary>
        /// Собирает строку подключения из текущего XML-сценария.
        /// </summary>
        private string BuildConnectionString()
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                Host = TestDataReader.GetString(TestContext, "Host"),
                Port = TestDataReader.GetInt32(TestContext, "Port"),
                Database = TestDataReader.GetString(TestContext, "Database"),
                Username = TestDataReader.GetString(TestContext, "Username"),
                Password = TestDataReader.GetString(TestContext, "Password"),
                Timeout = 2
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Проверяет, относится ли исключение к сетевым ошибкам подключения.
        /// </summary>
        /// <param name="ex">Исключение, полученное при подключении.</param>
        private static bool IsNetworkError(Exception ex)
        {
            if (ex is TimeoutException || ex is SocketException || ex is NpgsqlException)
            {
                return true;
            }

            return ex.InnerException != null && IsNetworkError(ex.InnerException);
        }

        /// <summary>
        /// Формирует сообщение об ошибке для текущего XML-сценария.
        /// </summary>
        private string Failure()
        {
            return "Ошибка в XML-сценарии подключения к БД: " + TestDataReader.GetCaseName(TestContext);
        }
    }
}
