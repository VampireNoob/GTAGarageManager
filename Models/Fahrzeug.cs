using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAGarageManager.Models
{
    public class Fahrzeug
    {
        public int SlotNummer { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDuplikat { get; set; }
        public bool IsLeer => string.IsNullOrWhiteSpace(Name) || Name == "/";

        // Anzeigename: leere Slots als "-" darstellen
        public string AnzeigeName => IsLeer ? "─" : Name;
    }
}