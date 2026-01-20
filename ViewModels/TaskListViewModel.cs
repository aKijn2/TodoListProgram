using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Todo_asa.Models;
using Todo_asa.Services;

namespace Todo_asa.ViewModels
{
    /// <summary>
    /// ViewModel for the main task list page
    /// </summary>
    public partial class TaskListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

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
        /// Load tasks from database, optionally with a filter index from tab tap
        /// </summary>
        [RelayCommand]
        private async Task LoadTasksAsync(object? filterParam = null)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                
                // Handle filter parameter from tab taps
                if (filterParam != null)
                {
                    if (int.TryParse(filterParam.ToString(), out int filterIndex))
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
                }
                
                List<TaskItem> tasks;
                
                if (SelectedFilter.HasValue)
                {
                    tasks = await _databaseService.GetTasksByStatusAsync(SelectedFilter.Value);
                }
                else
                {
                    tasks = await _databaseService.GetTasksAsync();
                }

                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load tasks: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
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
        /// Delete a task
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
                await _databaseService.DeleteTaskAsync(task);
                Tasks.Remove(task);
            }
        }

        /// <summary>
        /// Quick toggle task status
        /// </summary>
        [RelayCommand]
        private async Task ToggleTaskStatusAsync(TaskItem task)
        {
            if (task == null) return;

            // Cycle through statuses: ToDo -> InProgress -> Completed -> ToDo
            task.Status = task.Status switch
            {
                Models.TaskStatus.ToDo => Models.TaskStatus.InProgress,
                Models.TaskStatus.InProgress => Models.TaskStatus.Completed,
                Models.TaskStatus.Completed => Models.TaskStatus.ToDo,
                _ => Models.TaskStatus.ToDo
            };

            await _databaseService.SaveTaskAsync(task);
            
            // Reload to refresh the list
            await LoadTasksAsync();
        }

        /// <summary>
        /// Filter tasks by status
        /// </summary>
        partial void OnSelectedFilterIndexChanged(int value)
        {
            SelectedFilter = value switch
            {
                1 => Models.TaskStatus.ToDo,
                2 => Models.TaskStatus.InProgress,
                3 => Models.TaskStatus.Completed,
                _ => null // 0 = All
            };
            
            _ = LoadTasksAsync();
        }
    }
}
