namespace TraSuaApp.WpfClient.Helpers
{
    // Helper hiển thị toast thay cho MessageBox
    public static class NotiHelper
    {
        public static void Show(string msg)
        {
            System.Windows.Forms.NotifyIcon notify = new System.Windows.Forms.NotifyIcon();
            notify.Visible = true;
            notify.Icon = System.Drawing.SystemIcons.Information;
            notify.BalloonTipTitle = "Thông báo";
            notify.BalloonTipText = msg;
            notify.ShowBalloonTip(3000);
        }
    }

}
