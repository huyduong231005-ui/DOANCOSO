using t.Models.Entities.Common;

namespace t.Models.Entities;

public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}
