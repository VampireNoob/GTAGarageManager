<h1 align="center">GTA Online Garage Manager 🚗</h1>
<p align="center">
    A web app built with <a href="https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor">Blazor</a> and <a href="https://dotnet.microsoft.com/languages/csharp">C#</a> to manage GTA Online garages and vehicles.
</p>

![GTA Online Garage Manager](DEIN_NEUER_SCREENSHOT_LINK)

## A web app to manage all your GTA Online garages and vehicles.

You can view a live demo here: https://gtagaragemanager.runasp.net/

**Note:** The live demo is password-protected. Please [contact me](https://github.com/VampireNoob) for demo access.

## 🙂 Features:
- 🚗 Overview of all garages and vehicles
- ⚠️ Automatic duplicate detection
- ✏️ Edit, add and delete vehicles and garages
- 📸 Photo upload per garage (Supabase Storage)
- 🔒 Password-protected access (Cookie Authentication)
- ☁️ Cloud database storage (Supabase / PostgreSQL)
- 📱 iPad & mobile friendly

## A piece of code – the duplicate detection:
```csharp
var duplikatGruppen = alleFahrzeuge
    .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .ToList();
```

## Getting Started (local setup)
1. Clone the repository
2. Copy `appsettings.Example.json` to `appsettings.json`
3. Fill in your own Supabase URL, Key, and a password for `SiteAuth:Password`
4. Run the project

## Built With
* <img src="https://github.com/VampireNoob/Wedding-Wish-List/assets/128150500/c43e4d15-62e4-4254-a673-c4021fd4cf25" width="30"> C#
* Blazor Server (.NET 8)
* Supabase (PostgreSQL + Storage)
* Cookie Authentication
* HTML & CSS

## Contact
GitHub: [VampireNoob](https://github.com/VampireNoob)
