using System;
using System.Collections.Generic;
using System.Linq;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
public class OrderSpecification
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public string Material { get; set; }
    public string DesignNotes { get; set; }
}
public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; }
    public OrderSpecification Specification { get; set; }
}

public class CustomerTableModule
{
    private List<Customer> customers = new List<Customer>();
    private int nextId = 1;

    public int AddCustomer(string name, string email)
    {
        customers.Add(new Customer { CustomerId = nextId, Name = name, Email = email });
        return nextId++;
    }
    public Customer GetCustomerById(int id) => customers.FirstOrDefault(c => c.CustomerId == id);
}

public class OrderTableModule
{
    private List<Order> orders = new List<Order>();
    private int nextId = 1;

    public int AddOrder(int customerId, OrderSpecification spec)
    {
        orders.Add(new Order { OrderId = nextId, CustomerId = customerId, Status = "Čeká na potvrzení", Specification = spec });
        return nextId++;
    }

    public void ConfirmOrder(int orderId)
    {
        var order = orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null) order.Status = "Potvrzeno";
    }

    public void PrintAllOrders()
    {
        foreach (var o in orders) Console.WriteLine($"Objednávka {o.OrderId} | Zákazník ID: {o.CustomerId} | Stav: {o.Status}");
    }
}

class Program
{
    static void Main()
    {
        var customerModule = new CustomerTableModule();
        var orderModule = new OrderTableModule();

        int customerId = customerModule.AddCustomer("Jan Novák", "jan@novak.cz");
        var spec = new OrderSpecification { Width = 100, Height = 200, Depth = 50, Material = "Dub", DesignNotes = "Bez úchytek" };

        int orderId = orderModule.AddOrder(customerId, spec);
        orderModule.PrintAllOrders();

        Console.WriteLine("Potvrzujete objednávku? (A/N)");
        if (Console.ReadLine().ToUpper() == "A") orderModule.ConfirmOrder(orderId);
        orderModule.PrintAllOrders();
    }
}