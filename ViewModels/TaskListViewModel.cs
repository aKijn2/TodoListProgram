using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Todo_asa.Models;
using Todo_asa.Services;

namespace Todo_asa.ViewModels
{
    /// <summary>
    /// ViewModel for the main task list page - optimized for fast tab switching
    /// </summary>
    public partial class TaskListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        
        // Cache all tasks in memory for instant filtering
        private List<TaskItem> _cachedTasks = new();
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<TaskItem> _tasks = new();

        [ObservableProperty]
        private Models.TaskStatus? _selectedFilter;

        [ObservableProperty]
        private int _selectedFilterIndex = 0;

        public TaskListViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "My Tasks";
        }

        /// <summary>
        /// Initial load from database - called once on startup
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            
            try
            {
                _cachedTasks = await _databaseService.GetTasksAsync();
                _isInitialized = true;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load tasks: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Refresh cache from database
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            
            try
            {
                IsBusy = true;
                _cachedTasks = await _databaseService.GetTasksAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to refresh tasks: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Apply filter instantly from cached data - no database hit
        /// </summary>
        private void ApplyFilter()
        {
            IEnumerable<TaskItem> filtered = SelectedFilter.HasValue
                ? _cachedTasks.Where(t => t.Status == SelectedFilter.Value)
                : _cachedTasks;

            Tasks.Clear();
            foreach (var task in filtered.OrderByDescending(t => t.CreatedAt))
            {
                Tasks.Add(task);
            }
        }

        /// <summary>
        /// Handle tab tap - instant filtering from cache
        /// </summary>
        [RelayCommand]
        private Task LoadTasksAsync(object? filterParam = null)
        {
            // Handle filter parameter from tab taps
            if (filterParam != null && int.TryParse(filterParam.ToString(), out int filterIndex))
            {
                SelectedFilterIndex = filterIndex;
                SelectedFilter = filterIndex switch
                {
                    1 => Models.TaskStatus.ToDo,
                    2 => Models.TaskStatus.InProgress,
                    3 => Models.TaskStatus.Completed,
                    _ => null // 0 = All
                };
            }
            
            // Instant filter from cache - no await needed
            ApplyFilter();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Navigate to add new task page
        /// </summary>
        [RelayCommand]
        private async Task AddTaskAsync()
        {
            await Shell.Current.GoToAsync("TaskDetailPage");
        }

        /// <summary>
        /// Navigate to task detail/edit page
        /// </summary>
        [RelayCommand]
        private async Task ViewTaskAsync(TaskItem task)
        {
            if (task == null) return;
            await Shell.Current.GoToAsync($"TaskDetailPage?id={task.Id}");
        }

        /// <summary>
        /// Delete a task - update cache immediately
        /// </summary>
        [RelayCommand]
        private async Task DeleteTaskAsync(TaskItem task)
        {
            if (task == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Task",
                $"Are you sure you want to delete '{task.Title}'?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                // Update cache immediately
                _cachedTasks.Remove(task);
                Tasks.Remove(task);
                
                // Delete from database in background
                await _databaseService.DeleteTaskAsync(task);
            }
        }

        /// <summary>
        /// Quick toggle task status - update cache immediately
        /// </summary>
        [RelayCommand]
        private async Task ToggleTaskStatusAsync(TaskItem task)
        {
            if (task == null) return;

            // Cycle through statuses
            task.Status = task.Status switch
            {
                Models.TaskStatus.ToDo => Models.TaskStatus.InProgress,
                Models.TaskStatus.InProgress => Models.TaskStatus.Completed,
                Models.TaskStatus.Completed => Models.TaskStatus.ToDo,
                _ => Models.TaskStatus.ToDo
            };

            // Re-apply filter immediately (task may move to different tab)
            ApplyFilter();
            
            // Save to database in background
            await _databaseService.SaveTaskAsync(task);
        }

        /// <summary>
        /// Filter change handler - instant from cache
        /// </summary>
        partial void OnSelectedFilterIndexChanged(int value)
        {
            SelectedFilter = value switch
            {
                1 => Models.TaskStatus.ToDo,
                2 => Models.TaskStatus.InProgress,
                3 => Models.TaskStatus.Completed,
                _ => null
            };
            
            ApplyFilter();
        }

        /// <summary>
        /// Invalidate cache - called when returning from detail page
        /// </summary>
        public async Task InvalidateCacheAsync()
        {
            _cachedTasks = await _databaseService.GetTasksAsync();
            ApplyFilter();
        }
    }
}
