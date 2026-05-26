using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotesApp.Models;
using NotesApp.Services;

namespace NotesTests
{
    [TestClass]
    public class NoteRepositoryTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Подготавливает тестовую базу перед каждым сценарием работы с заметками.
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            TestDatabase.Reset();
        }

        /// <summary>
        /// Проверяет операции создания, чтения, изменения и удаления заметок.
        /// </summary>
        [TestMethod]
        [TestCategory("Notes")]
        [DataSource(
            "Microsoft.VisualStudio.TestTools.DataSource.XML",
            "TestData\\note-test-data.xml",
            "Case",
            DataAccessMethod.Sequential)]
        public void NoteOperations_WorkWithPostgreSqlStorage()
        {
            string operation = TestDataReader.GetString(TestContext, "Operation");
            string content = TestDataReader.GetString(TestContext, "Content");
            string newContent = TestDataReader.GetString(TestContext, "NewContent");

            NoteService noteService = new NoteService(new DbConnectionProvider());
            AppUser user = TestDatabase.GetUser("test_user");
            AppUser admin = TestDatabase.GetUser("test_admin");

            if (operation == "create")
            {
                NoteRecord note = noteService.AddNote(user, content);

                Assert.IsTrue(note.Id > 0, Failure());
                Assert.AreEqual(user.Id, note.OwnerId, Failure());
                Assert.AreEqual(content, note.Content, Failure());
            }
            else if (operation == "list")
            {
                noteService.AddNote(user, content);
                List<NoteRecord> notes = noteService.GetNotes(user);

                Assert.AreEqual(1, notes.Count, Failure());
                Assert.AreEqual(content, notes[0].Content, Failure());
            }
            else if (operation == "update")
            {
                NoteRecord note = noteService.AddNote(user, content);
                bool updated = noteService.UpdateNote(user, note.Id, newContent);
                List<NoteRecord> notes = noteService.GetNotes(user);

                Assert.IsTrue(updated, Failure());
                Assert.AreEqual(newContent, notes[0].Content, Failure());
            }
            else if (operation == "delete_existing")
            {
                NoteRecord note = noteService.AddNote(user, content);
                bool deleted = noteService.DeleteNote(user, note.Id);
                List<NoteRecord> notes = noteService.GetNotes(user);

                Assert.IsTrue(deleted, Failure());
                Assert.AreEqual(0, notes.Count, Failure());
            }
            else if (operation == "delete_missing")
            {
                bool deleted = noteService.DeleteNote(user, 999999);

                Assert.IsFalse(deleted, Failure());
            }
            else if (operation == "admin_read")
            {
                noteService.AddNote(user, content);
                List<NoteRecord> notes = noteService.GetUserNotes(admin, user.Username);

                Assert.AreEqual(1, notes.Count, Failure());
                Assert.AreEqual(user.Username, notes[0].OwnerUsername, Failure());
                Assert.AreEqual(content, notes[0].Content, Failure());
            }
            else
            {
                Assert.Fail("Неизвестная операция в XML: " + operation);
            }
        }

        /// <summary>
        /// Формирует сообщение об ошибке для текущего XML-сценария.
        /// </summary>
        private string Failure()
        {
            return "Ошибка в XML-сценарии заметок: " + TestDataReader.GetCaseName(TestContext);
        }
    }
}
