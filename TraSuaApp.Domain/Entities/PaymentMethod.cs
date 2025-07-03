namespace TraSuaApp.Domain.Entities;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public bool DangSuDung { get; set; } = true;

    public ICollection<Payment> Payments { get; set; }
}