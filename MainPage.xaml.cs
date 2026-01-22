using TaskFlow.ViewModels;

namespace TaskFlow
{
    public partial class MainPage : ContentPage
    {
        private readonly TaskListViewModel _viewModel;
        private bool _isFirstLoad = true;

        public MainPage(TaskListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (_isFirstLoad)
            {
                // First load - initialize from database
                await _viewModel.InitializeAsync();
                _isFirstLoad = false;
            }
            else
            {
                // Returning from detail page - refresh cache
                await _viewModel.InvalidateCacheAsync();
            }
        }
    }
}
