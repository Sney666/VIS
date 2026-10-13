using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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

public class ZakaznikGateway
{
    private readonly string _filePath = "zakaznici.json";

    public void Insert(ZakaznikDto dto)
    {
        var zakaznici = FindAll();
        dto.Id = zakaznici.Any() ? zakaznici.Max(z => z.Id) + 1 : 1;
        zakaznici.Add(dto);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(zakaznici, new JsonSerializerOptions { WriteIndented = true }));
    }

    public List<ZakaznikDto> FindAll()
    {
        if (!File.Exists(_filePath)) return new List<ZakaznikDto>();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<ZakaznikDto>>(json) ?? new List<ZakaznikDto>();
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
        File.WriteAllText(_filePath, JsonSerializer.Serialize(objednavky, new JsonSerializerOptions { WriteIndented = true }));
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

public class UnitOfWork
{
    private readonly List<object> _newObjects = new List<object>();
    private readonly List<object> _modifiedObjects = new List<object>();
    private readonly List<object> _removedObjects = new List<object>();

    private readonly ZakaznikGateway _zakaznikGateway = new ZakaznikGateway();
    private readonly ObjednavkaGateway _objednavkaGateway = new ObjednavkaGateway();

    public void RegisterNew(object entity) => _newObjects.Add(entity);
    public void RegisterModified(object entity) => _modifiedObjects.Add(entity);
    public void RegisterRemoved(object entity) => _removedObjects.Add(entity);

    public void Commit()
    {
        try
        {
            foreach (var obj in _newObjects)
            {
                if (obj is ZakaznikDto z) _zakaznikGateway.Insert(z);
            }

            foreach (var obj in _newObjects)
            {
                if (obj is ObjednavkaDto o) _objednavkaGateway.Insert(o);
            }

            foreach (var obj in _modifiedObjects)
            {
                if (obj is ObjednavkaDto o) _objednavkaGateway.Update(o);
            }

            Clear();
        }
        catch (Exception)
        {
            Clear();
            throw;
        }
    }

    private void Clear()
    {
        _newObjects.Clear();
        _modifiedObjects.Clear();
        _removedObjects.Clear();
    }
}

public class ObjednavkaService
{
    private readonly MaterialGateway _materialGateway = new MaterialGateway();

    public int ZpracujObjednavku(string jmenoZakaznika, int materialId, int sirka, int vyska, int hloubka)
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

        var uow = new UnitOfWork();

        var zakaznikDto = new ZakaznikDto
        {
            Jmeno = jmenoZakaznika
        };
        uow.RegisterNew(zakaznikDto);

        var objednavkaDto = new ObjednavkaDto
        {
            ZakaznikId = zakaznikDto.Id,
            MaterialId = materialId,
            Sirka = sirka,
            Vyska = vyska,
            Hloubka = hloubka,
            Stav = "WAITING_FOR_CONFIRMATION"
        };
        uow.RegisterNew(objednavkaDto);

        uow.Commit();

        return objednavkaDto.Id;
    }
}

public class Program
{
    public static void Main()
    {
        File.WriteAllText("materialy.json", "[{\"Id\":1,\"Nazev\":\"Dubové dřevo\"}]");
        File.Delete("zakaznici.json");
        File.Delete("objednavky.json");

        var service = new ObjednavkaService();
        var gateway = new ObjednavkaGateway();

        try
        {
            Console.WriteLine("Zadávám objednávku...");

            int noveId = service.ZpracujObjednavku("Jan Novák", 1, 1200, 750, 600);

            var ulozenaObjednavka = gateway.FindById(noveId);

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