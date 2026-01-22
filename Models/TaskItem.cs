using SQLite;

namespace TaskFlow.Models
{
    /// <summary>
    /// Represents a main task item
    /// </summary>
    [Table("Tasks")]
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Navigation property - not stored in DB, populated by service
        /// </summary>
        [Ignore]
        public List<SubTaskItem> SubTasks { get; set; } = new();

        /// <summary>
        /// Calculated property for progress display
        /// </summary>
        [Ignore]
        public string StatusDisplay => Status switch
        {
            TaskStatus.ToDo => "To Do",
            TaskStatus.InProgress => "In Progress",
            TaskStatus.Completed => "Completed",
            _ => "Unknown"
        };

        /// <summary>
        /// Calculated property for due date display
        /// </summary>
        [Ignore]
        public string DueDateDisplay 
        {
            get
            {
                if (!DueDate.HasValue) return "No due date";
                var dateStr = DueDate.Value.ToString("MMM dd, yyyy");
                
                if (Status == Models.TaskStatus.Completed || IsOverdue)
                    return dateStr;

                // Calculate time until end of due date (23:59:59)
                var deadline = DueDate.Value.Date.AddDays(1).AddTicks(-1);
                var diff = deadline - DateTime.Now;

                if (diff.TotalSeconds <= 0) return dateStr;

                string remaining;
                if (diff.TotalDays >= 1)
                {
                    // For > 24h, show days (rounding up to ensure "1.5 days" is treated safe, or just casting)
                    // Let's use simple casting to match "1d 10h" conceptually
                    remaining = $"{(int)diff.TotalDays}d";
                }
                else if (diff.TotalHours >= 1)
                {
                    remaining = $"{(int)diff.TotalHours}h";
                }
                else
                {
                    remaining = $"{(int)diff.TotalMinutes}m";
                }

                return $"{dateStr} - {remaining}";
            }
        }

        /// <summary>
        /// Check if task is overdue
        /// </summary>
        [Ignore]
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Now.Date && Status != TaskStatus.Completed;

        /// <summary>
        /// Subtask completion progress text
        /// </summary>
        [Ignore]
        public string SubTaskProgress
        {
            get
            {
                if (SubTasks.Count == 0) return string.Empty;
                var completed = SubTasks.Count(s => s.IsCompleted);
                return $"{completed}/{SubTasks.Count}";
            }
        }
    }
}
