using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace GTAGarageManager.Models
{
    [Table("fahrzeuge")]
    public class Fahrzeug : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("garage_id")]
        public long GarageId { get; set; }

        [Column("slot_nummer")]
        public int SlotNummer { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsDuplikat { get; set; }

        [JsonIgnore]
        public bool IsLeer => string.IsNullOrWhiteSpace(Name) || Name == "/";

        [JsonIgnore]
        public string AnzeigeName => IsLeer ? "─" : Name;
    }
}