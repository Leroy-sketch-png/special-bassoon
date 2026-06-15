namespace MoePortal.Core.Domain.Enums;

public enum InvoiceStatus
{
    Pending = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,
    Refunded = 5
}
