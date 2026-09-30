public class OrderItem
{
    public Order Order { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public double Subtotal => Quantity * (Product?.Price ?? 0);
}