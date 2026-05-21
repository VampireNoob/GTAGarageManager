namespace GTAGarageManager.Models
{
    public class Garage
    {
        public string Name { get; set; } = string.Empty;
        public string? FotoPath { get; set; }
        public List<Fahrzeug> Fahrzeuge { get; set; } = new();

        public int BelegteSlots => Fahrzeuge.Count(f => !f.IsLeer);
        public int MaxSlots => Fahrzeuge.Count;
        public string Kapazitaet => $"{BelegteSlots}/{MaxSlots}";
        public bool HatDuplikate => Fahrzeuge.Any(f => f.IsDuplikat);
    }
}