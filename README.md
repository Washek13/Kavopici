# Kávopíči

**Sdílená desktopová aplikace pro hodnocení a porovnávání kávových směsí při firemních degustacích.**

> English: A shared desktop app for coworkers to rate and compare coffee blends during office tastings.

## Project Overview

### Problem Statement

Office coffee tasting sessions lack a structured way to collect ratings, track which blends people enjoy, and compare results over time. Paper notes get lost, spreadsheets are clunky, and there's no easy way to see aggregate preferences across the group.

### Target Users

- **Admin** (1+ persons): Organizes the tastings — manages users, adds blends, selects today's blend. Any existing admin can grant/revoke admin rights to other users.
- **Tasters** (5–20 coworkers): Rate the daily blend, leave comments, browse historical stats.

### Deployment Model

The app is a **single WPF executable** living on a **shared network drive**. All users launch the same `.exe`. Data is stored locally in the same network folder (e.g., SQLite database file or JSON). No server, no internet connection required.

---

## Core Functionality

### Primary Features (MVP)

1. **User Authentication (lightweight)** — Select your profile from a list on launch. No passwords (trusted office environment). Admin is a designated role.
2. **Blend Management** — Admin adds/removes coffee blends with metadata (name, roaster, origin, roast level). Each blend is linked to the coworker who supplied it.
3. **Blend of the Day** — Admin selects which blend is being tasted today. This is prominently displayed for all users.
4. **Rating & Comments** — Users rate the daily blend 1–5 stars and optionally add a text comment. One rating per user per blend-of-the-day session.
5. **Statistics Dashboard** — View aggregate ratings per blend: average score, number of ratings, score distribution, ranking. Filter/sort by various criteria.

### User Flow

```
Launch app from network drive
        │
        ▼
  ┌──────────────────┐
  │ Výběr uživatele   │  (dropdown / list of registered users)
  └───────┬──────────┘
          │
          ▼
  ┌─────────────────────┐
  │   Hlavní přehled     │
  │                      │
  │  Dnešní káva:        │  ← prominently displayed
  │  [Název směsi]       │
  │                      │
  │  [★★★★☆ Hodnotit]   │  ← if not yet rated today
  │  [Přidat komentář]  │
  │                      │
  │  [Statistiky]        │  ← navigate to stats view
  │  [Správa]            │  ← visible only to admins
  └─────────────────────┘
```

**Admin Flow:**
1. Open Admin Panel (Správa)
2. Manage Users → Add name / Remove user / Toggle admin rights for any user
3. Manage Blends → Add blend (name, roaster, origin, roast level, supplier) / Remove blend
4. Set Blend of the Day → Pick from registered blends

**Taster Flow:**
1. Select profile on launch
2. See today's blend → Rate it (1–5 stars) + optional comment
3. Browse statistics → See rankings, averages, own rating history

### Key Interactions

| Action | Result |
|---|---|
| User rates a blend | Star rating + optional comment saved. UI confirms submission. Rating button becomes "Upravit hodnocení" for that session. |
| Admin sets blend of the day | All users see the new blend on their dashboard. Previous ratings remain in history. |
| Admin grants/revokes admin rights | Target user immediately gains or loses access to the admin panel on next navigation/refresh. |
| User views statistics | Sorted table/chart of all blends with avg rating, count, distribution. Clickable for detail view with individual comments. |
| Admin adds a blend | Blend appears in the selectable list. Linked to the coworker who supplied it. |
| Admin removes a user | User no longer appears in profile selection. Historical ratings preserved (marked as inactive). |

---

## Technical Specifications

### Platform

**WPF (.NET 8)** desktop application. Single executable + data files on a shared network folder.

### Tech Stack

| Component | Choice | Rationale |
|---|---|---|
| Framework | WPF (.NET 8) | Native Windows, rich UI, single exe deployment |
| Data Storage | SQLite | Single file DB, no server needed, concurrent read support |
| ORM | Entity Framework Core + SQLite provider | Simplifies data access, migrations |
| Charting | LiveCharts2 or OxyPlot | WPF-native charting for statistics |
| Packaging | Single-file publish (`dotnet publish -c Release -r win-x64 --self-contained`) | One exe, no install |

