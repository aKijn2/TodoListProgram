using SQLite;
using Todo_asa.Models;

namespace Todo_asa.Services
{
    /// <summary>
    /// SQLite database service for local data persistence
    /// </summary>
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "todo_asa.db3");
        }

        /// <summary>
        /// Initialize the database connection and create tables
        /// </summary>
        private async Task InitAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _database.CreateTableAsync<TaskItem>();
            await _database.CreateTableAsync<SubTaskItem>();
        }

        #region Task Operations

        /// <summary>
        /// Get all tasks with their subtasks
        /// </summary>
        public async Task<List<TaskItem>> GetTasksAsync()
        {
            await InitAsync();
            var tasks = await _database!.Table<TaskItem>().OrderByDescending(t => t.CreatedAt).ToListAsync();
            
            foreach (var task in tasks)
            {
                task.SubTasks = await GetSubTasksAsync(task.Id);
            }
            
            return tasks;
        }

        /// <summary>
        /// Get a single task by ID with subtasks
        /// </summary>
        public async Task<TaskItem?> GetTaskAsync(int id)
        {
            await InitAsync();
            var task = await _database!.Table<TaskItem>().FirstOrDefaultAsync(t => t.Id == id);
            
            if (task != null)
            {
                task.SubTasks = await GetSubTasksAsync(task.Id);
            }
            
            return task;
        }

        /// <summary>
        /// Get tasks filtered by status
        /// </summary>
        public async Task<List<TaskItem>> GetTasksByStatusAsync(Models.TaskStatus status)
        {
            await InitAsync();
            var tasks = await _database!.Table<TaskItem>()
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            
            foreach (var task in tasks)
            {
                task.SubTasks = await GetSubTasksAsync(task.Id);
            }
            
            return tasks;
        }

        /// <summary>
        /// Save a task (insert or update)
        /// </summary>
        public async Task<int> SaveTaskAsync(TaskItem task)
        {
            await InitAsync();
            task.UpdatedAt = DateTime.Now;

            if (task.Id != 0)
            {
                await _database!.UpdateAsync(task);
                return task.Id;
            }
            else
            {
                task.CreatedAt = DateTime.Now;
                await _database!.InsertAsync(task);
                return task.Id;
            }
        }

        /// <summary>
        /// Delete a task and all its subtasks
        /// </summary>
        public async Task<int> DeleteTaskAsync(TaskItem task)
        {
            await InitAsync();
            // Delete all subtasks first
            await _database!.ExecuteAsync("DELETE FROM SubTasks WHERE ParentTaskId = ?", task.Id);
            return await _database.DeleteAsync(task);
        }

        #endregion

        #region SubTask Operations

        /// <summary>
        /// Get subtasks for a task
        /// </summary>
        public async Task<List<SubTaskItem>> GetSubTasksAsync(int taskId)
        {
            await InitAsync();
            return await _database!.Table<SubTaskItem>()
                .Where(s => s.ParentTaskId == taskId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Save a subtask (insert or update)
        /// </summary>
        public async Task<int> SaveSubTaskAsync(SubTaskItem subTask)
        {
            await InitAsync();

            if (subTask.Id != 0)
            {
                await _database!.UpdateAsync(subTask);
                return subTask.Id;
            }
            else
            {
                subTask.CreatedAt = DateTime.Now;
                await _database!.InsertAsync(subTask);
                return subTask.Id;
            }
        }

        /// <summary>
        /// Delete a subtask
        /// </summary>
        public async Task<int> DeleteSubTaskAsync(SubTaskItem subTask)
        {
            await InitAsync();
            return await _database!.DeleteAsync(subTask);
        }

        /// <summary>
        /// Toggle subtask completion status
        /// </summary>
        public async Task ToggleSubTaskAsync(SubTaskItem subTask)
        {
            await InitAsync();
            subTask.IsCompleted = !subTask.IsCompleted;
            await _database!.UpdateAsync(subTask);
        }

        #endregion
    }
}
