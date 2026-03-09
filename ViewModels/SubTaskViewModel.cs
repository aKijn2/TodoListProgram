using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TaskFlow.Models;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Observable wrapper around SubTaskItem that supports nested children
    /// and in-line editing without needing to replace the whole object.
    /// </summary>
    public partial class SubTaskViewModel : ObservableObject
    {
        public int Id { get; set; }
        public int ParentTaskId { get; set; }
        public int ParentSubTaskId { get; set; }
        public DateTime CreatedAt { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TitleDecoration))]
        private string _title = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TitleDecoration))]
        [NotifyPropertyChangedFor(nameof(TitleColor))]
        private bool _isCompleted;

        /// <summary>
        /// Text typed into the "add sub-subtask" field for this particular subtask
        /// </summary>
        [ObservableProperty]
        private string _newChildTitle = string.Empty;

        /// <summary>
        /// True while the inline edit panel is open for this item
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotEditing))]
        private bool _isEditing = false;

        /// <summary>
        /// Draft title shown in the inline edit Entry
        /// </summary>
        [ObservableProperty]
        private string _editingTitle = string.Empty;

        public bool IsNotEditing => !IsEditing;

        public TextDecorations TitleDecoration =>
            IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None;

        // Unused at runtime (AppThemeBinding handles it in XAML) — kept for completeness
        public string TitleColor => IsCompleted ? "SecondaryTextColor" : "PrimaryTextColor";

        /// <summary>
        /// Direct children (one level deep) of this subtask
        /// </summary>
        public ObservableCollection<SubTaskViewModel> Children { get; set; } = new();

        // Factory helpers

        public static SubTaskViewModel FromModel(SubTaskItem model) => new SubTaskViewModel
        {
            Id = model.Id,
            ParentTaskId = model.ParentTaskId,
            ParentSubTaskId = model.ParentSubTaskId,
            Title = model.Title,
            IsCompleted = model.IsCompleted,
            CreatedAt = model.CreatedAt
        };

        public SubTaskItem ToModel() => new SubTaskItem
        {
            Id = Id,
            ParentTaskId = ParentTaskId,
            ParentSubTaskId = ParentSubTaskId,
            Title = Title,
            IsCompleted = IsCompleted,
            CreatedAt = CreatedAt
        };
    }
}
