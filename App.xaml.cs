namespace TaskFlow
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Load saved theme
            string savedTheme = Preferences.Get("AppTheme", "Unspecified");
            if (Enum.TryParse(savedTheme, out AppTheme theme) && theme != AppTheme.Unspecified)
            {
                UserAppTheme = theme;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS
            // Set desktop window size
            window.Width = 1200;
            window.Height = 800;
            window.MinimumWidth = 800;
            window.MinimumHeight = 600;
            
            // Allow resizing (remove max constraints)
            // window.MaximumWidth = double.PositiveInfinity;
            // window.MaximumHeight = double.PositiveInfinity;

            // Center window
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            if (displayInfo.Width > 0 && displayInfo.Height > 0)
            {
                var density = displayInfo.Density;
                var screenWidth = displayInfo.Width / density;
                var screenHeight = displayInfo.Height / density;

                window.X = (screenWidth - window.Width) / 2;
                window.Y = (screenHeight - window.Height) / 2;
            }
#endif

            return window;
        }
    }
}