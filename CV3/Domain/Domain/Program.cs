using System;
using System.Collections.Generic;

public class OrderSpecification {
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public string Material { get; set; }
    public string DesignNotes { get; set; }
}
public class Order
{
    public int OrderId { get; private set; }
    public OrderSpecification Specification { get; private set; }
    public string Status { get; private set; }

    public Order(int id, OrderSpecification spec)
    {
        OrderId = id;
        Specification = spec;
        Status = "Čeká na potvrzení";
    }

    public void Confirm()
    {
        Status = "Potvrzeno";
    }

    public void PrintSummary()
    {
        Console.WriteLine($"Objednávka {OrderId} | Stav: {Status} | Materiál: {Specification.Material}");
    }
}
public class Customer
{
    public int CustomerId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    private List<Order> orders = new List<Order>();
    private static int orderIdCounter = 1;

    public Customer(int id, string name, string email)
    {
        CustomerId = id;
        Name = name;
        Email = email;
    }

    public Order CreateOrder(OrderSpecification spec)
    {
        var order = new Order(orderIdCounter++, spec);
        orders.Add(order);
        return order;
    }

    public void ListOrders()
    {
        foreach (var order in orders) order.PrintSummary();
    }
}

class Program
{
    static void Main()
    {
        var customer = new Customer(1, "Jan Novák", "jan@novak.cz");
        var spec = new OrderSpecification { Width = 100, Height = 200, Depth = 50, Material = "Dub", DesignNotes = "Bez úchytek" };

        var order = customer.CreateOrder(spec);
        order.PrintSummary();

        Console.WriteLine("Potvrzujete objednávku? (A/N)");
        if (Console.ReadLine().ToUpper() == "A") order.Confirm();
        order.PrintSummary();
    }
}