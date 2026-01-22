using TaskFlow.Pages;

namespace TaskFlow
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute("TaskDetailPage", typeof(TaskDetailPage));
        }
    }
}
