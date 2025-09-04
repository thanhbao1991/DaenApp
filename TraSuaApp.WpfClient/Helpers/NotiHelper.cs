using System.Windows.Forms;

public static class NotiHelper
{
    // 🟟 Hàm 1 tham số → Thông tin
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

    // 🟟 Hàm lỗi gọn (2 tham số)
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
}