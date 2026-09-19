using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly PickupDate { get; set; }
    public string PickupWindow { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal Total { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
