namespace DBH.Payment.Service.Models.Enums;

public enum InvoiceStatus
{
    UNPAID,
    PAID,
    CANCELLED
}

public enum PaymentMethod
{
    PAYOS_VIETQR,
    CASH
}

public enum PaymentStatus
{
    PENDING,
    PAID,
    CANCELLED
}
