using Todo_asa.Pages;

namespace Todo_asa
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
