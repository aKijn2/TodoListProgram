using CommunityToolkit.Mvvm.ComponentModel;

namespace Todo_asa.ViewModels
{
    /// <summary>
    /// Base ViewModel with common functionality
    /// </summary>
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        public bool IsNotBusy => !IsBusy;
    }
}
