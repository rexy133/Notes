using System;

namespace NotesApp.Models
{
    /// <summary>
    /// Запись журнала действий.
    /// </summary>
    public class AuditEventRecord
    {
        public int Id { get; set; }

        public int? AccountId { get; set; }

        public string AccountName { get; set; }

        public string ActionCode { get; set; }

        public string Details { get; set; }

        public string ObjectName { get; set; }

        public DateTime EventTime { get; set; }
    }
}
