namespace Todo_asa
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            // Set fixed window size like a small mobile phone (360x640 is common)
            window.Width = 400;
            window.Height = 700;
            window.MinimumWidth = 400;
            window.MinimumHeight = 700;
            window.MaximumWidth = 400;
            window.MaximumHeight = 700;
#endif

            return window;
        }
    }
}