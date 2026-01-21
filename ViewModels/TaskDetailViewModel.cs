using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Todo_asa.Models;
using Todo_asa.Services;

namespace Todo_asa.ViewModels
{
    /// <summary>
    /// ViewModel for task detail/edit page
    /// </summary>
    [QueryProperty(nameof(TaskId), "id")]
    public partial class TaskDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private int _taskId;

        [ObservableProperty]
        private string _taskTitle = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private Models.TaskStatus _status = Models.TaskStatus.ToDo;

        [ObservableProperty]
        private DateTime? _dueDate;

        [ObservableProperty]
        private DateTime _selectedDueDate = DateTime.Today;

        [ObservableProperty]
        private bool _hasDueDate = false;

        [ObservableProperty]
        private DateTime _minimumDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<SubTaskItem> _subTasks = new();

        [ObservableProperty]
        private string _newSubTaskTitle = string.Empty;

        [ObservableProperty]
        private bool _isNewTask = true;

        [ObservableProperty]
        private int _selectedStatusIndex = 0;

        public TaskDetailViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "New Task";
        }

        /// <summary>
        /// Load task when ID changes
        /// </summary>
        partial void OnTaskIdChanged(int value)
        {
            if (value > 0)
            {
                _ = LoadTaskAsync(value);
            }
        }

        /// <summary>
        /// Load task from database
        /// </summary>
        private async Task LoadTaskAsync(int id)
        {
            try
            {
                IsBusy = true;
                var task = await _databaseService.GetTaskAsync(id);
                
                if (task != null)
                {
                    IsNewTask = false;
                    Title = "Edit Task";
                    TaskTitle = task.Title;
                    Description = task.Description;
                    Status = task.Status;
                    SelectedStatusIndex = (int)task.Status;
                    DueDate = task.DueDate;
                    if (task.DueDate.HasValue)
                    {
                        SelectedDueDate = task.DueDate.Value;
                        HasDueDate = true;
                    }
                    
                    SubTasks.Clear();
                    foreach (var subTask in task.SubTasks)
                    {
                        SubTasks.Add(subTask);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load task: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Save the task
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(TaskTitle))
            {
                await Shell.Current.DisplayAlert("Validation", "Please enter a task title", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var task = new TaskItem
                {
                    Id = TaskId,
                    Title = TaskTitle.Trim(),
                    Description = Description?.Trim() ?? string.Empty,
                    Status = (Models.TaskStatus)SelectedStatusIndex,
                    DueDate = DueDate
                };

                await _databaseService.SaveTaskAsync(task);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to save task: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Delete the task
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (TaskId == 0) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Task",
                "Are you sure you want to delete this task?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                try
                {
                    var task = await _databaseService.GetTaskAsync(TaskId);
                    if (task != null)
                    {
                        await _databaseService.DeleteTaskAsync(task);
                    }
                    await Shell.Current.GoToAsync("..");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to delete task: {ex.Message}", "OK");
                }
            }
        }

        /// <summary>
        /// Add a new subtask
        /// </summary>
        [RelayCommand]
        private async Task AddSubTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSubTaskTitle)) return;

            // For new tasks, we need to save the task first
            if (TaskId == 0)
            {
                if (string.IsNullOrWhiteSpace(TaskTitle))
                {
                    await Shell.Current.DisplayAlert("Validation", "Please enter a task title first", "OK");
                    return;
                }

                var task = new TaskItem
                {
                    Title = TaskTitle.Trim(),
                    Description = Description?.Trim() ?? string.Empty,
                    Status = (Models.TaskStatus)SelectedStatusIndex,
                    DueDate = DueDate
                };

                TaskId = await _databaseService.SaveTaskAsync(task);
                IsNewTask = false;
                Title = "Edit Task";
            }

            var subTask = new SubTaskItem
            {
                ParentTaskId = TaskId,
                Title = NewSubTaskTitle.Trim(),
                IsCompleted = false
            };

            await _databaseService.SaveSubTaskAsync(subTask);
            SubTasks.Add(subTask);
            NewSubTaskTitle = string.Empty;
        }

        /// <summary>
        /// Toggle subtask completion
        /// </summary>
        [RelayCommand]
        private async Task ToggleSubTaskAsync(SubTaskItem subTask)
        {
            if (subTask == null) return;

            // Find index first
            var index = SubTasks.IndexOf(subTask);
            if (index < 0) return;

            // Toggle state
            subTask.IsCompleted = !subTask.IsCompleted;
            
            // Save to database
            await _databaseService.SaveSubTaskAsync(subTask);
            
            // Create a new instance to force UI update (since model doesn't implement INotifyPropertyChanged)
            var updatedSubTask = new SubTaskItem
            {
                Id = subTask.Id,
                ParentTaskId = subTask.ParentTaskId,
                Title = subTask.Title,
                IsCompleted = subTask.IsCompleted,
                CreatedAt = subTask.CreatedAt
            };

            // Replace the item in the collection
            SubTasks[index] = updatedSubTask;
        }

        /// <summary>
        /// Delete a subtask
        /// </summary>
        [RelayCommand]
        private async Task DeleteSubTaskAsync(SubTaskItem subTask)
        {
            if (subTask == null) return;

            await _databaseService.DeleteSubTaskAsync(subTask);
            SubTasks.Remove(subTask);
        }

        /// <summary>
        /// Called when HasDueDate changes
        /// </summary>
        partial void OnHasDueDateChanged(bool value)
        {
            if (value)
            {
                DueDate = SelectedDueDate;
            }
            else
            {
                DueDate = null;
            }
        }

        /// <summary>
        /// Called when SelectedDueDate changes
        /// </summary>
        partial void OnSelectedDueDateChanged(DateTime value)
        {
            if (HasDueDate)
            {
                DueDate = value;
            }
        }

        /// <summary>
        /// Toggle due date on/off
        /// </summary>
        [RelayCommand]
        private void ToggleDueDate()
        {
            if (HasDueDate)
            {
                HasDueDate = false;
                DueDate = null;
            }
            else
            {
                HasDueDate = true;
                SelectedDueDate = DateTime.Today;
                DueDate = SelectedDueDate;
            }
        }

        /// <summary>
        /// Go back without saving
        /// </summary>
        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
