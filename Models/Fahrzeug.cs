using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

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

        public bool IsDuplikat { get; set; }
        public bool IsLeer => string.IsNullOrWhiteSpace(Name) || Name == "/";
        public string AnzeigeName => IsLeer ? "─" : Name;
    }
}