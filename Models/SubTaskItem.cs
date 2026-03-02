using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TaskFlow.Models
{
    /// <summary>
    /// Represents a subtask under a main task
    /// </summary>
    [Table("SubTasks")]
    public partial class SubTaskItem : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to parent task
        /// </summary>
        [Indexed]
        public int ParentTaskId { get; set; }

        /// <summary>
        /// Foreign key to parent subtask (0 means direct child of the task)
        /// </summary>
        [Indexed]
        public int ParentSubTaskId { get; set; } = 0;

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isCompleted = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Child sub-subtasks — not stored in DB, populated by service
        /// </summary>
        [Ignore]
        public List<SubTaskItem> Children { get; set; } = new();
    }
}
