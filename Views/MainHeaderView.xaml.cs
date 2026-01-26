using Microsoft.Maui.Controls;
using System;

namespace TaskFlow.Views
{
    public partial class MainHeaderView : ContentView
    {
        public MainHeaderView()
        {
            InitializeComponent();
        }

        private async void OnThemeToggleTapped(object sender, EventArgs e)
        {
            // 360-degree rotation animation for "premium" feel
            await ThemeIconPath.RotateTo(360, 500, Easing.CubicOut);
            ThemeIconPath.Rotation = 0;
        }

        private async void OnGameTapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushModalAsync(new Views.GamePage());
        }
    }
}
