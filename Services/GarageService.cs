using GTAGarageManager.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace GTAGarageManager.Services
{
    public class GarageService
    {
        private readonly string _jsonPfad;
        private readonly string _fotoPfad;

        private record GarageJson(string Name, List<string> Autos, string? FotoPath);

        public List<Garage> Garagen { get; private set; } = new();
        public int GesamtFahrzeuge { get; private set; }
        public int DuplikatTypen { get; private set; }
        public int DuplikatGesamt { get; private set; }

        public GarageService(IWebHostEnvironment env)
        {
            _jsonPfad = Path.Combine(env.ContentRootPath, "garagen.json");
            _fotoPfad = Path.Combine(env.WebRootPath, "fotos");

            // Fotos-Ordner anlegen falls nicht vorhanden
            if (!Directory.Exists(_fotoPfad))
                Directory.CreateDirectory(_fotoPfad);

            if (File.Exists(_jsonPfad))
                LadeVonJson();
            else
                LadeStandarddaten();

            BerechneStatistiken();
        }

        // ── FAHRZEUG CRUD ─────────────────────────────────────────────

        public void FahrzeugHinzufuegen(Garage garage, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            garage.Fahrzeuge.Add(new Fahrzeug
            {
                SlotNummer = garage.Fahrzeuge.Count + 1,
                Name = name.Trim()
            });
            SlotNummerNeuBerechnen(garage);
            BerechneStatistiken();
            SpeichereJson();
        }

        public void FahrzeugLoeschen(Garage garage, Fahrzeug fahrzeug)
        {
            garage.Fahrzeuge.Remove(fahrzeug);
            SlotNummerNeuBerechnen(garage);
            BerechneStatistiken();
            SpeichereJson();
        }

        // ── GARAGE CRUD ───────────────────────────────────────────────

        public void GarageHinzufuegen(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Garagen.Add(new Garage { Name = name.Trim() });
            SpeichereJson();
        }

        public void GarageLoeschen(Garage garage)
        {
            // Foto auch löschen wenn vorhanden
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                var pfad = Path.Combine(_fotoPfad, garage.FotoPath);
                if (File.Exists(pfad)) File.Delete(pfad);
            }
            Garagen.Remove(garage);
            BerechneStatistiken();
            SpeichereJson();
        }

        public void GarageUmbenennen(Garage garage, string neuerName)
        {
            if (string.IsNullOrWhiteSpace(neuerName)) return;
            garage.Name = neuerName.Trim();
            SpeichereJson();
        }

        public void GarageVerschieben(int vonIndex, int nachIndex)
        {
            if (vonIndex < 0 || vonIndex >= Garagen.Count) return;
            if (nachIndex < 0 || nachIndex >= Garagen.Count) return;
            var garage = Garagen[vonIndex];
            Garagen.RemoveAt(vonIndex);
            Garagen.Insert(nachIndex, garage);
            SpeichereJson();
        }

        // ── FOTO ──────────────────────────────────────────────────────

        public void FotoSetzen(Garage garage, string dateiName)
        {
            // Altes Foto löschen
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                var alterPfad = Path.Combine(_fotoPfad, garage.FotoPath);
                if (File.Exists(alterPfad)) File.Delete(alterPfad);
            }
            garage.FotoPath = dateiName;
            SpeichereJson();
        }

        public void FotoLoeschen(Garage garage)
        {
            if (!string.IsNullOrEmpty(garage.FotoPath))
            {
                var pfad = Path.Combine(_fotoPfad, garage.FotoPath);
                if (File.Exists(pfad)) File.Delete(pfad);
                garage.FotoPath = null;
                SpeichereJson();
            }
        }

        public string FotoPfad => _fotoPfad;

        public void Speichern()
        {
            BerechneStatistiken();
            SpeichereJson();
        }

        // ── INTERN ────────────────────────────────────────────────────

        private void SlotNummerNeuBerechnen(Garage garage)
        {
            for (int i = 0; i < garage.Fahrzeuge.Count; i++)
                garage.Fahrzeuge[i].SlotNummer = i + 1;
        }

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

        private void LadeVonJson()
        {
            var json = File.ReadAllText(_jsonPfad);
            var daten = JsonSerializer.Deserialize<List<GarageJson>>(json);
            if (daten == null) { LadeStandarddaten(); return; }

            foreach (var d in daten)
            {
                var garage = new Garage { Name = d.Name, FotoPath = d.FotoPath };
                for (int i = 0; i < d.Autos.Count; i++)
                    garage.Fahrzeuge.Add(new Fahrzeug { SlotNummer = i + 1, Name = d.Autos[i] });
                Garagen.Add(garage);
            }
        }

        private void SpeichereJson()
        {
            var daten = Garagen.Select(g => new GarageJson(
                g.Name,
                g.Fahrzeuge.Select(f => f.Name).ToList(),
                g.FotoPath
            )).ToList();

            var json = JsonSerializer.Serialize(daten, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_jsonPfad, json);
        }

        private void LadeStandarddaten()
        {
            var rohdaten = new List<(string Name, List<string> Autos)>
            {
                ("Tinsel Tower, Apt 45",                new() { "Gauntlet", "Sabre Turbo", "Ruiner", "Sentinel XS", "Postlude", "Gauntlet", "Sabre Turbo", "Ruiner", "Sentinel XS", "Tahoma Coupè" }),
                ("Integrity Way, 30",                   new() { "Stafford", "Duke O'Death", "Dukes", "Tornado", "Stallion Cabrio", "Cognoscenti (Gepanzert)", "Stinger GT", "Zion Classic", "Tulip M-100", "RT3000" }),
                ("3 Alta St, Apt 10",                   new() { "Granger", "Astron", "Dukes", "Phoenix", "Ruiner", "Mesa", "Rebel", "Dominator ASP", "Beater Dukes", "Buccaneer" }),
                ("331 Supply St",                       new() { "Peyote Custom Cabrio", "Osiris", "Ruiner ZZ-8", "Sprunk Buffalo", "Cheetah Classic", "BR8", "ETR1", "Sugoi", "Phoenix", "Penumbra" }),
                ("3 Alta St, Apt 57",                   new() { "Tampa GT", "Torero", "Coquette BlackFin Cabrio", "Michelli GT", "Retinue", "300R", "Infernus Classic", "Retinue Mk Ⅱ", "/", "Mamba Cabrio" }),
                ("Clubhaus in Vinewood",                new() { "Bati 801RR", "Carbon RS", "Akuma", "Vader", "Sanches (Beschriftung)", "Street Blazer", "Innovation", "Gargoyle", "Nightblade", "Bagger" }),
                ("Integrity Way, 35",                   new() { "Injection", "Issi Sport", "Bifta", "Raptor", "Hot Rod Blazer", "Hakuchou", "Bagger", "Nemesis", "Veto Modern", "Veto Classic" }),
                ("Firmen-Garage 1",                     new() { "Specter", "Jester", "Massacro", "Khamelion", "Komoda", "Tulip", "Comet Retro Custom", "Comet", "Coquette", "Exemplar", "Carbonizzare", "Feltzer", "Surano", "Rhinehart", "Elegy RH8", "Elegy Retro Custom", "Previon", "Sultan", "Kuruma (Gepanzert)", "Kuruma" }),
                ("Firmen-Garage 2",                     new() { "Imorgon", "V-STR", "Adder", "Banshee", "Monroe", "Ruston", "Hotknife", "Sultan Classic", "Deviant", "Flash GT", "Envisage", "Outlaw", "Vagrant", "Neon", "Thrax", "Itali GTB Custom", "Furia", "Visione", "Taipan", "Deveste Eight" }),
                ("Firmen-Garage 3",                     new() { "Warrener HKR", "Nebula Turbo", "Schwartzer", "8F Drafter", "Dominator", "/", "Vagner", "Itali RSX", "Zorrusso", "Comet S2 Cabrio", "Nero", "Windsor Cabrio", "Tailgater", "Viseris", "Infernus Classic", "Stirling GT", "Mamba", "Osiris", "FMJ", "Rampant Rocket" }),
                ("Basis am Lago Zancudo",               new() { "TM-02 Khanjali" }),
                ("Lieferanteneingang des Nachtclubs",   new() { "Gang Burrito" }),
                ("Nachtclub B2",                        new() { "Swinger", "RE-7B", "Omnis", "Tigon", "Deity", "Tropos-Ralley", "Coquette D10" }),
                ("Nachtclub B3",                        new() { "Blista Kanjo", "190z", "Stinger GT", "Hermes", "Penetrator", "Infernus", "Ardent", "Locust", "Krieger", "Jugular" }),
                ("Nachtclub B4",                        new() { "Shotaro", "Hakuchou Drag", "Sanctus", "Chimera", "Lectro", "Roosevelt Valor", "Roosevelt", "Lurcher", "Stafford", "Broadway" }),
                ("Arenawerkstatt",                      new() { "Vigilante", "Barrage", "MTW", "Scarab (Zukunftsschock)", "Zhaba", "Journey Ⅱ", "Halbkettenfahrzeug", "Sandking XL", "Squaddie" }),
                ("Arenawerkstatt B1",                   new() { "Walton L35", "Nightshark", "Deathbike (Zukunftsschock)", "Scramjet", "Duke O'Death", "ZR380 (Apokalypse)", "Imperator (Zukunftsschock)", "Ellie", "Tornado Rat Rod" }),
                ("Arenawerkstatt B2",                   new() { "Baller ST-D", "Slamtruck", "JB 700W", "Weevil", "Bewaffneter Tampa", "Caracara", "Winky", "Stromberg", "Ardent" }),
                ("Casino-Penthouse",                    new() { "Tempesta", "811 Cabrio", "Banshee 900R", "R88", "PR4", "DR1", "BR8", "Cypher", "Flash GT", "S80RR" }),
                ("Spielhalle",                          new() { "Virtue", "Youga Classic 4x4", "Buffalo S", "Tyrant", "Tezeract", "Vagner", "ETR1", "Nightshade", "Corsita", "Clique Wagon" }),
                ("Unit 2 Popular St",                   new() { "Vigero", "Issi Sport", "Club", "Futo", "Brioso 300", "Prairie", "Verus", "Manchez Scout", "Powersurge" }),
                ("Integrity Way 28",                    new() { "Growler", "Entity XF", "Bestia GTS", "Paragon R", "Windsor Cabrio", "Vectre", "Entity XXR", "Raiden", "Verlierer", "Z-Type" }),
                ("Autowerkstatt",                       new() { "Kanjo SJ", "ZR350", "Jester Classic", "Jester Classic", "Euros", "Jester RR", "Penumbra FF", "Penumbra FF", "Sultan RS", "Sultan RS" }),
                ("Agentur",                             new() { "Jubilee", "Caracara 4x4", "Everon", "Freecrawler", "Terminus", "Baller ST", "Insurgent", "Hellion", "Patriot Mil-Spec", "Cavalcade XL", "Rebla GTS", "Toros", "Dorado", "Seminole Frontier", "Park-Ranger", "Gauntlet Interceptor", "Schafter V12 (Gepanzert)", "Buffalo STX", "Gauntlet Hellfire", "Deluxo" }),
                ("Murrieta Heights",                    new() { "Desert Raid", "Buccaneer", "Stinger", "Sentinel Classic Widebody", "Issi Classic", "Draugur", "Greenwood", "Casco", "Sentinel Classic", "Futo GTX" }),
                ("Tinsel Towers, Apt 29",               new() { "/", "Cyclone", "Tempesta", "Reaper", "SC1", "Paragon R", "Cyclone", "Tempesta", "Reaper", "/" }),
                ("Eclipse Blvd - Ebene 1",              new() { "Brigham", "Romero", "I-Wagen", "Tyrus", "Zentorno", "Brigham", "Gauntlet Classic", "Tailgater S", "Autarch", "Entity MT" }),
                ("Eclipse Blvd - Ebene 2",              new() { "Paragon R (Gepanzert)", "Drift Tampa", "Itali GTO", "Chavos V6", "Coquette Classic", "Paragon R (Gepanzert)", "Drift Walton L35", "Pariah", "Revolter", "Comet SR" }),
                ("Eclipse Blvd - Ebene 3",              new() { "Omnis e-GT", "Calico GTF", "Postlude", "La Coureuse", "Savestra", "Dominator GTX", "Hotring Sabre", "Clique", "GB200", "Savestra" }),
                ("Eclipse Blvd - Ebene 4",              new() { "Comet Safari", "Neo", "Euros", "Tropos-Rallye", "Remus", "Fränken Stange", "Neo", "Impaler SZ", "Rapid GT Classic", "XA-21" }),
                ("Eclipse Blvd - Ebene 5",              new() { "Brawler", "Broadway", "T20", "Nero Custom", "Envisage", "MonstroCiti", "Eudora", "Itali GTB", "Schlagen GT", "Lynx" }),
                ("Kautionsbüro",                        new() { "300R" }),
                ("Textilfabrik",                        new() { "Boor", "Drift Yosemite", "Hotring Sabre", "Dominator GTT", "Sovereign", "Peyote Gasser", "GP1" }),
                ("Terrorbyte",                          new() { "Oppressor MK Ⅱ" }),
            };

            foreach (var (name, autos) in rohdaten)
            {
                var garage = new Garage { Name = name };
                for (int i = 0; i < autos.Count; i++)
                    garage.Fahrzeuge.Add(new Fahrzeug { SlotNummer = i + 1, Name = autos[i] });
                Garagen.Add(garage);
            }
        }
    }
}