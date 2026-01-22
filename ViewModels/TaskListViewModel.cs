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

        private ObservableCollection<TaskItem> _tasks = new();
        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        private ObservableCollection<TaskItem> _todayTasks = new();
        public ObservableCollection<TaskItem> TodayTasks
        {
            get => _todayTasks;
            set => SetProperty(ref _todayTasks, value);
        }

        private ObservableCollection<TaskItem> _upcomingTasks = new();
        public ObservableCollection<TaskItem> UpcomingTasks
        {
            get => _upcomingTasks;
            set => SetProperty(ref _upcomingTasks, value);
        }

        private bool _hasTasks;
        public bool HasTasks
        {
            get => _hasTasks;
            set => SetProperty(ref _hasTasks, value);
        }

        private Models.TaskStatus? _selectedFilter;
        public Models.TaskStatus? SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        private int _selectedFilterIndex = 0;
        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                if (SetProperty(ref _selectedFilterIndex, value))
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
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

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
            var filtered = _cachedTasks.AsEnumerable();

            // 1. Filter by Status (Tabs)
            if (SelectedFilter.HasValue)
            {
                filtered = filtered.Where(t => t.Status == SelectedFilter.Value);
            }

            // 2. Filter by Search Text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim().ToLowerInvariant();
                filtered = filtered.Where(t => 
                    (t.Title?.ToLowerInvariant().Contains(search) ?? false) || 
                    (t.Description?.ToLowerInvariant().Contains(search) ?? false));
            }

            // 3. Sort - Always newest first (default)
            // 3. Sort - Oldest due date first for optimal attention, then created date
            filtered = filtered.OrderByDescending(t => t.CreatedAt);

            var filteredList = filtered.ToList();
            var todayDate = DateTime.Now.Date;

            // Split into Today/Upcoming
            var todayItems = filteredList.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= todayDate)
                                         .OrderBy(t => t.Status == Models.TaskStatus.Completed) // Completed last
                                         .ThenBy(t => t.DueDate);
            
            var upcomingItems = filteredList.Where(t => !t.DueDate.HasValue || t.DueDate.Value.Date > todayDate)
                                            .OrderBy(t => t.DueDate.HasValue ? t.DueDate.Value : DateTime.MaxValue); // No date at bottom

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Tasks.Clear();
                TodayTasks.Clear();
                UpcomingTasks.Clear();

                foreach (var task in filteredList)
                {
                    Tasks.Add(task);
                }

                foreach(var t in todayItems) TodayTasks.Add(t);
                foreach(var t in upcomingItems) UpcomingTasks.Add(t);

                HasTasks = Tasks.Count > 0;
            });
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
                
                // Refresh all lists (Tasks, TodayTasks, UpcomingTasks)
                ApplyFilter();
                
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

        [RelayCommand]
        private void ToggleTheme()
        {
            if (Application.Current == null) return;

            AppTheme newTheme = Application.Current.UserAppTheme == AppTheme.Light
                ? AppTheme.Dark
                : AppTheme.Light;
            
            Application.Current.UserAppTheme = newTheme;
            Preferences.Set("AppTheme", newTheme.ToString());
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
