using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class PaymentMethod
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DangSuDung { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
