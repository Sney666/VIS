using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ArPattern
{
    public class Material
    {
        public int Id { get; set; }
        public string Nazev { get; set; }

        private static readonly string _filePath = "materialy.json";

        public static bool Exists(int id)
        {
            if (!File.Exists(_filePath)) return false;
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<Material>>(json) ?? new List<Material>();
            return list.Any(m => m.Id == id);
        }
    }

    public class Zakaznik
    {
        public int Id { get; set; }
        public string Jmeno { get; set; }

        private static readonly string _filePath = "zakaznici.json";

        public void Save()
        {
            var list = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<List<Zakaznik>>(File.ReadAllText(_filePath)) ?? new List<Zakaznik>()
                : new List<Zakaznik>();

            if (this.Id == 0)
            {
                this.Id = list.Any() ? list.Max(z => z.Id) + 1 : 1;
                list.Add(this);
            }
            else
            {
                var index = list.FindIndex(z => z.Id == this.Id);
                if (index != -1)
                {
                    list[index] = this;
                }
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public class Objednavka
    {
        public int Id { get; set; }
        public int ZakaznikId { get; set; }
        public int MaterialId { get; set; }
        public int Sirka { get; set; }
        public int Vyska { get; set; }
        public int Hloubka { get; set; }
        public string Stav { get; set; }

        private static readonly string _filePath = "objednavky.json";

        private void Validate()
        {
            if (Sirka <= 0 || Vyska <= 0 || Hloubka <= 0)
            {
                throw new ArgumentException("Rozměry musí být větší než 0.");
            }

            if (!Material.Exists(MaterialId))
            {
                throw new ArgumentException($"Materiál s ID '{MaterialId}' neexistuje v databázi.");
            }
        }

        public void Save()
        {
            Validate();

            var list = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<List<Objednavka>>(File.ReadAllText(_filePath)) ?? new List<Objednavka>()
                : new List<Objednavka>();

            if (this.Id == 0)
            {
                this.Id = list.Any() ? list.Max(o => o.Id) + 1 : 1;
                list.Add(this);
            }
            else
            {
                var index = list.FindIndex(o => o.Id == this.Id);
                if (index != -1)
                {
                    list[index] = this;
                }
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Delete()
        {
            if (!File.Exists(_filePath)) return;

            var list = JsonSerializer.Deserialize<List<Objednavka>>(File.ReadAllText(_filePath)) ?? new List<Objednavka>();
            list.RemoveAll(o => o.Id == this.Id);

            File.WriteAllText(_filePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static Objednavka Find(int id)
        {
            if (!File.Exists(_filePath)) return null;

            var list = JsonSerializer.Deserialize<List<Objednavka>>(File.ReadAllText(_filePath)) ?? new List<Objednavka>();
            return list.FirstOrDefault(o => o.Id == id);
        }
    }

    public class Program
    {
        public static void Main()
        {
            File.WriteAllText("materialy.json", "[{\"Id\":1,\"Nazev\":\"Dubové dřevo\"}]");

            try
            {
                var zakaznik = new Zakaznik { Jmeno = "Jan Novák" };
                zakaznik.Save();

                var objednavka = new Objednavka
                {
                    ZakaznikId = zakaznik.Id,
                    MaterialId = 1,
                    Sirka = 1200,
                    Vyska = 750,
                    Hloubka = 600,
                    Stav = "CEKA_NA_POTVRZENI"
                };

                objednavka.Save();

                var ulozenaObjednavka = Objednavka.Find(objednavka.Id);

                if (ulozenaObjednavka != null)
                {
                    Console.WriteLine($"[ÚSPĚCH] Objednávka potvrzena!");
                    Console.WriteLine($"- ID objednávky: {ulozenaObjednavka.Id}");
                    Console.WriteLine($"- ID zákazníka: {ulozenaObjednavka.ZakaznikId}");
                    Console.WriteLine($"- Stav: {ulozenaObjednavka.Stav}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHYBA] {ex.Message}");
            }
        }
    }
}