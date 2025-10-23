namespace TraSuaApp.Shared.Dtos.Requests
{
    public class PayDebtRequest
    {
        public string Type { get; set; } = "";
        public decimal? Amount { get; set; }   // null => thu đủ
        public string? Note { get; set; }
    }
}