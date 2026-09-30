using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public class Customer {
    public int CustomerId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; } 
}
public class OrderSpecification {
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public string Material { get; set; }
    public string DesignNotes { get; set; }
}
public class Order {
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; }
    public OrderSpecification Specification { get; set; }
}

public class OrderService
{
    private List<Customer> customers = new List<Customer>();
    private List<Order> orders = new List<Order>();
    private int nextOrderId = 1;
    private int nextCustomerId = 1;
    public Customer CreateCustomer(string name, string email)
    {
        var c = new Customer { CustomerId = nextCustomerId++, Email = email, Name = name };
        customers.Add(c);
        return c;
    }
    public Order CreateOrder(int customerId, double width, double height, double depth, string material, string notes)
    {
        var spec = new OrderSpecification { Width = width, Height = height, Depth = depth, Material = material, DesignNotes = notes };
        var order = new Order { OrderId = nextOrderId++, CustomerId = customerId, Status = "Čeká na potvrzení", Specification = spec };
        orders.Add(order);
        return order;
    }
    public void ConfirmOrder(int orderId)
    {
        var order = orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null) order.Status = "Potvrzeno";
    }

    public void PrintOrder(int orderId)
    {
        var order = orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null) Console.WriteLine($"Objednávka {order.OrderId} | Stav: {order.Status} | Materiál: {order.Specification.Material}");
    }
}

class Program
{
    static void Main()
    {
        var service = new OrderService();
        var customer = service.CreateCustomer("Jan Novák", "jan@novak.cz");
        var order = service.CreateOrder(customer.CustomerId, 100, 200, 50, "Dub", "Bez úchytek");

        service.PrintOrder(order.OrderId);
        Console.WriteLine("Potvrzujete objednávku? (A/N)");
        if (Console.ReadLine().ToUpper() == "A") service.ConfirmOrder(order.OrderId);
        service.PrintOrder(order.OrderId);
    }
}