# Kávopíči

**Sdílená aplikace pro hodnocení a porovnávání kávových směsí při firemních degustacích.**

> English: A shared app for coworkers to rate and compare coffee blends during office tastings.

## O projektu

Kávopíči je webová aplikace pro kancelářské degustace kávy. Administrátor nastaví "kávu dne", kolegové ji ohodnotí (1–5 hvězdiček), přidají komentář a chuťové poznámky, a aplikace zobrazí souhrnné statistiky, žebříčky a porovnání směsí.

### Cílové skupiny

- **Admin** — spravuje uživatele, přidává směsi, nastavuje denní kávu, exportuje data.
- **Degustátoři** (5–20 kolegů) — hodnotí kávu dne, prohlížejí statistiky a historii.

---

## Funkce

### Přihlášení
- Výběr profilu ze seznamu (bez hesel — důvěryhodné prostředí).
- Na prvním spuštění volba databáze (vytvoření nové nebo otevření existující) a vytvoření prvního administrátora.

### Nástěnka (Dashboard)
- **Přehled** — kliknutelné karty s nejlépe hodnocenou směsí (→ detail) a počtem vlastních hodnocení (→ statistiky).
- **Kolečko chutí kávy** — odkaz na interaktivní kolečko chutí pro lepší orientaci v chuťových profilech.
- Zobrazení "Káv dne" — administrátor může nastavit více směsí k degustaci, každá se zobrazí jako samostatná karta.
- **Tajné hlasování** — detaily směsi se odhalí až po ohodnocení. Poznámka administrátora je viditelná i před hodnocením, aby uživatelé rozlišili jednotlivé vzorky.
- Hodnocení 1–5 hvězdiček, volitelný komentář a výběr chuťových poznámek (Ovocná, Ořechová, Čokoládová, Karamelová, Květinová, Kořeněná, Citrusová, Medová).
- Úprava vlastního hodnocení.
- **Zpětné hlasování** — pokud nebyla nastavena káva dne a uživatel neohodnotil poslední směsi, na nástěnce se zobrazí karty pro zpětné hodnocení.

### Statistiky
- Souhrnná tabulka směsí (průměr, **kontroverznost**, počet, pražírna, dodavatel, cena/kg, **cena/★**) — sortovatelná podle sloupců.
- **Kontroverznost** — úroveň shody mezi hodnotiteli vypočtená z rozptylu hodnocení (Shoda / Mírný rozpor / Rozpor).
- **Cena za hvězdičku** — poměr ceny za kilogram a průměrného hodnocení; nižší hodnota = lepší hodnota za peníze.
- **Detail směsi** — rozložení hvězdiček, jednotlivá hodnocení s komentáři a poznámkami.
- **Moje hodnocení** — historie všech degustací s možností zpětného ohodnocení zmeškaných směsí a **úpravy existujících hodnocení** (tajné hlasování platí i zpětně).
- **Porovnání směsí** — dvě směsi vedle sebe s rozložením hodnocení.
- **Export CSV** — stažení dat do souboru (včetně kontroverznosti a ceny/★).

### Správa (admin)
- **Uživatelé** — přidání, deaktivace, přidělení/odebrání admin práv (poslední admin nelze odebrat).
- **Směsi** — přidání (název, pražírna, původ, stupeň pražení, dodavatel, hmotnost, cena), úprava existujících směsí a odebrání (soft delete). Cena za 1 kg se automaticky vypočítá z hmotnosti a ceny.
- **Káva dne** — přidání a odebrání směsí pro denní degustaci (lze vybrat více směsí), volitelná poznámka ke každé.
- **Export CSV** — export statistik.

### Automatické aktualizace
- Kontrola nových verzí přes GitHub Releases na pozadí při startu.
- Stažení a instalace MSIX balíčku přímo z aplikace.

---

## Technologie

| Vrstva | Technologie |
|---|---|
| Framework | .NET 8, ASP.NET Core |
| UI | Blazor Server (Interactive SSR), vlastní CSS |
| Databáze | SQLite (WAL mód, busy timeout 5 s) |
| ORM | Entity Framework Core 8.0 |
| Balení | MSIX (Windows), self-contained (multi-platform: win-x64, osx-x64, osx-arm64) |
| Testování | xUnit, Coverlet |
| CI/CD | GitHub Actions |

---

## Architektura

