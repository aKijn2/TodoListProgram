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

        private async void OnThemeToggleTapped(object sender, EventArgs e)
        {
            // 360-degree rotation animation for "premium" feel
            await ThemeIconLabel.RotateTo(360, 500, Easing.CubicOut);
            ThemeIconLabel.Rotation = 0;
        }

        private async void OnTaskItemLoaded(object sender, EventArgs e)
        {
            if (sender is View view)
            {
                // Fade in animation
                await view.FadeTo(1, 400, Easing.CubicOut);
            }
        }
    }
}