### Data Model

```
┌──────────────┐     ┌──────────────────┐     ┌──────────────┐
│    User       │     │   CoffeeBlend     │     │   Rating      │
├──────────────┤     ├──────────────────┤     ├──────────────┤
│ Id (int, PK)  │     │ Id (int, PK)      │     │ Id (int, PK)  │
│ Name (string) │     │ Name (string)     │     │ BlendId (FK)  │
│ IsAdmin (bool) │     │ Roaster (string)  │     │ UserId (FK)   │
│ IsActive (bool)│     │ Origin (string?)  │     │ SessionId(FK) │
│ CreatedAt     │     │ RoastLevel (enum) │     │ Stars (1-5)   │
└──────────────┘     │ SupplierId (FK→User)│    │ Comment (str?)│
                      │ IsActive (bool)    │     │ CreatedAt     │
                      │ CreatedAt          │     └──────────────┘
                      └──────────────────┘
                                                  ┌──────────────────┐
                                                  │  TastingSession   │
                                                  ├──────────────────┤
                                                  │ Id (int, PK)      │
                                                  │ BlendId (FK)      │
                                                  │ Date (DateOnly)   │
                                                  │ IsActive (bool)   │
                                                  │ CreatedAt         │
                                                  └──────────────────┘
```

**Key relationships:**
- A `TastingSession` represents one "blend of the day" event. One per day (enforced by app logic, not hard constraint — admin can change mid-day).
- A `Rating` belongs to one `User` and one `TastingSession`. Unique constraint on `(UserId, SessionId)` — one rating per user per session.
- A `CoffeeBlend` has a `Supplier` (FK to `User`) — the coworker who brought it.

**RoastLevel enum:** `Light`, `MediumLight`, `Medium`, `MediumDark`, `Dark`

### State Management

- **All state lives in SQLite** on the network drive. No local caching.
- **File locking:** SQLite handles concurrent reads well. Writes are serialized by SQLite's locking. For this scale (5–20 users, infrequent writes), this is sufficient.
- **No real-time sync:** Users see updated data on navigation/refresh. A manual "Refresh" button on the dashboard is sufficient. No polling or file watchers needed for MVP.

### External Dependencies

None. Fully offline, self-contained.

---

## UI/UX Requirements

### Layout — Main Views

**1. Výběr uživatele (startup — Profile Selection)**
- Simple list or dropdown of active users
- "Enter" or double-click to proceed
- No password field

**2. Hlavní přehled (Dashboard — main view after login)**
- Header: App name "Kávopíči", logged-in user name, Refresh button (Obnovit), Logout button (Odhlásit)
- Prominent card: "Dnešní káva" — blend name, roaster, origin, roast level, supplier name
- If no blend set today: "Dnes nebyla vybrána žádná káva" message
- Rating section: 5 clickable stars + comment text box + Submit button (Odeslat)
- If already rated: show current rating with "Upravit" option
- Navigation: "Statistiky" button, "Správa" button (admins only)

**3. Statistiky (Statistics View)**
- Table: Název směsi | Průměrné hodnocení | Počet hodnocení | Dodavatel | visual bar for avg
- Sortable by any column
- Click a blend → Detail view: list of individual ratings with comments, score distribution chart
- Optional: "Moje hodnocení" tab showing the logged-in user's rating history

**4. Správa (Admin Panel)**
- Tabs or sections:
  - **Uživatelé**: List with Add/Remove buttons. Add = text input for name. Toggle admin role (checkbox or button) for each user.
  - **Směsi**: List with Add/Remove buttons. Add = form (name, roaster, origin, roast level dropdown, supplier dropdown).
  - **Káva dne**: Dropdown to select blend → "Nastavit jako dnešní kávu" button. Shows current selection.

