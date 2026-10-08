using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TdgPattern
{
    public class ZakaznikDto
    {
        public int Id { get; set; }
        public string Jmeno { get; set; }
    }

    public class MaterialDto
    {
        public int Id { get; set; }
        public string Nazev { get; set; }
    }

    public class ObjednavkaDto
    {
        public int Id { get; set; }
        public int ZakaznikId { get; set; }
        public int MaterialId { get; set; }
        public int Sirka { get; set; }
        public int Vyska { get; set; }
        public int Hloubka { get; set; }
        public string Stav { get; set; }
    }

    public class MaterialGateway
    {
        private readonly string _filePath = "materialy.json";

        public MaterialDto FindById(int id)
        {
            if (!File.Exists(_filePath)) return null;
            var json = File.ReadAllText(_filePath);
            var materialy = JsonSerializer.Deserialize<List<MaterialDto>>(json);
            return materialy?.FirstOrDefault(m => m.Id == id);
        }
    }

    public class ObjednavkaGateway
    {
        private readonly string _filePath = "objednavky.json";

        public void Insert(ObjednavkaDto dto)
        {
            var objednavky = FindAll();

            dto.Id = objednavky.Any() ? objednavky.Max(o => o.Id) + 1 : 1;

            objednavky.Add(dto);

            var json = JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public ObjednavkaDto FindById(int id)
        {
            return FindAll().FirstOrDefault(o => o.Id == id);
        }

        public List<ObjednavkaDto> FindAll()
        {
            if (!File.Exists(_filePath)) return new List<ObjednavkaDto>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ObjednavkaDto>>(json) ?? new List<ObjednavkaDto>();
        }

        public void Update(ObjednavkaDto dto)
        {
            var objednavky = FindAll();
            var index = objednavky.FindIndex(o => o.Id == dto.Id);
            if (index != -1)
            {
                objednavky[index] = dto;
                File.WriteAllText(_filePath, JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

    public class ObjednavkaService
    {
        private readonly ObjednavkaGateway _objednavkaGateway = new ObjednavkaGateway();
        private readonly MaterialGateway _materialGateway = new MaterialGateway();

        public int ZpracujObjednavku(int zakaznikId, int materialId, int sirka, int vyska, int hloubka)
        {
            if (sirka <= 0 || vyska <= 0 || hloubka <= 0)
            {
                throw new ArgumentException("Rozměry musí být větší než 0.");
            }

            var material = _materialGateway.FindById(materialId);
            if (material == null)
            {
                throw new ArgumentException($"Materiál s ID '{materialId}' neexistuje v databázi.");
            }

            var dto = new ObjednavkaDto
            {
                ZakaznikId = zakaznikId,
                MaterialId = materialId,
                Sirka = sirka,
                Vyska = vyska,
                Hloubka = hloubka,
                Stav = "CEKA_NA_POTVRZENI"
            };

            _objednavkaGateway.Insert(dto);

            return dto.Id;
        }
    }

    public class Program
    {
        public static void Main()
        {
            File.WriteAllText("materialy.json", "[{\"Id\":1,\"Nazev\":\"Dubové dřevo\"}]");

            var service = new ObjednavkaService();
            var gateway = new ObjednavkaGateway();

            try
            {
                Console.WriteLine("Zadávám objednávku...");

                int noveId = service.ZpracujObjednavku(1, 1, 1200, 750, 600);

                var ulozenaObjednavka = gateway.FindById(noveId);

                if (ulozenaObjednavka != null)
                {
                    Console.WriteLine($"[ÚSPĚCH] Objednávka potvrzena!");
                    Console.WriteLine($"- ID objednávky: {ulozenaObjednavka.Id}");
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