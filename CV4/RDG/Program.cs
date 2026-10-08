using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RdgPattern
{
    public class MaterialRow
    {
        public int Id { get; set; }
        public string Nazev { get; set; }

        private static readonly string _filePath = "materialy.json";

        public static MaterialRow Find(int id)
        {
            return FindAll().FirstOrDefault(m => m.Id == id);
        }

        public static List<MaterialRow> FindAll()
        {
            if (!File.Exists(_filePath)) return new List<MaterialRow>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<MaterialRow>>(json) ?? new List<MaterialRow>();
        }
    }

    public class ZakaznikRow
    {
        public int Id { get; set; }
        public string Jmeno { get; set; }

        private static readonly string _filePath = "zakaznici.json";

        public void Insert()
        {
            var zakaznici = FindAll();
            this.Id = zakaznici.Any() ? zakaznici.Max(z => z.Id) + 1 : 1;
            zakaznici.Add(this);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(zakaznici, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Update()
        {
            var zakaznici = FindAll();
            var index = zakaznici.FindIndex(z => z.Id == this.Id);
            if (index != -1)
            {
                zakaznici[index] = this;
                File.WriteAllText(_filePath, JsonSerializer.Serialize(zakaznici, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public void Delete()
        {
            var zakaznici = FindAll();
            zakaznici.RemoveAll(z => z.Id == this.Id);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(zakaznici, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static ZakaznikRow Find(int id)
        {
            return FindAll().FirstOrDefault(z => z.Id == id);
        }

        public static List<ZakaznikRow> FindAll()
        {
            if (!File.Exists(_filePath)) return new List<ZakaznikRow>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ZakaznikRow>>(json) ?? new List<ZakaznikRow>();
        }
    }

    public class ObjednavkaRow
    {
        public int Id { get; set; }
        public int ZakaznikId { get; set; }
        public int MaterialId { get; set; }
        public int Sirka { get; set; }
        public int Vyska { get; set; }
        public int Hloubka { get; set; }
        public string Stav { get; set; }

        private static readonly string _filePath = "objednavky.json";

        public void Insert()
        {
            if (Sirka <= 0 || Vyska <= 0 || Hloubka <= 0)
            {
                throw new ArgumentException("Rozměry musí být větší než 0.");
            }

            var material = MaterialRow.Find(MaterialId);
            if (material == null)
            {
                throw new ArgumentException($"Materiál s ID '{MaterialId}' neexistuje v databázi.");
            }

            var objednavky = FindAll();
            this.Id = objednavky.Any() ? objednavky.Max(o => o.Id) + 1 : 1;
            objednavky.Add(this);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Update()
        {
            var objednavky = FindAll();
            var index = objednavky.FindIndex(o => o.Id == this.Id);
            if (index != -1)
            {
                objednavky[index] = this;
                File.WriteAllText(_filePath, JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public void Delete()
        {
            var objednavky = FindAll();
            objednavky.RemoveAll(o => o.Id == this.Id);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static ObjednavkaRow Find(int id)
        {
            return FindAll().FirstOrDefault(o => o.Id == id);
        }

        public static List<ObjednavkaRow> FindAll()
        {
            if (!File.Exists(_filePath)) return new List<ObjednavkaRow>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ObjednavkaRow>>(json) ?? new List<ObjednavkaRow>();
        }
    }

    public class Program
    {
        public static void Main()
        {
            File.WriteAllText("materialy.json", "[{\"Id\":1,\"Nazev\":\"Dubové dřevo\"}]");

            try
            {
                var zakaznik = new ZakaznikRow { Jmeno = "Jan Novák" };
                zakaznik.Insert();

                var objednavka = new ObjednavkaRow
                {
                    ZakaznikId = zakaznik.Id,
                    MaterialId = 1,
                    Sirka = 1200,
                    Vyska = 750,
                    Hloubka = 600,
                    Stav = "CEKA_NA_POTVRZENI"
                };

                objednavka.Insert();

                var ulozenaObjednavka = ObjednavkaRow.Find(objednavka.Id);

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