### Key Reusable Components

- `StarRating` — clickable 5-star component (input mode + display mode)
- `BlendCard` — displays blend info (name, roaster, origin, roast, supplier)
- `StatsTable` — sortable data grid for blend statistics
- `BlendDetailView` — expanded view with ratings list + chart

### Design Principles

- **Minimal, clean UI.** This is a quick lunch-break interaction. No onboarding, no tutorials. Everything on-screen should be obvious.
- **Large touch-friendly targets** for the star rating (some users may be on touchscreen laptops).
- **Coffee-inspired color palette:** warm browns, cream/off-white backgrounds, dark text. Accent color for stars (amber/gold).
- **Fast interaction:** Rating the daily blend should take under 10 seconds from app launch.

### Responsive Behavior

Not applicable — WPF desktop app. Design for a minimum window size of **800×600px**. Allow resizing but use a reasonable max-width for content (~1000px centered).

---

## Implementation Priorities

### Phase 1 — MVP

1. SQLite database setup with EF Core migrations
2. Profile selection screen
3. Admin: CRUD users
4. Admin: CRUD coffee blends (with supplier assignment)
5. Admin: Set blend of the day
6. User: Rate blend of the day (1–5 stars + comment)
7. User: View statistics table (avg rating, count per blend)
8. Single-file publish for network drive deployment

**Estimated scope:** Core app is functional. Users can rate, admin can manage, everyone sees stats.

### Phase 2 — Polish

1. Statistics detail view (individual ratings, comments, distribution chart)
2. "My Ratings" history view
3. Edit/update existing rating within the same session
4. Visual polish — coffee-themed styling, star animation
5. Blend of the Day history (past sessions list)
6. Data export (CSV) for the admin

### Phase 3 — Nice to Have

1. Leaderboard / "Most Popular Blend" highlights
2. User avatars or initials
3. Tasting notes vocabulary (predefined tags: fruity, nutty, chocolatey, etc.)
4. Head-to-head blend comparison view
5. Print-friendly stats report

### Out of Scope

- **No authentication / passwords** — trusted office LAN environment
- **No real-time updates / SignalR / push** — manual refresh is fine
- **No web or mobile version** — WPF only
- **No cloud sync or remote access**
- **No multi-language support** — Czech only (all UI strings in Czech)

---

## Success Criteria

### Functional Requirements

- [ ] Admin can add/remove users and blends
- [ ] Admin can grant/revoke admin rights to any other user
- [ ] At least one admin must always exist (prevent last admin from losing rights)
- [ ] Admin can assign a supplier to each blend
- [ ] Admin can set the blend of the day
- [ ] Users can select their profile and rate the daily blend (1–5 stars)
- [ ] Users can add an optional comment to their rating
- [ ] Users can view aggregate statistics for all blends
- [ ] One rating per user per tasting session (enforced)
- [ ] App runs from a network drive without installation
- [ ] Multiple users can use the app concurrently without data corruption

### Performance Targets

- App launch to profile selection: **< 3 seconds**
- Submitting a rating: **< 1 second**
- Loading statistics view: **< 2 seconds** (for up to 50 blends, 500 ratings)
- SQLite file size should stay under **10 MB** for years of typical use

### Testing Approach

- **Manual testing:** Primary approach. Test concurrent access from 2–3 machines on the same network folder.
- **Unit tests:** Data access layer — verify rating uniqueness constraint, CRUD operations, statistics calculations.
- **Concurrency test:** Two users submit ratings simultaneously. Verify both are saved correctly.
- **Edge cases:** See error handling section below.

---

## Development Notes

### Known Constraints

