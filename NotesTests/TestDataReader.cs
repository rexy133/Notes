using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NotesTests
{
    public static class TestDataReader
    {
        /// <summary>
        /// Возвращает строковое значение поля из текущего XML-сценария.
        /// </summary>
        /// <param name="context">Контекст текущего теста.</param>
        /// <param name="fieldName">Название поля в XML.</param>
        public static string GetString(TestContext context, string fieldName)
        {
            object value = context.DataRow[fieldName];
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        /// <summary>
        /// Возвращает целочисленное значение поля из текущего XML-сценария.
        /// </summary>
        /// <param name="context">Контекст текущего теста.</param>
        /// <param name="fieldName">Название поля в XML.</param>
        public static int GetInt32(TestContext context, string fieldName)
        {
            return int.Parse(GetString(context, fieldName));
        }

        /// <summary>
        /// Возвращает логическое значение поля из текущего XML-сценария.
        /// </summary>
        /// <param name="context">Контекст текущего теста.</param>
        /// <param name="fieldName">Название поля в XML.</param>
        public static bool GetBoolean(TestContext context, string fieldName)
        {
            return bool.Parse(GetString(context, fieldName));
        }

        /// <summary>
        /// Возвращает имя текущего XML-сценария.
        /// </summary>
        /// <param name="context">Контекст текущего теста.</param>
        public static string GetCaseName(TestContext context)
        {
            return GetString(context, "Name");
        }
    }
}
