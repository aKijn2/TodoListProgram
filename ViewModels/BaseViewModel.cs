using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Base ViewModel with common functionality
    /// </summary>
    public partial class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                }
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsNotBusy => !IsBusy;
    }
}