- **UI Language:** All user-facing strings must be in Czech. Code, comments, and variable names remain in English.
- **First-run bootstrapping:** On first launch (empty database), the app must prompt to create the first user who is automatically granted admin rights. Without this, no one can access the admin panel.
- **SQLite concurrent writes:** SQLite uses file-level locking. With WAL mode enabled, concurrent reads are fine. Concurrent writes are serialized but at this scale (seconds apart) there should be no issues. Use WAL mode (`PRAGMA journal_mode=WAL`) and set a reasonable busy timeout (`PRAGMA busy_timeout=5000`).
- **Network drive latency:** File I/O over SMB/CIFS is slower than local disk. Keep DB queries minimal. Don't hold connections open unnecessarily.
- **.NET runtime:** Self-contained publish bundles the runtime (~60–80 MB exe). This is acceptable for a network drive. Alternatively, require .NET 8 runtime pre-installed and publish framework-dependent (~1 MB) — depends on the environment.
- **Single point of failure:** The SQLite file is the single source of truth. If the network drive goes down, the app is unavailable. No mitigation needed beyond standard IT backup practices.

### Security Considerations

- **No authentication.** Anyone with network drive access can open the app and select any profile. This is by design for a trusted office environment.
- **Admin role is trust-based.** Admin flag is stored in the DB. A technically savvy user could edit the SQLite file directly. Acceptable risk.
- **No sensitive data.** The app stores names and coffee opinions. No PII concerns beyond employee names visible to all coworkers anyway.

### Accessibility

- Keyboard navigation for all interactions (Tab, Enter, arrow keys for star rating)
- Sufficient color contrast for text
- Star rating should have text labels (not just visual) for screen readers

### Error Handling

| Scenario | Handling |
|---|---|
| Network drive unavailable | Show clear error: "Nelze se připojit k databázi. Zkontrolujte síťové připojení." Retry button (Zkusit znovu). |
| SQLite locked (concurrent write) | Retry with busy timeout (5s). If still locked, show "Databáze je zaneprázdněná, zkuste to znovu." |
| Duplicate rating attempt | Prevent at UI level (disable submit if already rated). Enforce at DB level (unique constraint). Show message if constraint violated. |
| No blend of the day set | Show "Dnes nebyla vybrána žádná káva" on dashboard. Disable rating UI. |
| Admin removes a blend that has ratings | Soft delete (set `IsActive = false`). Blend disappears from selection but ratings preserved in stats. |
| Admin removes a user | Soft delete. User disappears from profile list. Historical ratings preserved. |
| Corrupt or missing DB file | On startup, if DB doesn't exist, create it with migrations. If corrupt, show error with path to DB file for manual recovery. |
| Last admin tries to revoke own admin rights | Prevent: at least one admin must exist at all times. Show "Nelze odebrat práva poslednímu administrátorovi." |

---

## File Structure (Suggested)

```
Kavopici/
├── Kavopici.sln
├── src/
│   └── Kavopici/
│       ├── App.xaml / App.xaml.cs
│       ├── Models/
│       │   ├── User.cs
│       │   ├── CoffeeBlend.cs
│       │   ├── TastingSession.cs
│       │   └── Rating.cs
│       ├── Data/
│       │   ├── KavopiciDbContext.cs
│       │   └── Migrations/
│       ├── ViewModels/
│       │   ├── LoginViewModel.cs
│       │   ├── DashboardViewModel.cs
│       │   ├── StatisticsViewModel.cs
│       │   └── AdminViewModel.cs
│       ├── Views/
│       │   ├── LoginView.xaml
│       │   ├── DashboardView.xaml
│       │   ├── StatisticsView.xaml
│       │   └── AdminView.xaml
│       ├── Components/
│       │   ├── StarRating.xaml
│       │   └── BlendCard.xaml
│       └── Services/
│           ├── IRatingService.cs
│           └── RatingService.cs
└── tests/
    └── Kavopici.Tests/
```

### Database Location

The SQLite database file (`kavopici.db`) should be stored **next to the executable** on the network drive. The app resolves its path relative to `Assembly.GetExecutingAssembly().Location`. This way, all users hitting the same exe also hit the same database.

```
\\server\share\Kavopici\
├── Kavopici.exe
└── kavopici.db       ← created automatically on first launch
```