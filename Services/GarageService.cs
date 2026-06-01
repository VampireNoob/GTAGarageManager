using GTAGarageManager.Models;
using Microsoft.AspNetCore.Hosting;
using Supabase;

namespace GTAGarageManager.Services
{
    public class GarageService
    {
        private readonly Client _supabase;
        private readonly string _fotoPfad;

        public List<Garage> Garagen { get; private set; } = new();
        public int GesamtFahrzeuge { get; private set; }
        public int DuplikatTypen { get; private set; }
        public int DuplikatGesamt { get; private set; }

        public GarageService(Client supabase, IWebHostEnvironment env)
        {
            _supabase = supabase;
            _fotoPfad = Path.Combine(env.WebRootPath, "fotos");

            if (!Directory.Exists(_fotoPfad))
                Directory.CreateDirectory(_fotoPfad);
        }

        // ── LADEN ─────────────────────────────────────────────────────

        public async Task LadeAlleGaragen()
        {
            var garaResult = await _supabase
                .From<Garage>()
                .Order("reihenfolge", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var fahrzResult = await _supabase
                .From<Fahrzeug>()
                .Order("slot_nummer", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var fahrzeuge = fahrzResult.Models;

            Garagen = garaResult.Models.Select(g =>
            {
                g.Fahrzeuge = fahrzeuge
                    .Where(f => f.GarageId == g.Id)
                    .ToList();
                return g;
            }).ToList();

            BerechneStatistiken();
        }

        // ── FAHRZEUG CRUD ─────────────────────────────────────────────

        public async Task FahrzeugHinzufuegen(Garage garage, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var fahrzeug = new Fahrzeug
            {
                GarageId = garage.Id,
                Name = name.Trim(),
                SlotNummer = garage.Fahrzeuge.Count + 1
            };
            await _supabase.From<Fahrzeug>().Insert(fahrzeug);
            await LadeAlleGaragen();
        }

        public async Task FahrzeugLoeschen(Fahrzeug fahrzeug)
        {
            // Garage finden
            var garage = Garagen.FirstOrDefault(g => g.Id == fahrzeug.GarageId);

            await _supabase.From<Fahrzeug>()
                .Where(f => f.Id == fahrzeug.Id)
                .Delete();

            await LadeAlleGaragen();

            // Slot-Nummern neu berechnen
            if (garage != null)
            {
                var aktualisierteGarage = Garagen.FirstOrDefault(g => g.Id == garage.Id);
                if (aktualisierteGarage != null)
                {
                    for (int i = 0; i < aktualisierteGarage.Fahrzeuge.Count; i++)
                    {
                        aktualisierteGarage.Fahrzeuge[i].SlotNummer = i + 1;
                        await _supabase.From<Fahrzeug>()
                            .Where(f => f.Id == aktualisierteGarage.Fahrzeuge[i].Id)
                            .Set(f => f.SlotNummer, i + 1)
                            .Update();
                    }
                }
            }
        }

        public async Task FahrzeugAktualisieren(Fahrzeug fahrzeug)
        {
            await _supabase.From<Fahrzeug>()
                .Where(f => f.Id == fahrzeug.Id)
                .Set(f => f.Name, fahrzeug.Name)
                .Update();
            BerechneStatistiken();
        }

        // ── GARAGE CRUD ───────────────────────────────────────────────

        public async Task GarageHinzufuegen(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var garage = new Garage
            {
                Name = name.Trim(),
                Reihenfolge = Garagen.Count
            };
            await _supabase.From<Garage>().Insert(garage);
            await LadeAlleGaragen();
        }

        public async Task GarageLoeschen(Garage garage)
        {
            await _supabase.From<Fahrzeug>()
                .Where(f => f.GarageId == garage.Id)
                .Delete();
            await _supabase.From<Garage>()
                .Where(g => g.Id == garage.Id)
                .Delete();
            await LadeAlleGaragen();
        }

        public async Task GarageUmbenennen(Garage garage, string neuerName)
        {
            if (string.IsNullOrWhiteSpace(neuerName)) return;
            await _supabase.From<Garage>()
                .Where(g => g.Id == garage.Id)
                .Set(g => g.Name, neuerName.Trim())
                .Update();
            garage.Name = neuerName.Trim();
        }

        // ── FOTO ──────────────────────────────────────────────────────

        public async Task FotoSetzen(Garage garage, string dateiName)
        {
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                var alterPfad = Path.Combine(_fotoPfad, garage.FotoPath);
                if (File.Exists(alterPfad)) File.Delete(alterPfad);
            }
            await _supabase.From<Garage>()
                .Where(g => g.Id == garage.Id)
                .Set(g => g.FotoPath, dateiName)
                .Update();
            garage.FotoPath = dateiName;
        }

        public async Task FotoLoeschen(Garage garage)
        {
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                var pfad = Path.Combine(_fotoPfad, garage.FotoPath);
                if (File.Exists(pfad)) File.Delete(pfad);
                await _supabase.From<Garage>()
                    .Where(g => g.Id == garage.Id)
                    .Set(g => g.FotoPath, (string?)null)
                    .Update();
                garage.FotoPath = null;
            }
        }

        public string FotoPfad => _fotoPfad;

        public async Task Speichern()
        {
            BerechneStatistiken();
            await LadeAlleGaragen();
        }

        // ── INTERN ────────────────────────────────────────────────────

        private void BerechneStatistiken()
        {
            var alle = Garagen
                .SelectMany(g => g.Fahrzeuge)
                .Where(f => !f.IsLeer)
                .ToList();

            var dupGruppen = alle
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            var dupSet = dupGruppen
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var f in Garagen.SelectMany(g => g.Fahrzeuge))
                f.IsDuplikat = !f.IsLeer && dupSet.Contains(f.Name);

            GesamtFahrzeuge = alle.Count;
            DuplikatTypen = dupGruppen.Count;
            DuplikatGesamt = dupGruppen.Sum(g => g.Count());
        }
    }
}