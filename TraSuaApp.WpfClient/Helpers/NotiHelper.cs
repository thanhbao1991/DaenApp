using System.Windows.Forms;
using TraSuaApp.WpfClient.Helpers;

public static class NotiHelper
{
    public static void Show(string msg)
    {
        //NotifyIcon notify = new NotifyIcon
        //{
        //    Visible = true,
        //    Icon = SystemIcons.Information,
        //    BalloonTipTitle = "Thông báo",
        //    BalloonTipText = msg
        //};

        //notify.ShowBalloonTip(3000);
        //Task.Delay(4000).ContinueWith(_ => notify.Dispose());
        MessageBox.Show(msg);
    }
    public static void ShowError(string msg)
    {
        MessageBox.Show(msg);
        //NotifyIcon notify = new NotifyIcon
        //{
        //    Visible = true,
        //    Icon = SystemIcons.Error,
        //    BalloonTipTitle = "Lỗi: ",
        //    BalloonTipText = msg
        //};

        //notify.ShowBalloonTip(4000);
        //Task.Delay(5000).ContinueWith(_ => notify.Dispose());
    }
    // public static TextBlock? TargetTextBlock { get; set; }




    public static void ShowSilent(string message) => NotiCenter.Add(message, "info");
    public static void ShowSuccess(string message) => NotiCenter.Add(message, "success");
    public static void ShowWarn(string message) => NotiCenter.Add(message, "warn");
    public static void ShowError2(string message) => NotiCenter.Add(message, "error");


}
