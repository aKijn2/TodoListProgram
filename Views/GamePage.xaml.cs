using Microsoft.Maui.Controls;
using System;
using System.Linq;

namespace TaskFlow.Views
{
    public partial class GamePage : ContentPage
    {
        private bool isPlayerXTurn = true;
        private int movesCount = 0;
        private bool isGameActive = true;
        private Button[] buttons;

        public GamePage()
        {
            InitializeComponent();
            InitializeGameButtons();
        }

        private void InitializeGameButtons()
        {
            buttons = GameGrid.Children.OfType<Button>().ToArray();
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        private void OnGameButtonClicked(object sender, EventArgs e)
        {
            if (!isGameActive) return;

            var button = (Button)sender;

            if (!string.IsNullOrEmpty(button.Text)) return;

            // Determine color based on player and theme
            Color textColor;
            if (isPlayerXTurn)
            {
                // X should be Black in Light Mode, White in Dark Mode
                textColor = Application.Current.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black;
            }
            else
            {
                // O is Gray
                textColor = Colors.Gray;
            }

            button.Text = isPlayerXTurn ? "X" : "O";
            button.TextColor = textColor;
            
            movesCount++;

            if (CheckForWinner())
            {
                StatusLabel.Text = $"Player {(isPlayerXTurn ? "X" : "O")} Wins!";
                isGameActive = false;
            }
            else if (movesCount == 9)
            {
                StatusLabel.Text = "It's a Draw!";
                isGameActive = false;
            }
            else
            {
                isPlayerXTurn = !isPlayerXTurn;
                StatusLabel.Text = $"Player {(isPlayerXTurn ? "X" : "O")}'s Turn";
            }
        }

        private bool CheckForWinner()
        {
            // Winning combinations indices
            int[][] winningCombinations = new int[][]
            {
                new int[] { 0, 1, 2 }, // Row 1
                new int[] { 3, 4, 5 }, // Row 2
                new int[] { 6, 7, 8 }, // Row 3
                new int[] { 0, 3, 6 }, // Col 1
                new int[] { 1, 4, 7 }, // Col 2
                new int[] { 2, 5, 8 }, // Col 3
                new int[] { 0, 4, 8 }, // Diagonal 1
                new int[] { 2, 4, 6 }  // Diagonal 2
            };

            foreach (var combo in winningCombinations)
            {
                var b1 = buttons[combo[0]];
                var b2 = buttons[combo[1]];
                var b3 = buttons[combo[2]];

                if (!string.IsNullOrEmpty(b1.Text) &&
                    b1.Text == b2.Text &&
                    b2.Text == b3.Text)
                {
                    HighlightWinningButtons(b1, b2, b3);
                    return true;
                }
            }

            return false;
        }

        private void HighlightWinningButtons(Button b1, Button b2, Button b3)
        {
            var highlightColor = Colors.LightGreen; // Or a theme color
            var textColor = Colors.Black; // Ensure high contrast

            b1.BackgroundColor = highlightColor;
            b1.TextColor = textColor;

            b2.BackgroundColor = highlightColor;
            b2.TextColor = textColor;

            b3.BackgroundColor = highlightColor;
            b3.TextColor = textColor;
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            foreach (var btn in buttons)
            {
                btn.Text = "";
                // Reset styling - relying on AppThemeBinding might be tricky with manual C# overrides, 
                // so we might need to be careful or just reset to default transparent/white
                btn.BackgroundColor = (Color)App.Current.Resources.MergedDictionaries.First()["White"]; 
                // A simpler reset is to just clear local values if possible, 
                // or re-apply the style, but let's just clear specific properties for now
                btn.ClearValue(Button.BackgroundColorProperty);
            }

            isPlayerXTurn = true;
            movesCount = 0;
            isGameActive = true;
            StatusLabel.Text = "Player X's Turn";
        }
    }
}
