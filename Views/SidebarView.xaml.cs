using Microsoft.Maui.Controls;
using System;

namespace TaskFlow.Views
{
    public partial class SidebarView : ContentView
    {
        public SidebarView()
        {
            InitializeComponent();
        }

        private async void OnGameTapped(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new GamePage());
        }
    }
}
