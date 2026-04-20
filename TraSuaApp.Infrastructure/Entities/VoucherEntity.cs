namespace TraSuaApp.Infrastructure.Entities;

public partial class Voucher
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public decimal? DieuKienToiThieu { get; set; }

    

    public string KieuGiam { get; set; } = null!;

    public DateTime? LastModified { get; set; }

}
