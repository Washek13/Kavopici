# Kávopíči

**A shared app for coworkers to rate and compare coffee blends during office tastings.**

## About

Kávopíči is a web application for office coffee tastings. An admin sets the "coffee of the day", coworkers rate it (1–5 stars), add a comment and tasting notes, and the app displays summary statistics, leaderboards, and blend comparisons.

### Target users

- **Admin** — manages users, adds blends, sets the daily coffee, exports data.
- **Tasters** (5–20 coworkers) — rate the coffee of the day, browse statistics and history.

---

## Features

### Login
- Profile selection from a list (no passwords — trusted environment).
- **Auto-login** — the selected user is remembered via a browser cookie. On page refresh or app restart, the user is logged in automatically. Logging out clears the cookie.
- On first launch: language selection (🇨🇿 CS / 🇸🇰 SK / 🇬🇧 EN / 🇩🇪 DE), database setup (create new or open existing), and first admin creation.

### Dashboard
- **Overview** — clickable cards for the top-rated blend (→ detail) and your total rating count (→ statistics).
- **Coffee flavor wheel** — link to an interactive flavor wheel for better orientation in taste profiles.
- Multiple "coffees of the day" — admin can set multiple blends for a session, each shown as a separate card.
- **Secret voting** — blend details are revealed only after rating. Admin notes are visible before voting so users can tell samples apart.
- Rating 1–5 stars, optional comment, and tasting note selection (Fruity, Nutty, Chocolatey, Caramel, Floral, Spiced, Citrusy, Honey).
- Edit your own rating.
- **Retroactive voting** — missed blends can be rated later from the "My Ratings" tab in Statistics.

### Statistics
- Summary table of blends (average, **controversy**, count, roaster, supplier, price/kg, **price/★**) — sortable by column.
- **Controversy** — agreement level between raters calculated from score variance (Agreement / Mild disagreement / Disagreement).
- **Price per star** — ratio of price per kilogram to average rating; lower = better value for money.
- **Blend detail** — star distribution, individual ratings with comments and tasting notes.
- **My ratings** — full tasting history with retroactive rating of missed blends and **editing of existing ratings**.
- **Blend comparison** — two blends side by side with rating distribution.
- **CSV export** — download data to a file (including controversy and price/★).

### Admin
- **Users** — add, deactivate, grant/revoke admin rights (last admin cannot be removed).
- **Blends** — add (name, roaster, origin, roast level, supplier, weight, price), edit existing blends and remove (soft delete). Price per kg is calculated automatically from weight and price.
- **Coffee of the day** — add and remove blends for the daily tasting session (multiple blends supported), optional note per blend.
- **CSV export** — export statistics.

### Localization
- Four supported languages: **Czech, Slovak, English, German**.
- Language is selected on first launch and stored in a cookie. Can be changed at any time via the header switcher.
- Everything is translated, including tasting notes, roast levels, and the bug report template.

### Auto-update
- Checks for new versions via GitHub Releases in the background on startup.
- Downloads and installs the MSIX package directly from within the app.

---

## Technology

| Layer | Technology |
|---|---|
| Framework | .NET 8, ASP.NET Core |
| UI | Blazor Server (Interactive SSR), custom CSS |
| Database | SQLite (DELETE journal, busy timeout 30 s, Pooling=False) |
| ORM | Entity Framework Core 8.0 |
| Localization | .NET `IStringLocalizer`, `.resx` resource files (cs, sk, en, de) |
| Packaging | MSIX (Windows), self-contained (multi-platform: win-x64, osx-x64, osx-arm64) |
| Testing | xUnit, Coverlet |
| CI/CD | GitHub Actions |

---

## Architecture

```
Kavopici.sln
├── src/
│   ├── Kavopici.Core/          # Domain logic, models, services, database
│   │   ├── Models/             # User, CoffeeBlend, TastingSession, Rating, TastingNote, BlendStatistics
│   │   ├── Data/               # KavopiciDbContext, DbContextFactory, SQLite pragma interceptor
│   │   └── Services/           # UserService, BlendService, SessionService, RatingService,
│   │                           # StatisticsService, CsvExportService, AppSettingsService, IUpdateService
│   └── Kavopici.Web/           # ASP.NET Core app (entry point)
│       ├── Components/
│       │   ├── Pages/          # Login, Dashboard, Statistics, BlendDetail, Comparison, Admin
│       │   ├── Shared/         # StarRating, BlendCard, UserInitials, ControversyBadge
│       │   └── Layout/         # MainLayout (navigation, update banner)
│       ├── Resources/          # SharedResources.resx + satellites (.cs/.sk/.en/.de)
│       ├── Services/           # AppState, UpdateService, UpdateState
│       └── wwwroot/            # CSS, icons, favicon
└── tests/
    └── Kavopici.Tests/         # Unit tests (UserService, RatingService, SessionService, StatisticsService)
```

### Data model

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

Unique: (UserId, SessionId) — one rating per user per session.
```

**RoastLevel**: `Light`, `MediumLight`, `Medium`, `MediumDark`, `Dark`

---

## Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and run

```bash
# Build the full solution
dotnet build Kavopici.sln

# Run the app
dotnet run --project src/Kavopici.Web/Kavopici.Web.csproj
```

The app starts at `http://localhost:5201` and opens the browser automatically.

### Tests

```bash
dotnet test tests/Kavopici.Tests/Kavopici.Tests.csproj
```

### Publish

```bash
# Windows (win-x64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r win-x64 --self-contained

# macOS Intel (osx-x64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r osx-x64 --self-contained

# macOS Apple Silicon (osx-arm64)
dotnet publish src/Kavopici.Web/Kavopici.Web.csproj -c Release -r osx-arm64 --self-contained
```

---

## Installation (end users)

The app is distributed as an MSIX package via [GitHub Releases](https://github.com/Washek13/Kavopici/releases).

### First-time installation

1. From the Releases page, download `Kavopici.cer` and `Install-Certificate.ps1`.
2. Run `Install-Certificate.ps1` as Administrator — installs the signing certificate (one-time only).
3. Download and open `Kavopici-X.Y.Z.msix` — installs the app.

### Updates

The app checks for new versions automatically. When an update is available, a banner with an "Update" button appears in the header.

---

## Configuration

- **Database path** — stored in `%APPDATA%/Kavopici/settings.json`. Selected by the user on first launch.
- **SQLite pragmas** — DELETE journal, busy timeout 30 s, synchronous NORMAL, Pooling=False (set automatically via interceptor).
- **Port** — `http://localhost:5201` (configurable in `appsettings.json`).

---

## CI/CD

GitHub Actions workflow (`.github/workflows/build-msix.yml`) triggers on push of a `v*` tag:

1. Build and publish the .NET app.
2. Generate tile images from the icon.
3. Create and sign the MSIX package.
4. Publish a GitHub Release with the package, certificate, and install script.

Secrets: `CERT_PFX_BASE64`, `CERT_PASSWORD`.
