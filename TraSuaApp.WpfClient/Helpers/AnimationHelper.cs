using System.Windows;
using System.Windows.Media.Animation;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class AnimationHelper
    {
        public static async Task FadeSwitchAsync(
            FrameworkElement? oldContent,
            FrameworkElement? newContent,
            int fadeOutMs = 180,
            int fadeInMs = 240,
            int delayMs = 50)
        {
            // Fade-out
            if (oldContent != null && oldContent.IsVisible && oldContent.Opacity > 0.9)
            {
                var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(fadeOutMs))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource();
                fadeOut.Completed += (s, e) => tcs.SetResult();
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                await tcs.Task;

                oldContent.Visibility = Visibility.Collapsed;
            }

            // Fade-in
            if (newContent != null)
            {
                newContent.BeginAnimation(UIElement.OpacityProperty, null);
                newContent.Visibility = Visibility.Visible;
                newContent.Opacity = 0;

                if (delayMs > 0) await Task.Delay(delayMs);

                var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(fadeInMs))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                newContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }
        public static void FadeInWindow(Window window, int durationMs = 300)
        {
            window.Opacity = 0;
            var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            window.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        public static async Task FadeOutWindowAsync(Window window, int durationMs = 250)
        {
            var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(durationMs))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var tcs = new TaskCompletionSource();
            fadeOut.Completed += (s, _) => tcs.SetResult();

            window.BeginAnimation(Window.OpacityProperty, fadeOut);
            await tcs.Task;
        }

    }
}