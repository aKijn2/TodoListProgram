using SQLite;
using TaskFlow.Models;

namespace TaskFlow.Services
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
            try 
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folderPath = Path.Combine(documentsPath, "Todo_asa");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string newDbPath = Path.Combine(folderPath, "Todo_asa.db3");
                
                // Migrate existing data if it exists in the old location
                string oldDbPath = Path.Combine(FileSystem.AppDataDirectory, "Todo_asa.db3");
                if (!File.Exists(newDbPath) && File.Exists(oldDbPath))
                {
                    File.Copy(oldDbPath, newDbPath);
                }

                _dbPath = newDbPath;
            }
            catch
            {
                // Fallback to AppData if Documents is not accessible (e.g. mobile permissions issues)
                _dbPath = Path.Combine(FileSystem.AppDataDirectory, "Todo_asa.db3");
            }
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

            // Schema migration: add ParentSubTaskId column if this is an existing DB
            try
            {
                await _database.ExecuteAsync(
                    "ALTER TABLE SubTasks ADD COLUMN ParentSubTaskId INTEGER NOT NULL DEFAULT 0");
            }
            catch
            {
                // Column already exists — safe to ignore
            }
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
                task.SubTasks = await GetAllSubTasksForTaskAsync(task.Id);
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
                task.SubTasks = await GetAllSubTasksForTaskAsync(task.Id);
            }
            
            return task;
        }

        /// <summary>
        /// Get ALL subtasks for a task (top-level only; children loaded separately)
        /// </summary>
        private async Task<List<SubTaskItem>> GetAllSubTasksForTaskAsync(int taskId)
        {
            return await GetSubTasksAsync(taskId);
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
                task.SubTasks = await GetAllSubTasksForTaskAsync(task.Id);
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
        /// Delete a task and all its subtasks (including child sub-subtasks)
        /// </summary>
        public async Task<int> DeleteTaskAsync(TaskItem task)
        {
            await InitAsync();
            // Delete all subtasks and sub-subtasks for this task
            await _database!.ExecuteAsync("DELETE FROM SubTasks WHERE ParentTaskId = ?", task.Id);
            return await _database.DeleteAsync(task);
        }

        #endregion

        #region SubTask Operations

        /// <summary>
        /// Get top-level subtasks for a task (direct children only)
        /// </summary>
        public async Task<List<SubTaskItem>> GetSubTasksAsync(int taskId)
        {
            await InitAsync();
            return await _database!.Table<SubTaskItem>()
                .Where(s => s.ParentTaskId == taskId && s.ParentSubTaskId == 0)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get child sub-subtasks for a given subtask
        /// </summary>
        public async Task<List<SubTaskItem>> GetChildSubTasksAsync(int subTaskId)
        {
            await InitAsync();
            return await _database!.Table<SubTaskItem>()
                .Where(s => s.ParentSubTaskId == subTaskId)
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
        /// Delete a subtask and all its children
        /// </summary>
        public async Task<int> DeleteSubTaskAsync(SubTaskItem subTask)
        {
            await InitAsync();
            // Cascade-delete any children of this subtask first
            await _database!.ExecuteAsync("DELETE FROM SubTasks WHERE ParentSubTaskId = ?", subTask.Id);
            return await _database.DeleteAsync(subTask);
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
