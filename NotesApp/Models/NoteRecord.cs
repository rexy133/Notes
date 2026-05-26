using System;

namespace NotesApp.Models
{
    /// <summary>
    /// Заметка пользователя.
    /// </summary>
    public class NoteRecord
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public string OwnerUsername { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }
    }
}
