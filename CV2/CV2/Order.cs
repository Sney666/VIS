using System.Collections.Generic;
using System.Linq;

public class Order
{
    public int Id { get; set; }
    public string? Date { get; set; }
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    public double CalculateTotal()
    {
        return Items.Sum(item => item.Subtotal);
    }
    public void ProcessPayment(Payment payment)
    {
        Console.WriteLine($"Zpracována platba ve výši {payment.Amount} metodou: {payment.Method}");
    }
}