```
Kavopici.sln
├── src/
│   ├── Kavopici.Core/          # Doménová logika, modely, služby, databáze
│   │   ├── Models/             # User, CoffeeBlend, TastingSession, Rating, TastingNote, BlendStatistics
│   │   ├── Data/               # KavopiciDbContext, DbContextFactory, SQLite WAL interceptor
│   │   └── Services/           # UserService, BlendService, SessionService, RatingService,
│   │                           # StatisticsService, CsvExportService, AppSettingsService, IUpdateService
│   └── Kavopici.Web/           # ASP.NET Core aplikace (vstupní bod)
│       ├── Components/
│       │   ├── Pages/          # Login, Dashboard, Statistics, BlendDetail, Comparison, Admin
│       │   ├── Shared/         # StarRating, BlendCard, UserInitials, ControversyBadge
│       │   └── Layout/         # MainLayout (navigace, update banner)
│       ├── Services/           # AppState, UpdateService, UpdateState
│       └── wwwroot/            # CSS, ikony, favicon
└── tests/
    └── Kavopici.Tests/         # Unit testy (UserService, RatingService, SessionService, StatisticsService)
```

### Datový model

```
User                    CoffeeBlend              TastingSession
├── Id                  ├── Id                   ├── Id
├── Name (unique)       ├── Name                 ├── BlendId (FK)
├── IsAdmin             ├── Roaster              ├── Date (DateOnly)
├── IsActive            ├── Origin?              ├── IsActive
└── CreatedAt           ├── RoastLevel (enum)    ├── Comment?
                        ├── SupplierId (FK→User) └── CreatedAt
                        ├── WeightGrams?
                        ├── PriceCzk?
                        ├── PricePerKg? (calc.)
                        ├── IsActive
                        └── CreatedAt

Rating                  TastingNote              RatingTastingNote
├── Id                  ├── Id                   ├── RatingId (PK, FK)
├── BlendId (FK)        └── Name (unique)        └── TastingNoteId (PK, FK)
├── UserId (FK)
├── SessionId (FK)
├── Stars (1–5, check)
├── Comment?
└── CreatedAt

Unique: (UserId, SessionId) — jedno hodnocení na uživatele za sezení.
```

**RoastLevel**: `Light`, `MediumLight`, `Medium`, `MediumDark`, `Dark`

---

## Spuštění pro vývoj

### Požadavky

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build a spuštění

```bash
# Build celého řešení
dotnet build Kavopici.sln

# Spuštění aplikace
dotnet run --project src/Kavopici.Web/Kavopici.Web.csproj
```

Aplikace se spustí na `http://localhost:5201` a automaticky otevře prohlížeč.

### Testy

```bash
dotnet test tests/Kavopici.Tests/Kavopici.Tests.csproj
```

### Publikace

```bash
# Windows (win-x64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r win-x64 --self-contained

# macOS Intel (osx-x64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r osx-x64 --self-contained

# macOS Apple Silicon (osx-arm64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r osx-arm64 --self-contained
```

---

## Instalace (pro uživatele)

Aplikace se distribuuje jako MSIX balíček přes [GitHub Releases](https://github.com/Washek13/Kavopici/releases).

### Prvotní instalace

1. Ze stránky Releases stáhněte soubory `Kavopici.cer` a `Install-Certificate.ps1`.
2. Spusťte `Install-Certificate.ps1` jako Administrátor — nainstaluje podpisový certifikát (stačí jednou).
3. Stáhněte a otevřete `Kavopici-X.Y.Z.msix` — nainstaluje aplikaci.

### Aktualizace

Aplikace kontroluje nové verze automaticky. Pokud je dostupná aktualizace, v záhlaví se zobrazí banner s tlačítkem "Aktualizovat".

---

## Konfigurace

- **Cesta k databázi** — ukládá se do `%APPDATA%/Kavopici/settings.json`. Uživatel ji vybírá při prvním spuštění.
- **SQLite pragmy** — WAL mód, busy timeout 5 s, synchronous NORMAL (nastaveno automaticky přes interceptor).
- **Port** — `http://localhost:5201` (konfigurovatelný v `appsettings.json`).

---

## CI/CD

GitHub Actions workflow (`.github/workflows/build-msix.yml`) se spouští při push tagu `v*`:

1. Build a publish .NET aplikace.
2. Generování tile obrázků z ikony.
3. Vytvoření a podepsání MSIX balíčku.
4. Publikace GitHub Release s balíčkem, certifikátem a instalačním skriptem.

Secrets: `CERT_PFX_BASE64`, `CERT_PASSWORD`.
