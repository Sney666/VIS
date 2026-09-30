using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderingSystem
{
    class Program
    {
        static List<Customer> customers = new List<Customer>();
        static List<Product> products = new List<Product>();
        static List<Category> categories = new List<Category>();

        static readonly string filePath = "data.json";

        static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true
        };

        static void Main(string[] args)
        {

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n--- Objednávkový Systém ---");
                Console.WriteLine("1. Add Customer");
                Console.WriteLine("2. Add Product");
                Console.WriteLine("3. Create Order");
                Console.WriteLine("4. View Orders");
                Console.WriteLine("5. Exit");
                Console.Write("Vyberte volbu: ");

                string choice = Console.ReadLine() ?? "";
                switch (choice)
                {
                    case "1": AddCustomer(); break;
                    case "2": AddProduct(); break;
                    case "3": CreateOrder(); break;
                    case "4": ViewOrders(); break;
                    case "5":
                        SaveData();
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Neplatná volba.");
                        break;
                }
            }
        }

        static void AddCustomer()
        {
            Customer customer = new Customer();
            customer.Address = new Address();

            Console.Write("ID zákazníka: ");
            if (int.TryParse(Console.ReadLine(), out int id)) customer.Id = id;

            Console.Write("Jméno zákazníka: ");
            customer.Name = Console.ReadLine() ?? "Neznámý";

            Console.Write("Ulice: ");
            customer.Address.Street = Console.ReadLine() ?? "";

            Console.Write("Město: ");
            customer.Address.City = Console.ReadLine() ?? "";

            customers.Add(customer);
            Console.WriteLine("Zákazník přidán.");
        }

        static void AddProduct()
        {
            Product product = new Product();

            Console.Write("ID produktu: ");
            if (int.TryParse(Console.ReadLine(), out int id)) product.Id = id;

            Console.Write("Název produktu: ");
            product.Name = Console.ReadLine() ?? "Neznámý produkt";

            Console.Write("Cena produktu: ");
            if (double.TryParse(Console.ReadLine(), out double price)) product.Price = price;

            Console.Write("Kategorie: ");
            string catName = Console.ReadLine() ?? "Obecná";

            Category? category = categories.FirstOrDefault(c => string.Equals(c.Name, catName, StringComparison.OrdinalIgnoreCase));
            if (category == null)
            {
                category = new Category { Name = catName };
                categories.Add(category);
            }

            product.Category = category;
            category.Products.Add(product);

            products.Add(product);
            Console.WriteLine("Produkt přidán.");
        }

        static void CreateOrder()
        {
            Console.Write("Zadejte ID zákazníka: ");
            if (!int.TryParse(Console.ReadLine(), out int customerId)) return;

            Customer? customer = customers.FirstOrDefault(c => c.Id == customerId);
            if (customer == null)
            {
                Console.WriteLine("Zákazník neexistuje.");
                return;
            }

            Order order = new Order
            {
                Id = new Random().Next(1000, 9999),
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                Customer = customer
            };

            while (true)
            {
                Console.Write("ID produktu (nebo 'done' pro ukončení): ");
                string input = Console.ReadLine() ?? "";
                if (input.ToLower() == "done") break;

                if (int.TryParse(input, out int productId))
                {
                    Product? product = products.FirstOrDefault(p => p.Id == productId);
                    if (product != null)
                    {
                        Console.Write("Množství: ");
                        if (int.TryParse(Console.ReadLine(), out int qty))
                        {
                            OrderItem item = new OrderItem
                            {
                                Order = order,
                                Product = product,
                                Quantity = qty
                            };
                            order.Items.Add(item);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Produkt nenalezen.");
                    }
                }
            }

            double total = order.CalculateTotal();
            Console.WriteLine($"K úhradě: {total}");
            Console.Write("Platební metoda (Card/Cash): ");
            string method = Console.ReadLine() ?? "Cash";

            Payment payment;
            if (method.ToLower() == "card")
            {
                Console.Write("Číslo karty: ");
                string cardNum = Console.ReadLine() ?? "0000";
                payment = new CreditCardPayment { Method = method, Amount = total, CardNumber = cardNum };
            }
            else
            {
                payment = new Payment { Method = method, Amount = total };
            }

            order.ProcessPayment(payment);

            customer.Orders.Add(order);
            Console.WriteLine("Objednávka úspěšně vytvořena.");
        }

        static void ViewOrders()
        {
            var allOrders = customers.SelectMany(c => c.Orders).ToList();
            foreach (var order in allOrders)
            {
                Console.WriteLine($"\nObjednávka: {order.Id} | Zákazník: {order.Customer?.Name} | Celkem: {order.CalculateTotal()}");
                foreach (var item in order.Items)
                {
                    Console.WriteLine($" - {item.Product?.Name} (Ks: {item.Quantity}) | Mezisoučet: {item.Subtotal}");
                }
            }
        }

        static void SaveData()
        {
            try
            {
                var dataStore = new { Customers = customers, Products = products, Categories = categories };
                string jsonString = JsonSerializer.Serialize(dataStore, jsonOptions);
                File.WriteAllText(filePath, jsonString);
                Console.WriteLine("Data uložena.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba uložení: {ex.Message}");
            }
        }

       
    }
}