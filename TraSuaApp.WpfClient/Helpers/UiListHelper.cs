// Helpers/UiListHelper.cs
namespace TraSuaApp.WpfClient.Helpers
{
    public static class UiListHelper
    {
        /// <summary>
        /// Chụp snapshot nhanh trên UI thread và xử lý list nặng ở background
        /// </summary>
        public static async Task<List<T>> BuildListAsync<T>(
            IEnumerable<T> source,
            Func<IEnumerable<T>, IEnumerable<T>> pipeline)
        {
            // snapshot nhanh trên UI thread
            var snap = source.ToList();
            // chạy filter/sort ở background
            return await Task.Run(() => pipeline(snap).ToList());
        }
    }
}