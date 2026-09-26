using AshaNandanvan.Application.Orders;

namespace AshaNandanvan.Application.Payments;

public static class PaymentFlow
{
    public static PaymentRequest ForOrder(OrderSummary order, string currency) =>
        new(
            order.OrderNumber,
            order.CustomerEmail,
            $"Asha Nandanvan {order.OrderNumber}",
            order.Total,
            currency,
            order.Items.Select(i => new PaymentLine($"{i.ProductName} ({i.Unit})", i.Quantity, i.UnitPrice)).ToList());
}
