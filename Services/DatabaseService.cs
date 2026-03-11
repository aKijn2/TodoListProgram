using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.Models;

namespace TaskFlow.Services
{
    // ── Supabase response DTOs ────────────────────────────────────────────────
    internal sealed class TaskDto
    {
        public int       Id          { get; set; }
        public string    Title       { get; set; } = string.Empty;
        public string    Description { get; set; } = string.Empty;
        public int       Status      { get; set; }
        public DateTime? DueDate     { get; set; }
        public DateTime  CreatedAt   { get; set; }
        public DateTime  UpdatedAt   { get; set; }
    }

    internal sealed class SubTaskDto
    {
        public int      Id              { get; set; }
        public int      ParentTaskId    { get; set; }
        public int      ParentSubTaskId { get; set; }
        public string   Title           { get; set; } = string.Empty;
        public bool     IsCompleted     { get; set; }
        public DateTime CreatedAt       { get; set; }
    }

    public class DatabaseService
    {
        private readonly HttpClient _httpClient;

        // ── Supabase credentials ──────────────────────────────────────────────
        // 1. Sign up free at https://supabase.com and create a project
        // 2. In Project Settings → API, copy the Project URL and anon/public key
        private const string SupabaseUrl = "https://iejhblfmpesmzkmlfoyi.supabase.co";
        private const string AnonKey     = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImllamhibGZtcGVzbXprbWxmb3lpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzMyMTU2OTksImV4cCI6MjA4ODc5MTY5OX0.GfnrbOBaXbSKv42Qtxcm7NL918CwCxIkAXlmknzMDLw";
        // ─────────────────────────────────────────────────────────────────────

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DatabaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"{SupabaseUrl}/rest/v1/");
            _httpClient.DefaultRequestHeaders.Add("apikey", AnonKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AnonKey}");
        }

        // ── Task operations ───────────────────────────────────────────────────

        public async Task<List<TaskItem>> GetTasksAsync()
        {
            // 2 parallel requests instead of 1 + N + N*M sequential round-trips
            var tasksTask    = GetAsync<List<TaskDto>>("Tasks?select=*&order=CreatedAt.desc");
            var subTasksTask = GetAsync<List<SubTaskDto>>("SubTasks?select=*&order=CreatedAt.asc");
            await Task.WhenAll(tasksTask, subTasksTask);

            return BuildTaskGraph(tasksTask.Result ?? new(), subTasksTask.Result ?? new());
        }

        public async Task<TaskItem?> GetTaskAsync(int id)
        {
            var dtos = await GetAsync<List<TaskDto>>($"Tasks?select=*&Id=eq.{id}") ?? new();
            var dto  = dtos.FirstOrDefault();
            if (dto == null) return null;
            var task = MapTask(dto);
            task.SubTasks = await GetAllSubTasksAsync(task.Id);
            return task;
        }

        public async Task<List<TaskItem>> GetTasksByStatusAsync(Models.TaskStatus status)
        {
            // Still fetch all subtasks in one shot and filter tasks in memory
            var tasksTask    = GetAsync<List<TaskDto>>("Tasks?select=*&order=CreatedAt.desc");
            var subTasksTask = GetAsync<List<SubTaskDto>>("SubTasks?select=*&order=CreatedAt.asc");
            await Task.WhenAll(tasksTask, subTasksTask);

            var allTasks = BuildTaskGraph(tasksTask.Result ?? new(), subTasksTask.Result ?? new());
            return allTasks.Where(t => t.Status == status).ToList();
        }

        public async Task<int> SaveTaskAsync(TaskItem task)
        {
            task.UpdatedAt = DateTime.UtcNow;
            if (task.Id != 0)
            {
                await PatchAsync($"Tasks?Id=eq.{task.Id}", new
                {
                    task.Title, task.Description,
                    Status  = (int)task.Status,
                    task.DueDate, task.UpdatedAt
                });
                return task.Id;
            }
            else
            {
                task.CreatedAt = DateTime.UtcNow;
                var result = await PostAsync<List<TaskDto>>("Tasks", new
                {
                    task.Title, task.Description,
                    Status = (int)task.Status,
                    task.DueDate, task.CreatedAt, task.UpdatedAt
                });
                task.Id = result?.FirstOrDefault()?.Id ?? 0;
                return task.Id;
            }
        }

        public async Task<int> DeleteTaskAsync(TaskItem task)
        {
            // Cascade: delete subtasks first, then the task
            await _httpClient.DeleteAsync($"SubTasks?ParentTaskId=eq.{task.Id}");
            await _httpClient.DeleteAsync($"Tasks?Id=eq.{task.Id}");
            return 1;
        }

        // ── SubTask operations ────────────────────────────────────────────────

        public async Task<List<SubTaskItem>> GetSubTasksAsync(int taskId)
        {
            var dtos = await GetAsync<List<SubTaskDto>>(
                $"SubTasks?select=*&ParentTaskId=eq.{taskId}&ParentSubTaskId=eq.0&order=CreatedAt.asc") ?? new();
            return dtos.Select(MapSubTask).ToList();
        }

        public async Task<List<SubTaskItem>> GetChildSubTasksAsync(int subTaskId)
        {
            var dtos = await GetAsync<List<SubTaskDto>>(
                $"SubTasks?select=*&ParentSubTaskId=eq.{subTaskId}&order=CreatedAt.asc") ?? new();
            return dtos.Select(MapSubTask).ToList();
        }

        public async Task<int> SaveSubTaskAsync(SubTaskItem subTask)
        {
            if (subTask.Id != 0)
            {
                await PatchAsync($"SubTasks?Id=eq.{subTask.Id}", new
                {
                    subTask.Title, subTask.IsCompleted
                });
                return subTask.Id;
            }
            else
            {
                subTask.CreatedAt = DateTime.UtcNow;
                var result = await PostAsync<List<SubTaskDto>>("SubTasks", new
                {
                    subTask.ParentTaskId, subTask.ParentSubTaskId,
                    subTask.Title, subTask.IsCompleted, subTask.CreatedAt
                });
                subTask.Id = result?.FirstOrDefault()?.Id ?? 0;
                return subTask.Id;
            }
        }

        public async Task<int> DeleteSubTaskAsync(SubTaskItem subTask)
        {
            await _httpClient.DeleteAsync($"SubTasks?ParentSubTaskId=eq.{subTask.Id}");
            await _httpClient.DeleteAsync($"SubTasks?Id=eq.{subTask.Id}");
            return 1;
        }

        public async Task ToggleSubTaskAsync(SubTaskItem subTask)
        {
            subTask.IsCompleted = !subTask.IsCompleted;
            await PatchAsync($"SubTasks?Id=eq.{subTask.Id}", new { subTask.IsCompleted });
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<T?> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_json);
        }

        private async Task<T?> PostAsync<T>(string url, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body, options: _json)
            };
            req.Headers.Add("Prefer", "return=representation");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_json);
        }

        private async Task PatchAsync(string url, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(body, options: _json)
            };
            req.Headers.Add("Prefer", "return=minimal");
            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<SubTaskItem>> GetAllSubTasksAsync(int taskId)
        {
            var top = await GetSubTasksAsync(taskId);
            foreach (var s in top)
                s.Children = await GetChildSubTasksAsync(s.Id);
            return top;
        }

        // Builds the full task+subtask graph from two already-fetched lists — zero extra HTTP calls
        private static List<TaskItem> BuildTaskGraph(List<TaskDto> taskDtos, List<SubTaskDto> subDtos)
        {
            // Children (ParentSubTaskId != 0) indexed by their parent subtask id
            var childrenBySubTask = subDtos
                .Where(s => s.ParentSubTaskId != 0)
                .GroupBy(s => s.ParentSubTaskId)
                .ToDictionary(g => g.Key, g => g.Select(MapSubTask).ToList());

            // Top-level subtasks (ParentSubTaskId == 0) indexed by task id
            var subsByTask = subDtos
                .Where(s => s.ParentSubTaskId == 0)
                .GroupBy(s => s.ParentTaskId)
                .ToDictionary(g => g.Key, g => g.Select(s =>
                {
                    var st = MapSubTask(s);
                    st.Children = childrenBySubTask.GetValueOrDefault(st.Id) ?? new();
                    return st;
                }).ToList());

            return taskDtos.Select(d =>
            {
                var task = MapTask(d);
                task.SubTasks = subsByTask.GetValueOrDefault(task.Id) ?? new();
                return task;
            }).ToList();
        }

        // ── Model mappings ────────────────────────────────────────────────────

        private static TaskItem MapTask(TaskDto d) => new()
        {
            Id          = d.Id,
            Title       = d.Title,
            Description = d.Description,
            Status      = (Models.TaskStatus)d.Status,
            DueDate     = d.DueDate,
            CreatedAt   = d.CreatedAt,
            UpdatedAt   = d.UpdatedAt
        };

        private static SubTaskItem MapSubTask(SubTaskDto d) => new()
        {
            Id              = d.Id,
            ParentTaskId    = d.ParentTaskId,
            ParentSubTaskId = d.ParentSubTaskId,
            Title           = d.Title,
            IsCompleted     = d.IsCompleted,
            CreatedAt       = d.CreatedAt
        };
    }
}
