using Todo_asa.ViewModels;

namespace Todo_asa
{
    public partial class MainPage : ContentPage
    {
        private readonly TaskListViewModel _viewModel;

        public MainPage(TaskListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTasksCommand.ExecuteAsync(null);
        }
    }
}
