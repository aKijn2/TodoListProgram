using Microsoft.Maui.Controls;
using System;

namespace TaskFlow.Views
{
    public partial class TaskItemView : ContentView
    {
        public TaskItemView()
        {
            InitializeComponent();
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
