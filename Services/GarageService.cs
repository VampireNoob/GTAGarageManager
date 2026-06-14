using GTAGarageManager.Models;
using Supabase;

namespace GTAGarageManager.Services
{
    public class GarageService
    {
        private readonly Client _supabase;
        private const string BucketName = "garagen-fotos";

        public List<Garage> Garagen { get; private set; } = new();
        public int GesamtFahrzeuge { get; private set; }
        public int DuplikatTypen { get; private set; }
        public int DuplikatGesamt { get; private set; }

        public GarageService(Client supabase)
        {
            _supabase = supabase;
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

        public async Task FahrzeugAktualisieren(Fahrzeug fahrzeug)
        {
            await _supabase.From<Fahrzeug>()
                .Where(f => f.Id == fahrzeug.Id)
                .Set(f => f.Name, fahrzeug.Name)
                .Update();
            BerechneStatistiken();
        }

        public async Task FahrzeugLoeschen(Fahrzeug fahrzeug)
        {
            var garage = Garagen.FirstOrDefault(g => g.Id == fahrzeug.GarageId);

            await _supabase.From<Fahrzeug>()
                .Where(f => f.Id == fahrzeug.Id)
                .Delete();

            await LadeAlleGaragen();

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

        // ── FOTO (Supabase Storage) ───────────────────────────────────

        public async Task FotoSetzen(Garage garage, byte[] dateiBytes, string dateiName)
        {
            var pfadImBucket = $"{garage.Id}/{Guid.NewGuid()}{Path.GetExtension(dateiName)}";

            await AltesFotoLoeschen(garage);

            await _supabase.Storage
                .From(BucketName)
                .Upload(dateiBytes, pfadImBucket);

            var oeffentlicheUrl = _supabase.Storage
                .From(BucketName)
                .GetPublicUrl(pfadImBucket);

            await _supabase.From<Garage>()
                .Where(g => g.Id == garage.Id)
                .Set(g => g.FotoPath, oeffentlicheUrl)
                .Update();

            garage.FotoPath = oeffentlicheUrl;
        }

        public async Task FotoLoeschen(Garage garage)
        {
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                await AltesFotoLoeschen(garage);

                await _supabase.From<Garage>()
                    .Where(g => g.Id == garage.Id)
                    .Set(g => g.FotoPath, (string?)null)
                    .Update();

                garage.FotoPath = null;
            }
        }

        private async Task AltesFotoLoeschen(Garage garage)
        {
            if (string.IsNullOrEmpty(garage.FotoPath)) return;

            var marker = $"{BucketName}/";
            var index = garage.FotoPath.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) return;

            var pfadImBucket = garage.FotoPath.Substring(index + marker.Length);

            try
            {
                await _supabase.Storage
                    .From(BucketName)
                    .Remove(new List<string> { pfadImBucket });
            }
            catch
            {
                // Falls das Löschen fehlschlägt, ignorieren
            }
        }

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