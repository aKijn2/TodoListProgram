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
            // Set fixed window size like a small mobile phone (360x640 is common)
            window.Width = 400;
            window.Height = 700;
            window.MinimumWidth = 400;
            window.MinimumHeight = 700;
            window.MaximumWidth = 400;
            window.MaximumHeight = 700;

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