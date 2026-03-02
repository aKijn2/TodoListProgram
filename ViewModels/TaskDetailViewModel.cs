using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// ViewModel for task detail/edit page
    /// </summary>
    [QueryProperty(nameof(TaskId), "id")]
    public partial class TaskDetailViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        private int _taskId;
        public int TaskId
        {
            get => _taskId;
            set
            {
                if (SetProperty(ref _taskId, value))
                {
                     if (value > 0)
                     {
                         _ = LoadTaskAsync(value);
                     }
                }
            }
        }

        private string _taskTitle = string.Empty;
        public string TaskTitle
        {
            get => _taskTitle;
            set => SetProperty(ref _taskTitle, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private Models.TaskStatus _status = Models.TaskStatus.ToDo;
        public Models.TaskStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        private DateTime _selectedDueDate = DateTime.Today;
        public DateTime SelectedDueDate
        {
            get => _selectedDueDate;
            set
            {
                if (SetProperty(ref _selectedDueDate, value))
                {
                    if (HasDueDate)
                    {
                        DueDate = value;
                    }
                }
            }
        }

        private bool _hasDueDate = false;
        public bool HasDueDate
        {
            get => _hasDueDate;
            set
            {
                if (SetProperty(ref _hasDueDate, value))
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
            }
        }

        private DateTime _minimumDate = DateTime.Today;
        public DateTime MinimumDate
        {
            get => _minimumDate;
            set => SetProperty(ref _minimumDate, value);
        }

        private ObservableCollection<SubTaskViewModel> _subTasks = new();
        public ObservableCollection<SubTaskViewModel> SubTasks
        {
            get => _subTasks;
            set => SetProperty(ref _subTasks, value);
        }

        private string _newSubTaskTitle = string.Empty;
        public string NewSubTaskTitle
        {
            get => _newSubTaskTitle;
            set => SetProperty(ref _newSubTaskTitle, value);
        }

        private bool _isNewTask = true;
        public bool IsNewTask
        {
            get => _isNewTask;
            set => SetProperty(ref _isNewTask, value);
        }

        private int _selectedStatusIndex = 0;
        public int SelectedStatusIndex
        {
            get => _selectedStatusIndex;
            set => SetProperty(ref _selectedStatusIndex, value);
        }

        public TaskDetailViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "New Task";
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
                        var vm = SubTaskViewModel.FromModel(subTask);
                        // Load one level of children
                        var children = await _databaseService.GetChildSubTasksAsync(subTask.Id);
                        foreach (var child in children)
                            vm.Children.Add(SubTaskViewModel.FromModel(child));
                        SubTasks.Add(vm);
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
        /// Add a new top-level subtask
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

            var subTaskModel = new SubTaskItem
            {
                ParentTaskId = TaskId,
                ParentSubTaskId = 0,
                Title = NewSubTaskTitle.Trim(),
                IsCompleted = false
            };

            await _databaseService.SaveSubTaskAsync(subTaskModel);
            var vm = SubTaskViewModel.FromModel(subTaskModel);
            SubTasks.Add(vm);
            NewSubTaskTitle = string.Empty;
        }

        /// <summary>
        /// Add a child sub-subtask to an existing subtask
        /// </summary>
        [RelayCommand]
        private async Task AddChildSubTaskAsync(SubTaskViewModel parent)
        {
            if (parent == null || string.IsNullOrWhiteSpace(parent.NewChildTitle)) return;

            var childModel = new SubTaskItem
            {
                ParentTaskId = parent.ParentTaskId,
                ParentSubTaskId = parent.Id,
                Title = parent.NewChildTitle.Trim(),
                IsCompleted = false
            };

            await _databaseService.SaveSubTaskAsync(childModel);
            parent.Children.Add(SubTaskViewModel.FromModel(childModel));
            parent.NewChildTitle = string.Empty;
        }

        /// <summary>
        /// Toggle subtask or sub-subtask completion
        /// </summary>
        [RelayCommand]
        private async Task ToggleSubTaskAsync(SubTaskViewModel subTask)
        {
            if (subTask == null) return;
            subTask.IsCompleted = !subTask.IsCompleted;
            await _databaseService.SaveSubTaskAsync(subTask.ToModel());
        }

        /// <summary>
        /// Edit a subtask or sub-subtask title via a prompt
        /// </summary>
        [RelayCommand]
        private async Task EditSubTaskAsync(SubTaskViewModel subTask)
        {
            if (subTask == null) return;

            string? result = await Shell.Current.DisplayPromptAsync(
                "Edit Subtask",
                "Update the subtask title:",
                initialValue: subTask.Title,
                maxLength: 200,
                keyboard: Keyboard.Text);

            if (result == null || string.IsNullOrWhiteSpace(result)) return;

            subTask.Title = result.Trim();
            await _databaseService.SaveSubTaskAsync(subTask.ToModel());
        }

        /// <summary>
        /// Delete a top-level subtask (and its children) from the list
        /// </summary>
        [RelayCommand]
        private async Task DeleteSubTaskAsync(SubTaskViewModel subTask)
        {
            if (subTask == null) return;
            await _databaseService.DeleteSubTaskAsync(subTask.ToModel());
            SubTasks.Remove(subTask);
        }

        /// <summary>
        /// Delete a child sub-subtask from its parent's Children collection
        /// </summary>
        [RelayCommand]
        private async Task DeleteChildSubTaskAsync(SubTaskViewModel child)
        {
            if (child == null) return;

            // Find the parent that owns this child
            var parent = SubTasks.FirstOrDefault(s => s.Children.Contains(child));
            if (parent != null)
            {
                await _databaseService.DeleteSubTaskAsync(child.ToModel());
                parent.Children.Remove(child);
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
