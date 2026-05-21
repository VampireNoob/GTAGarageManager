using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace GTAGarageManager.Models
{
    [Table("garagen")]
    public class Garage : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("foto_path")]
        public string? FotoPath { get; set; }

        [Column("reihenfolge")]
        public int Reihenfolge { get; set; }

        public List<Fahrzeug> Fahrzeuge { get; set; } = new();

        public int BelegteSlots => Fahrzeuge.Count(f => !f.IsLeer);
        public int MaxSlots => Fahrzeuge.Count;
        public string Kapazitaet => $"{BelegteSlots}/{MaxSlots}";
        public bool HatDuplikate => Fahrzeuge.Any(f => f.IsDuplikat);
    }
}