using System.Windows;
using System.Windows.Media.Animation;

namespace TraSuaApp.WpfClient.Controls
{
    public class AnimatedWindow : Window
    {
        private bool _isClosing = false;

        public AnimatedWindow()
        {
            // Fade-in khi mở
            this.Loaded += (s, e) =>
            {
                this.Opacity = 0;
                var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            };
        }

        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosing)
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            _isClosing = true;

            await FadeOutAsync();

            base.OnClosing(e);
            this.Close();
        }

        private Task FadeOutAsync(int durationMs = 250)
        {
            var tcs = new TaskCompletionSource();

            var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (s, e) => tcs.SetResult();

            this.BeginAnimation(Window.OpacityProperty, fadeOut);

            return tcs.Task;
        }
    }
}