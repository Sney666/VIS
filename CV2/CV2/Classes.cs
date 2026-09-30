using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderingSystem
{
    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Address? Address { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
    }

    public class Category
    {
        public string? Name { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
    }

    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Price { get; set; }
        public Category? Category { get; set; }
    }

    public class Payment
    {
        public string? Method { get; set; }
        public double Amount { get; set; }
    }

    public class CreditCardPayment : Payment
    {
        public string? CardNumber { get; set; }
    }

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

    public class OrderItem
    {
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }

        public double Subtotal => Quantity * (Product?.Price ?? 0);
    }
}