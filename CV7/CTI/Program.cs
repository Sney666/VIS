using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace RezervaceMistnosti
{
    public abstract class Uzivatel
    {
        public int Id { get; set; }
        public string Jmeno { get; set; }
        public string Email { get; set; }

        private List<Rezervace> _rezervace = null;

        public List<Rezervace> GetRezervace(UnitOfWork uow)
        {
            if (_rezervace == null)
            {
                Console.WriteLine($"[Lazy Load] Načítám kolekci rezervací pro uživatele ID {Id} z DB...");
                _rezervace = uow.RezervaceGateway.FindByUzivatelId(Id);
            }
            return _rezervace;
        }
    }

    public class BeznyUzivatel : Uzivatel
    {
        public string Oddeleni { get; set; }
    }

    public class PracovnikRecepce : Uzivatel
    {
        public string PridelenaBudova { get; set; }
        public int UrovenPristupu { get; set; }
    }

    public class Rozmery
    {
        public int Delka { get; set; }
        public int Sirka { get; set; }

        public int SpocitejPlochu() => Delka * Sirka;
    }

    public class Mistnost
    {
        public int Id { get; set; }
        public string Nazev { get; set; }
        public int Kapacita { get; set; }
        public Rozmery Rozmery { get; set; }
    }

    public class Rezervace
    {
        public int Id { get; set; }
        public int UzivatelId { get; set; }
        public int MistnostId { get; set; }
        public int PocetOsob { get; set; }
        public string Stav { get; set; }

        private Uzivatel _uzivatel = null;
        public void NastavUzivatele(Uzivatel u)
        {
            _uzivatel = u;
        }

        public Uzivatel GetUzivatel(UnitOfWork uow)
        {
            if (_uzivatel == null)
            {
                _uzivatel = uow.UzivatelGateway.FindById(UzivatelId);
            }
            return _uzivatel;
        }
    }

    public class IdentityMap
    {
        private readonly Dictionary<string, object> _map = new Dictionary<string, object>();

        public T Get<T>(int id) where T : class
        {
            string key = $"{typeof(T).Name}_{id}";
            return _map.TryGetValue(key, out var obj) ? (T)obj : null;
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
    public class UzivatelGateway
    {
        private readonly SqliteConnection _conn;
        private readonly IdentityMap _map;

        public UzivatelGateway(SqliteConnection conn, IdentityMap map)
        {
            _conn = conn;
            _map = map;
        }

        public void Insert(Uzivatel u, SqliteTransaction tx)
        {
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = "INSERT INTO Uzivatel (Typ, Jmeno, Email) VALUES (@typ, @jmeno, @email); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@jmeno", u.Jmeno);
            cmd.Parameters.AddWithValue("@email", u.Email);

            if (u is BeznyUzivatel)
                cmd.Parameters.AddWithValue("@typ", "B");
            else if (u is PracovnikRecepce)
                cmd.Parameters.AddWithValue("@typ", "R");
            else
                throw new ArgumentException("Neznámý typ uživatele.");

            u.Id = Convert.ToInt32(cmd.ExecuteScalar());

            using var childCmd = _conn.CreateCommand();
            childCmd.Transaction = tx;

            if (u is BeznyUzivatel b)
            {
                childCmd.CommandText = "INSERT INTO BeznyUzivatel (Id, Oddeleni) VALUES (@id, @oddeleni)";
                childCmd.Parameters.AddWithValue("@id", u.Id);
                childCmd.Parameters.AddWithValue("@oddeleni", b.Oddeleni ?? (object)DBNull.Value);
                childCmd.ExecuteNonQuery();
            }
            else if (u is PracovnikRecepce r)
            {
                childCmd.CommandText = "INSERT INTO PracovnikRecepce (Id, PridelenaBudova, UrovenPristupu) VALUES (@id, @budova, @uroven)";
                childCmd.Parameters.AddWithValue("@id", u.Id);
                childCmd.Parameters.AddWithValue("@budova", r.PridelenaBudova ?? (object)DBNull.Value);
                childCmd.Parameters.AddWithValue("@uroven", r.UrovenPristupu);
                childCmd.ExecuteNonQuery();
            }

            _map.Add(u.Id, u);
        }

        public Uzivatel FindById(int id)
        {
            var cached = _map.Get<Uzivatel>(id);
            if (cached != null)
            {
                Console.WriteLine($"[Identity Map] Uživatel ID {id} vrácen přímo z paměti.");
                return cached;
            }

            Console.WriteLine($"[SQL] Čtu uživatele ID {id} z databáze.");

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Typ, Jmeno, Email FROM Uzivatel WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int uId = reader.GetInt32(0);
                string typ = reader.GetString(1);
                string jmeno = reader.GetString(2);
                string email = reader.GetString(3);

                reader.Close();

                Uzivatel u = null;

                if (typ == "B")
                {
                    using var bCmd = _conn.CreateCommand();
                    bCmd.CommandText = "SELECT Oddeleni FROM BeznyUzivatel WHERE Id = @id";
                    bCmd.Parameters.AddWithValue("@id", uId);
                    using var bReader = bCmd.ExecuteReader();

                    if (bReader.Read())
                    {
                        u = new BeznyUzivatel
                        {
                            Id = uId,
                            Jmeno = jmeno,
                            Email = email,
                            Oddeleni = bReader.IsDBNull(0) ? null : bReader.GetString(0)
                        };
                    }
                }
                else if (typ == "R")
                {
                    using var rCmd = _conn.CreateCommand();
                    rCmd.CommandText = "SELECT PridelenaBudova, UrovenPristupu FROM PracovnikRecepce WHERE Id = @id";
                    rCmd.Parameters.AddWithValue("@id", uId);
                    using var rReader = rCmd.ExecuteReader();

                    if (rReader.Read())
                    {
                        u = new PracovnikRecepce
                        {
                            Id = uId,
                            Jmeno = jmeno,
                            Email = email,
                            PridelenaBudova = rReader.IsDBNull(0) ? null : rReader.GetString(0),
                            UrovenPristupu = rReader.GetInt32(1)
                        };
                    }
                }

                if (u != null)
                {
                    _map.Add(u.Id, u);
                }
                return u;
            }
            return null;
        }
    }

    public class MistnostGateway
    {
        private readonly SqliteConnection _conn;
        private readonly IdentityMap _map;

        public MistnostGateway(SqliteConnection conn, IdentityMap map)
        {
            _conn = conn;
            _map = map;
        }

        public void Insert(Mistnost m, SqliteTransaction tx)
        {
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Mistnost (Nazev, Kapacita, Delka, Sirka) VALUES (@nazev, @kapacita, @delka, @sirka); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@nazev", m.Nazev);
            cmd.Parameters.AddWithValue("@kapacita", m.Kapacita);
            cmd.Parameters.AddWithValue("@delka", m.Rozmery.Delka);
            cmd.Parameters.AddWithValue("@sirka", m.Rozmery.Sirka);

            m.Id = Convert.ToInt32(cmd.ExecuteScalar());
            _map.Add(m.Id, m);
        }

        public Mistnost FindById(int id)
        {
            var cached = _map.Get<Mistnost>(id);
            if (cached != null) return cached;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Nazev, Kapacita, Delka, Sirka FROM Mistnost WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var m = new Mistnost
                {
                    Id = reader.GetInt32(0),
                    Nazev = reader.GetString(1),
                    Kapacita = reader.GetInt32(2),
                    Rozmery = new Rozmery
                    {
                        Delka = reader.GetInt32(3),
                        Sirka = reader.GetInt32(4)
                    }
                };
                _map.Add(m.Id, m);
                return m;
            }
            return null;
        }
    }

    public class RezervaceGateway
    {
        private readonly SqliteConnection _conn;
        private readonly IdentityMap _map;

        public RezervaceGateway(SqliteConnection conn, IdentityMap map)
        {
            _conn = conn;
            _map = map;
        }

        public void Insert(Rezervace r, SqliteTransaction tx)
        {
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO Rezervace (UzivatelId, MistnostId, PocetOsob, Stav) 
                                VALUES (@uid, @mid, @pocet, @stav); 
                                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@uid", r.UzivatelId);
            cmd.Parameters.AddWithValue("@mid", r.MistnostId);
            cmd.Parameters.AddWithValue("@pocet", r.PocetOsob);
            cmd.Parameters.AddWithValue("@stav", r.Stav);

            r.Id = Convert.ToInt32(cmd.ExecuteScalar());
            _map.Add(r.Id, r);
        }

        public void Update(Rezervace r, SqliteTransaction tx)
        {
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Rezervace SET Stav = @stav, PocetOsob = @pocet WHERE Id = @id";
            cmd.Parameters.AddWithValue("@stav", r.Stav);
            cmd.Parameters.AddWithValue("@pocet", r.PocetOsob);
            cmd.Parameters.AddWithValue("@id", r.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id, SqliteTransaction tx)
        {
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Rezervace WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public Rezervace FindById(int id)
        {
            var cached = _map.Get<Rezervace>(id);
            if (cached != null)
            {
                return cached;
            }

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UzivatelId, MistnostId, PocetOsob, Stav FROM Rezervace WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var r = new Rezervace
                {
                    Id = reader.GetInt32(0),
                    UzivatelId = reader.GetInt32(1),
                    MistnostId = reader.GetInt32(2),
                    PocetOsob = reader.GetInt32(3),
                    Stav = reader.GetString(4)
                };
                _map.Add(r.Id, r);
                return r;
            }
            return null;
        }

        public List<Rezervace> FindByUzivatelId(int uzivatelId)
        {
            var list = new List<Rezervace>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, UzivatelId, MistnostId, PocetOsob, Stav FROM Rezervace WHERE UzivatelId = @uid";
            cmd.Parameters.AddWithValue("@uid", uzivatelId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                var cached = _map.Get<Rezervace>(id);
                if (cached != null)
                {
                    list.Add(cached);
                }
                else
                {
                    var r = new Rezervace
                    {
                        Id = id,
                        UzivatelId = reader.GetInt32(1),
                        MistnostId = reader.GetInt32(2),
                        PocetOsob = reader.GetInt32(3),
                        Stav = reader.GetString(4)
                    };
                    _map.Add(r.Id, r);
                    list.Add(r);
                }
            }
            return list;
        }
    }

    public class UnitOfWork : IDisposable
    {
        private readonly SqliteConnection _conn;
        public SqliteConnection Connection => _conn;
        public IdentityMap Map { get; }
        public UzivatelGateway UzivatelGateway { get; }
        public MistnostGateway MistnostGateway { get; }
        public RezervaceGateway RezervaceGateway { get; }

        private readonly List<object> _newObjects = new List<object>();
        private readonly List<object> _modifiedObjects = new List<object>();
        private readonly List<object> _removedObjects = new List<object>();

        public UnitOfWork(string connectionString)
        {
            _conn = new SqliteConnection(connectionString);
            _conn.Open();

            Map = new IdentityMap();
            UzivatelGateway = new UzivatelGateway(_conn, Map);
            MistnostGateway = new MistnostGateway(_conn, Map);
            RezervaceGateway = new RezervaceGateway(_conn, Map);
        }

        public void RegisterNew(object entity) => _newObjects.Add(entity);
        public void RegisterModified(object entity) => _modifiedObjects.Add(entity);
        public void RegisterRemoved(object entity) => _removedObjects.Add(entity);

        public void Commit()
        {
            using var tx = _conn.BeginTransaction();
            try
            {
                foreach (var obj in _newObjects)
                    if (obj is Uzivatel u) UzivatelGateway.Insert(u, tx);

                foreach (var obj in _newObjects)
                    if (obj is Mistnost m) MistnostGateway.Insert(m, tx);

                foreach (var obj in _newObjects)
                {
                    if (obj is Rezervace r)
                    {
                        var u = r.GetUzivatel(this);
                        if (u != null && r.UzivatelId == 0)
                            r.UzivatelId = u.Id;

                        RezervaceGateway.Insert(r, tx);
                    }
                }

                foreach (var obj in _modifiedObjects)
                    if (obj is Rezervace r) RezervaceGateway.Update(r, tx);

                foreach (var obj in _removedObjects)
                    if (obj is Rezervace r) RezervaceGateway.Delete(r.Id, tx);

                tx.Commit();
                Clear();
            }
            catch (Exception)
            {
                tx.Rollback();
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

        public void Dispose() => _conn.Dispose();
    }

    public class RezervaceService
    {
        private readonly string _connString;

        public RezervaceService(string connString)
        {
            _connString = connString;
        }

        public int VytvorRezervaciNovemuUzivateli(string jmeno, string email, int mistnostId, int pocetOsob)
        {
            if (pocetOsob <= 0)
                throw new ArgumentException("Počet osob musí být větší než 0.");

            using var uow = new UnitOfWork(_connString);

            var mistnost = uow.MistnostGateway.FindById(mistnostId);
            if (mistnost == null)
                throw new ArgumentException("Zvolená místnost neexistuje.");

            if (pocetOsob > mistnost.Kapacita)
                throw new ArgumentException("Počet osob překračuje kapacitu místnosti.");

            var uzivatel = new BeznyUzivatel { Jmeno = jmeno, Email = email, Oddeleni = "Nové oddělení" };
            uow.RegisterNew(uzivatel);

            var rezervace = new Rezervace
            {
                MistnostId = mistnost.Id,
                PocetOsob = pocetOsob,
                Stav = "WAITING_FOR_CONFIRMATION"
            };
            rezervace.NastavUzivatele(uzivatel);
            uow.RegisterNew(rezervace);

            uow.Commit();

            return rezervace.Id;
        }
    }

    public class Program
    {
        private static string ConnString = "Data Source=rezervace_cti.db";

        public static void Main()
        {
            InicializujDatabazi();

            Console.WriteLine("--- TEST CTI (Class Table Inheritance) ---");

            int idBezny, idRecepce;

            using (var uow = new UnitOfWork(ConnString))
            {
                var bezny = new BeznyUzivatel { Jmeno = "Petr Běžný", Email = "petr@firma.cz", Oddeleni = "Marketing" };
                var recepce = new PracovnikRecepce { Jmeno = "Anna Recepčni", Email = "anna@firma.cz", PridelenaBudova = "Centrála", UrovenPristupu = 3 };

                uow.RegisterNew(bezny);
                uow.RegisterNew(recepce);
                uow.Commit();

                idBezny = bezny.Id;
                idRecepce = recepce.Id;

                Console.WriteLine($"[ULOŽENO] Běžný uživatel uložen s ID: {idBezny} do dvou tabulek (Uzivatel + BeznyUzivatel)");
                Console.WriteLine($"[ULOŽENO] Pracovník recepce uložen s ID: {idRecepce} do dvou tabulek (Uzivatel + PracovnikRecepce)\n");
            }

            using (var uow = new UnitOfWork(ConnString))
            {
                var nactenyBezny = uow.UzivatelGateway.FindById(idBezny);
                if (nactenyBezny is BeznyUzivatel b)
                {
                    Console.WriteLine($"[ÚSPĚCH] Načten: {b.Jmeno}, Oddělení: {b.Oddeleni}");
                }

                var nactenaRecepce = uow.UzivatelGateway.FindById(idRecepce);
                if (nactenaRecepce is PracovnikRecepce r)
                {
                    Console.WriteLine($"[ÚSPĚCH] Načten: {r.Jmeno}, Budova: {r.PridelenaBudova}");
                }
            }
        }

        private static void InicializujDatabazi()
        {
            using var conn = new SqliteConnection(ConnString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                DROP TABLE IF EXISTS Rezervace;
                DROP TABLE IF EXISTS PracovnikRecepce;
                DROP TABLE IF EXISTS BeznyUzivatel;
                DROP TABLE IF EXISTS Uzivatel;
                DROP TABLE IF EXISTS Mistnost;

                CREATE TABLE Uzivatel (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Typ TEXT NOT NULL,
                    Jmeno TEXT NOT NULL,
                    Email TEXT NOT NULL
                );

                CREATE TABLE BeznyUzivatel (
                    Id INTEGER PRIMARY KEY,
                    Oddeleni TEXT,
                    FOREIGN KEY (Id) REFERENCES Uzivatel(Id)
                );

                CREATE TABLE PracovnikRecepce (
                    Id INTEGER PRIMARY KEY,
                    PridelenaBudova TEXT,
                    UrovenPristupu INTEGER,
                    FOREIGN KEY (Id) REFERENCES Uzivatel(Id)
                );

                CREATE TABLE Mistnost (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nazev TEXT NOT NULL,
                    Kapacita INTEGER NOT NULL,
                    Delka INTEGER NOT NULL,
                    Sirka INTEGER NOT NULL
                );

                CREATE TABLE Rezervace (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UzivatelId INTEGER NOT NULL,
                    MistnostId INTEGER NOT NULL,
                    PocetOsob INTEGER NOT NULL,
                    Stav TEXT NOT NULL,
                    FOREIGN KEY (UzivatelId) REFERENCES Uzivatel(Id),
                    FOREIGN KEY (MistnostId) REFERENCES Mistnost(Id)
                );

                INSERT INTO Mistnost (Nazev, Kapacita, Delka, Sirka) VALUES ('Zasedačka A', 10, 5, 4);
            ";
            cmd.ExecuteNonQuery();
        }
    }
}