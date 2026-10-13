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

public class IdentityMap
{
    private readonly Dictionary<string, object> _map = new Dictionary<string, object>();

    public T Get<T>(int id) where T : class
    {
        string key = $"{typeof(T).Name}_{id}";
        if (_map.TryGetValue(key, out var obj))
        {
            return (T)obj;
        }
        return null;
    }

    public void Add<T>(int id, T entity) where T : class
    {
        string key = $"{typeof(T).Name}_{id}";
        if (!_map.ContainsKey(key))
        {
            _map.Add(key, entity);
        }
    }
}

public class MaterialGateway
{
    private readonly string _filePath = "materialy.json";

    public MaterialDto FindById(int id, IdentityMap map)
    {
        var cached = map.Get<MaterialDto>(id);
        if (cached != null)
        {
            return cached;
        }

        if (!File.Exists(_filePath)) return null;
        var json = File.ReadAllText(_filePath);
        var materialy = JsonSerializer.Deserialize<List<MaterialDto>>(json);
        var material = materialy?.FirstOrDefault(m => m.Id == id);

        if (material != null)
        {
            map.Add(id, material);
        }

        return material;
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

    public ObjednavkaDto FindById(int id, IdentityMap map)
    {
        var cached = map.Get<ObjednavkaDto>(id);
        if (cached != null)
        {
            return cached;
        }

        var objednavka = FindAll().FirstOrDefault(o => o.Id == id);

        if (objednavka != null)
        {
            map.Add(id, objednavka);
        }

        return objednavka;
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
    private readonly IdentityMap _map;

    public ObjednavkaService(IdentityMap map)
    {
        _map = map;
    }

    public int ZpracujObjednavku(int zakaznikId, int materialId, int sirka, int vyska, int hloubka)
    {
        if (sirka <= 0 || vyska <= 0 || hloubka <= 0)
        {
            throw new ArgumentException("Rozměry musí být větší než 0.");
        }

        var material = _materialGateway.FindById(materialId, _map);
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

        var map = new IdentityMap();
        var service = new ObjednavkaService(map);
        var gateway = new ObjednavkaGateway();

        try
        {
            int noveId = service.ZpracujObjednavku(1, 1, 1200, 750, 600);

            var ulozenaObjednavka1 = gateway.FindById(noveId, map);
            var ulozenaObjednavka2 = gateway.FindById(noveId, map);

            bool isSameInstance = object.ReferenceEquals(ulozenaObjednavka1, ulozenaObjednavka2);
            Console.WriteLine($"Jedná se o úplně stejnou instanci v paměti: {isSameInstance}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CHYBA] {ex.Message}");
        }
    }
}