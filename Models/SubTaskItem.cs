using SQLite;

namespace Todo_asa.Models
{
    /// <summary>
    /// Represents a subtask under a main task
    /// </summary>
    [Table("SubTasks")]
    public class SubTaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to parent task
        /// </summary>
        [Indexed]
        public int ParentTaskId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
