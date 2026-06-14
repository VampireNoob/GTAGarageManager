<h1 align="center">GTA Online Garage Manager 🚗</h1>

<p align="center">
    A web app built with <a href="https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor">Blazor</a> and <a href="https://dotnet.microsoft.com/languages/csharp">C#</a> to manage GTA Online garages and vehicles.
</p>

<img width="1853" height="713" alt="GTA" src="https://github.com/user-attachments/assets/a439af8a-27c7-4b81-964b-2d30d6eb5e12" />

![GTA Online Garage Manager](DEIN_SCREENSHOT_LINK)

## A web app to manage all your GTA Online garages and vehicles.

You can view a live demo here: https://gtagaragemanager-production.up.railway.app/

## 🙂 Features:
- 🚗 Overview of all garages and vehicles
- ⚠️ Automatic duplicate detection
- ✏️ Edit, add and delete vehicles and garages
- 📸 Photo upload per garage
- 💾 Automatic JSON storage
- 📱 iPad & mobile friendly

## A piece of code – the duplicate detection:
```csharp
var duplikatGruppen = alleFahrzeuge
    .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .ToList();
```

## Built With
* <img src="https://github.com/VampireNoob/Wedding-Wish-List/assets/128150500/c43e4d15-62e4-4254-a673-c4021fd4cf25" width="30"> C#
* Blazor Server (.NET 8)
* JavaScript (SortableJS)
* HTML & CSS

## Contact
GitHub: [VampireNoob](https://github.com/VampireNoob)
