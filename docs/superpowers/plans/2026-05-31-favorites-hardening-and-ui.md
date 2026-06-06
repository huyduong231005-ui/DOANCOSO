# Favorites Hardening And UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix Favorites so repeated and concurrent requests do not raise PostgreSQL duplicate-key errors, then complete the Favorites page and authenticated topbar link without bundling unrelated site-wide effects.

**Architecture:** Replace the state-flipping `Toggle` action with an idempotent `Set` command that receives the desired state (`shouldBeFavorite`). The command handler queries Favorites with `IgnoreQueryFilters()`, restores an existing soft-deleted row instead of inserting a duplicate, and performs one bounded recovery attempt if concurrent inserts race on `IX_YeuThich_NguoiDungId_CanHoId`. Keep the UI changes local to the Favorites workflow and use a delegated submit lock to prevent accidental double-submit while the request is in flight.

**Tech Stack:** ASP.NET Core MVC 10, EF Core 10, Npgsql/PostgreSQL, Razor views, vanilla JavaScript, xUnit, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers for .NET.

---

## Confirmed Root Cause

The existing schema intentionally keeps one row for each `(NguoiDungId, CanHoId)` pair:

- `Favorite` inherits soft-delete fields from `BaseEntity`.
- `AppDbContext.ApplySoftDeleteFilter()` hides rows where `IsDeleted == true`.
- `IX_YeuThich_NguoiDungId_CanHoId` is unique and is not filtered by `DaXoa`.
- A normal EF query can therefore return `null` after unfavoriting even though the soft-deleted row still occupies the unique key.

Using `IgnoreQueryFilters()` and restoring the old row fixes the sequential case. It does not fully protect the insert path when two requests race after both observe that no row exists. The implementation below handles both cases.

## Scope

### Included

- Backend Favorites command with explicit desired state.
- Restore of soft-deleted Favorite rows.
- Bounded recovery for concurrent insert conflicts.
- Fast integration tests for the HTTP flow.
- PostgreSQL-backed concurrency test for the real unique index.
- Authenticated topbar link and active styling.
- Favorites page layout, empty state, responsive listing cards.
- A small submit lock limited to Favorite forms.

### Excluded

- Site-wide card tilt, magnetic scroll-to-top, top progress bar, landing-page hover redesign, and unrelated Rentals animation changes.
- Schema changes such as replacing the existing unique index with a partial index.
- Broad CSS cleanup outside selectors touched by the Favorites workflow.

The current worktree already contains unrelated modifications in `t/Views/Home/Rentals.cshtml`, `t/wwwroot/css/luxe-haven.css`, `t/wwwroot/css/rentals-page.css`, and `t/wwwroot/js/site.js`. Preserve them, but do not stage them as part of this feature unless a specific scoped hunk is required below.

Running the existing integration suite can also create untracked files under `t/wwwroot/uploads/listings/`. Treat those files as generated test artifacts: inspect them separately and keep them out of scoped feature commits. Do not remove any pre-existing upload unless its provenance has been verified.

## File Map

### Create

- `t/Application/Commands/Favorites/SetFavoriteCommand.cs`
  - Defines the desired-state command and result type.
- `t/Application/Commands/Favorites/SetFavoriteCommandHandler.cs`
  - Owns Favorite state transitions and bounded conflict recovery.
- `t/wwwroot/js/favorites.js`
  - Locks only Favorite submit buttons until navigation completes.
- `t.Tests/Integration/FavoritesFlowTests.cs`
  - Covers HTTP add, soft-delete, restore, idempotency, page rendering, and form contract.
- `t.PostgresTests/t.PostgresTests.csproj`
  - Separate PostgreSQL integration-test project so provider-specific tests remain explicit.
- `t.PostgresTests/FavoritesPostgresRaceTests.cs`
  - Reproduces concurrent insert pressure against PostgreSQL and asserts recovery.

### Modify

- `t/Program.cs`
  - Registers `SetFavoriteCommandHandler`.
- `t/Controllers/FavoritesController.cs`
  - Replaces state-flipping `Toggle` with idempotent `Set`.
- `t/Views/Home/Rentals.cshtml`
  - Posts `shouldBeFavorite = !isFav` to `Favorites.Set`.
- `t/Views/Home/ApartmentDetail.cshtml`
  - Posts explicit desired Favorite state to `Favorites.Set`.
- `t/Views/Favorites/Index.cshtml`
  - Posts `shouldBeFavorite = false`, renders the scoped Favorites layout.
- `t/Views/Shared/_LuxeTopbar.cshtml`
  - Adds the authenticated Favorites link and active class.
- `t/Views/Shared/_Layout.cshtml`
  - Loads `favorites.js`.
- `t/wwwroot/css/luxe-haven.css`
  - Adds only `.login-link.active` if it is not already present.
- `t.Tests/t.Tests.csproj`
  - No new package is required; modify only if test helpers need an explicit reference.
- `t.slnx`
  - Adds `t.PostgresTests`.

## Result Contract

Use a narrow command contract:

```csharp
public sealed record SetFavoriteCommand(
    string UserId,
    int ApartmentId,
    bool ShouldBeFavorite);

public enum SetFavoriteStatus
{
    Updated,
    Unchanged,
    ApartmentNotFound
}

public sealed record SetFavoriteResult(
    SetFavoriteStatus Status,
    bool IsFavorite);
```

The handler must be idempotent:

| Existing row | Requested state | Result |
| --- | --- | --- |
| none | `true` | insert one active row |
| none | `false` | no-op |
| active | `true` | no-op |
| active | `false` | soft-delete through `_db.Favorites.Remove(existing)` |
| soft-deleted | `true` | restore the same row and clear deletion metadata |
| soft-deleted | `false` | no-op |

## Conflict-Recovery Rules

The handler may retry once after `DbUpdateException` only on the `ShouldBeFavorite == true` insert path:

1. Call `_db.ChangeTracker.Clear()` because failed `SaveChangesAsync()` can leave the attempted Favorite and generated audit entries tracked.
2. Query the same `(UserId, ApartmentId)` with `IgnoreQueryFilters()`.
3. If no matching Favorite exists, rethrow the original exception. This avoids swallowing unrelated database failures.
4. If a matching Favorite exists, execute the desired-state transition one more time.
5. Do not retry more than once. A second `DbUpdateException` must propagate.

Do not manually set `_db.Entry(existing).State = EntityState.Modified` when restoring a tracked entity. Assigning `IsDeleted`, `DeletedAt`, and `DeletedBy` is enough.

---

### Task 0: Record Baseline And Protect Existing Work

**Files:**
- Inspect: `t/Controllers/FavoritesController.cs`
- Inspect: `t/Views/Favorites/Index.cshtml`
- Inspect: `t/Views/Home/Rentals.cshtml`
- Inspect: `t/Views/Home/ApartmentDetail.cshtml`
- Inspect: `t/Views/Shared/_LuxeTopbar.cshtml`
- Inspect: `t/wwwroot/css/luxe-haven.css`
- Inspect: `t/wwwroot/css/rentals-page.css`
- Inspect: `t/wwwroot/js/site.js`

- [ ] **Step 1: Capture the dirty-worktree baseline**

Run:

```powershell
git status --short
git diff --stat
git diff -- t/Controllers/FavoritesController.cs t/Views/Favorites/Index.cshtml t/Views/Home/Rentals.cshtml t/Views/Shared/_LuxeTopbar.cshtml t/wwwroot/css/luxe-haven.css t/wwwroot/css/rentals-page.css t/wwwroot/js/site.js
```

Expected: existing uncommitted UI and Favorites edits are visible. Do not revert them.

If untracked files exist under `t/wwwroot/uploads/listings/`, record them as generated-artifact candidates and keep them unstaged.

- [ ] **Step 2: Verify the current fast test baseline**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --verbosity minimal
```

Expected before implementation: `21` tests pass.

- [ ] **Step 3: Create an isolated branch or worktree if implementation is not already isolated**

Use branch prefix `codex/`, for example:

```powershell
git switch -c codex/favorites-hardening-ui
```

If the current dirty worktree must be preserved as-is, create a separate worktree and manually apply only scoped Favorites hunks there. Never reset or discard the current changes.

---

### Task 1: Add Failing HTTP Tests For Desired-State Behavior

**Files:**
- Create: `t.Tests/Integration/FavoritesFlowTests.cs`
- Reference: `t.Tests/Integration/TestWebApplicationFactory.cs`
- Reference: `t.Tests/Integration/PlannedFlowTests.cs`

- [ ] **Step 1: Add HTTP helpers**

In `FavoritesFlowTests.cs`, add:

- Anti-forgery token extraction matching the pattern already used in `PlannedFlowTests`.
- A registration helper that creates a unique account and returns an authenticated `HttpClient`.
- A helper that posts `/Favorites/Set` with `apartmentId`, `shouldBeFavorite`, `returnUrl`, and `__RequestVerificationToken`.

Use a newly registered user per test so tests do not depend on seeded Favorite state.

- [ ] **Step 2: Write the sequential restore test**

Add:

```csharp
[Fact]
public async Task Set_ShouldSoftDeleteAndRestoreTheSameFavoriteRow()
```

Flow:

1. Register a unique user.
2. Select an existing apartment ID.
3. POST desired state `true`.
4. Query Favorites with `IgnoreQueryFilters()` and store the inserted row ID.
5. POST desired state `false`.
6. Assert that the same row has `IsDeleted == true` and `DeletedAt != null`.
7. POST desired state `true`.
8. Assert that there is still exactly one row, its ID is unchanged, `IsDeleted == false`, `DeletedAt == null`, and `DeletedBy == null`.

- [ ] **Step 3: Write the idempotency test**

Add:

```csharp
[Fact]
public async Task Set_ShouldBeIdempotent_WhenTheSameDesiredStateIsPostedRepeatedly()
```

Post `true` twice and `false` twice. Assert that the database contains exactly one soft-deleted row for the pair and no duplicate rows.

- [ ] **Step 4: Run tests to verify they fail before implementation**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoritesFlowTests" --verbosity minimal
```

Expected: FAIL because `/Favorites/Set` and the desired-state handler do not exist yet.

- [ ] **Step 5: Commit the tests**

```powershell
git add t.Tests/Integration/FavoritesFlowTests.cs
git commit -m "test: cover favorite desired-state flow"
```

---

### Task 2: Implement The Desired-State Favorite Command

**Files:**
- Create: `t/Application/Commands/Favorites/SetFavoriteCommand.cs`
- Create: `t/Application/Commands/Favorites/SetFavoriteCommandHandler.cs`
- Modify: `t/Program.cs`
- Modify: `t/Controllers/FavoritesController.cs`
- Modify: `t/Views/Home/Rentals.cshtml`
- Modify: `t/Views/Home/ApartmentDetail.cshtml`
- Modify: `t/Views/Favorites/Index.cshtml`

- [ ] **Step 1: Add the command contract**

Create `SetFavoriteCommand.cs` using the contract in the **Result Contract** section.

- [ ] **Step 2: Add the minimal sequential handler**

Create `SetFavoriteCommandHandler.cs`:

```csharp
public sealed class SetFavoriteCommandHandler
{
    private readonly AppDbContext _db;

    public SetFavoriteCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SetFavoriteResult> HandleAsync(
        SetFavoriteCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate apartment existence.
        // Query Favorite with IgnoreQueryFilters().
        // Apply the state table above.
        // Save only when a transition is required.
    }
}
```

At this stage, implement the sequential transition table. Add conflict recovery in Task 4 after the PostgreSQL race test fails.

- [ ] **Step 3: Register the handler**

Add to `t/Program.cs` near the other command handlers:

```csharp
builder.Services.AddScoped<SetFavoriteCommandHandler>();
```

- [ ] **Step 4: Replace `Toggle` with `Set`**

Inject `SetFavoriteCommandHandler` into `FavoritesController`.

Replace the old action with:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Set(
    int apartmentId,
    bool shouldBeFavorite,
    string? returnUrl,
    CancellationToken cancellationToken)
```

Controller responsibilities:

- Read the authenticated user ID.
- Call the handler.
- Return `NotFound()` for `ApartmentNotFound`.
- Preserve the existing JSON response shape for fetch callers: `{ ok = true, favorite = result.IsFavorite }`.
- Preserve local-only redirect validation with `Url.IsLocalUrl(returnUrl)`.
- Set a success message only when useful.

Do not keep an internal state-flipping path unless an external caller is identified during implementation.

- [ ] **Step 5: Update all Favorite forms**

Use `rg -n "Favorites.*Toggle|asp-action=\"Toggle\"" t -g '*.cshtml'` to find callers.

Update forms:

```html
<form asp-controller="Favorites" asp-action="Set" method="post" data-favorite-form>
    @Html.AntiForgeryToken()
    <input type="hidden" name="apartmentId" value="..." />
    <input type="hidden" name="shouldBeFavorite" value="..." />
    <input type="hidden" name="returnUrl" value="..." />
</form>
```

Values:

- Rentals card: `!isFav`.
- Apartment detail: `!isFav`.
- Favorites page remove button: `false`.

- [ ] **Step 6: Run the focused tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoritesFlowTests" --verbosity minimal
```

Expected: sequential and idempotency tests PASS.

- [ ] **Step 7: Run the fast regression suite**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --verbosity minimal
```

Expected: all fast tests PASS.

- [ ] **Step 8: Commit**

In an isolated clean worktree, stage the scoped files:

```powershell
git add t/Application/Commands/Favorites/SetFavoriteCommand.cs
git add t/Application/Commands/Favorites/SetFavoriteCommandHandler.cs
git add t/Program.cs
git add t/Controllers/FavoritesController.cs
git add t/Views/Home/Rentals.cshtml
git add t/Views/Home/ApartmentDetail.cshtml
git add t/Views/Favorites/Index.cshtml
git commit -m "fix: make favorite updates idempotent"
```

If implementation is performed in the current dirty worktree instead, stage reviewed scoped patches non-interactively with `git apply --cached <reviewed-scoped.patch>`. Do not stage an entire dirty file that contains unrelated changes.

---

### Task 3: Add A PostgreSQL Race Reproduction

**Files:**
- Create: `t.PostgresTests/t.PostgresTests.csproj`
- Create: `t.PostgresTests/FavoritesPostgresRaceTests.cs`
- Modify: `t.slnx`

- [ ] **Step 1: Create the PostgreSQL integration-test project**

Create `t.PostgresTests/t.PostgresTests.csproj` targeting `net10.0`.

Include:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.12.0" />
<ProjectReference Include="..\t\t.csproj" />
```

Pin the container image in the fixture, for example `postgres:17-alpine`, so provider tests remain reproducible.

- [ ] **Step 2: Register all projects in the solution**

`t.slnx` is currently empty. Register the app, fast tests, and PostgreSQL tests so solution-level commands become useful:

```powershell
dotnet sln .\t.slnx add .\t\t.csproj .\t.Tests\t.Tests.csproj .\t.PostgresTests\t.PostgresTests.csproj
```

Expected: `t.slnx` lists all three projects.

- [ ] **Step 3: Create a deterministic concurrent-insert test**

Add:

```csharp
[Fact]
public async Task SetFavorite_ShouldConvergeToOneActiveRow_WhenTwoAddsRace()
```

Test setup:

1. Start one PostgreSQL Testcontainer.
2. Run `Database.MigrateAsync()`.
3. Insert the minimum user, category, region, and apartment graph required by the model.
4. Create two separate `AppDbContext` instances.
5. Attach a one-shot shared `SaveChangesInterceptor` or barrier so both handlers reach the Favorite insert save after both initial reads have completed.
6. Execute `SetFavoriteCommand(..., ShouldBeFavorite: true)` concurrently in both contexts.
7. Assert both tasks complete without an unhandled exception.
8. Query with `IgnoreQueryFilters()` and assert exactly one active Favorite row exists.

The barrier matters. A test that merely starts two tasks without synchronizing the insert path can pass without exercising the race.

- [ ] **Step 4: Run the test to verify it fails before recovery is added**

Docker Desktop or another Docker-compatible daemon must be running.

Run:

```powershell
dotnet test .\t.PostgresTests\t.PostgresTests.csproj --filter "FullyQualifiedName~SetFavorite_ShouldConvergeToOneActiveRow_WhenTwoAddsRace" --verbosity minimal
```

Expected before Task 4: FAIL with a PostgreSQL duplicate-key error for `IX_YeuThich_NguoiDungId_CanHoId`.

- [ ] **Step 5: Commit the PostgreSQL reproduction**

```powershell
git add t.PostgresTests t.slnx
git commit -m "test: reproduce concurrent favorite insert race"
```

---

### Task 4: Add Bounded Conflict Recovery

**Files:**
- Modify: `t/Application/Commands/Favorites/SetFavoriteCommandHandler.cs`
- Test: `t.PostgresTests/FavoritesPostgresRaceTests.cs`

- [ ] **Step 1: Implement one bounded recovery attempt**

Refactor the transition logic into a private method so the handler can call it once normally and once after a recoverable conflict.

Required shape:

```csharp
try
{
    return await ApplyDesiredStateAsync(command, cancellationToken);
}
catch (DbUpdateException original) when (command.ShouldBeFavorite)
{
    _db.ChangeTracker.Clear();

    var conflictExists = await _db.Favorites
        .IgnoreQueryFilters()
        .AnyAsync(
            f => f.UserId == command.UserId &&
                 f.ApartmentId == command.ApartmentId,
            cancellationToken);

    if (!conflictExists)
        throw;

    return await ApplyDesiredStateAsync(command, cancellationToken);
}
```

Constraints:

- This is one retry only.
- Do not catch a second failure.
- Do not silently accept a conflict unless the matching Favorite row is visible after clearing the tracker.
- Preserve normal AppDbContext audit and soft-delete behavior by using EF entities and `SaveChangesAsync()`, not raw SQL.

- [ ] **Step 2: Run the PostgreSQL race test**

Run:

```powershell
dotnet test .\t.PostgresTests\t.PostgresTests.csproj --filter "FullyQualifiedName~SetFavorite_ShouldConvergeToOneActiveRow_WhenTwoAddsRace" --verbosity minimal
```

Expected: PASS.

- [ ] **Step 3: Add a repeated concurrent-add test**

Add a second test or extend the first test to repeat the synchronized race multiple times with fresh pairs. Assert no duplicates and no unhandled exceptions.

- [ ] **Step 4: Run all backend tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --verbosity minimal
dotnet test .\t.PostgresTests\t.PostgresTests.csproj --no-restore --verbosity minimal
```

Expected: all fast and PostgreSQL tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add t/Application/Commands/Favorites/SetFavoriteCommandHandler.cs
git add t.PostgresTests/FavoritesPostgresRaceTests.cs
git commit -m "fix: recover from concurrent favorite inserts"
```

---

### Task 5: Add A Favorite-Only Submit Lock

**Files:**
- Create: `t/wwwroot/js/favorites.js`
- Modify: `t/Views/Shared/_Layout.cshtml`
- Modify: `t/Views/Home/Rentals.cshtml`
- Modify: `t/Views/Home/ApartmentDetail.cshtml`
- Modify: `t/Views/Favorites/Index.cshtml`
- Test: `t.Tests/Integration/FavoritesFlowTests.cs`

- [ ] **Step 1: Write the form-contract test**

Add:

```csharp
[Fact]
public async Task FavoriteForms_ShouldPostDesiredState_AndExposeSubmitLockHook()
```

Assert rendered Favorite forms:

- Post to `/Favorites/Set`.
- Include `name="shouldBeFavorite"`.
- Include `data-favorite-form`.

- [ ] **Step 2: Run the focused test and confirm failure**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoriteForms_ShouldPostDesiredState" --verbosity minimal
```

Expected before the hook is complete: FAIL.

- [ ] **Step 3: Add delegated submit locking**

Create `t/wwwroot/js/favorites.js`:

```javascript
(function () {
  document.addEventListener("submit", function (event) {
    const form = event.target.closest("[data-favorite-form]");
    if (!form || form.dataset.submitting === "true") return;

    form.dataset.submitting = "true";
    form.setAttribute("aria-busy", "true");

    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = true;
  });
})();
```

Use event delegation so the hook continues to work after soft navigation replaces page content.

- [ ] **Step 4: Load the script**

Add to `t/Views/Shared/_Layout.cshtml` after `site.js`:

```html
<script src="~/js/favorites.js" asp-append-version="true"></script>
```

- [ ] **Step 5: Run focused and fast tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoritesFlowTests" --verbosity minimal
dotnet test .\t.Tests\t.Tests.csproj --no-restore --verbosity minimal
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add t/wwwroot/js/favorites.js
git add t/Views/Shared/_Layout.cshtml
git add t/Views/Home/Rentals.cshtml
git add t/Views/Home/ApartmentDetail.cshtml
git add t/Views/Favorites/Index.cshtml
git add t.Tests/Integration/FavoritesFlowTests.cs
git commit -m "fix: prevent duplicate favorite form submits"
```

When working outside an isolated clean worktree, use a reviewed cached patch for any file that still contains unrelated hunks.

---

### Task 6: Complete The Scoped Favorites UI

**Files:**
- Modify: `t/Controllers/FavoritesController.cs`
- Modify: `t/Views/Favorites/Index.cshtml`
- Modify: `t/Views/Shared/_LuxeTopbar.cshtml`
- Modify: `t/wwwroot/css/luxe-haven.css`
- Test: `t.Tests/Integration/FavoritesFlowTests.cs`

- [ ] **Step 1: Add the page-rendering tests**

Add:

```csharp
[Fact]
public async Task FavoritesPage_ShouldRenderTopbarActiveLinkAndEmptyState()

[Fact]
public async Task FavoritesPage_ShouldRenderSavedApartmentCards()
```

Assert:

- The page contains `_LuxeTopbar` output.
- The authenticated `Yêu thích` link is present.
- The Favorites link receives the active class.
- Empty users see the empty-state copy and Rentals CTA.
- Users with Favorites see listing cards and remove forms posting `shouldBeFavorite=false`.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoritesPage_ShouldRender" --verbosity minimal
```

Expected before UI completion: FAIL.

- [ ] **Step 3: Set active navigation in the controller**

In `FavoritesController.Index()`:

```csharp
ViewData["ActiveNav"] = "favorites";
```

Keep `ActiveNav` ownership in the controller. Remove the duplicate assignment from `Views/Favorites/Index.cshtml`.

- [ ] **Step 4: Add the authenticated topbar link**

In `_LuxeTopbar.cshtml`, next to `Tin của tôi`:

```html
<a class="login-link @(activeNav == "favorites" ? "active" : string.Empty)"
   asp-controller="Favorites"
   asp-action="Index"
   data-soft-nav="true">Yêu thích</a>
```

- [ ] **Step 5: Add active-link styling**

In `luxe-haven.css`, add only if missing:

```css
.login-link.active {
  color: var(--primary);
}
```

- [ ] **Step 6: Finish the Favorites page**

Keep the page scoped and consistent with Rentals:

- Set `ViewData["BodyClass"] = "luxe-rentals"`.
- Render `<partial name="_LuxeTopbar" />`.
- Use `<main>` with `pt-24` so the fixed topbar does not overlap content.
- Render a responsive `grid gap-4 sm:grid-cols-2 lg:grid-cols-3`.
- Keep the glass-style empty state with a Rentals CTA.
- Keep cards structurally aligned with Rentals cards.
- Do not add global 3D tilt or scroll effects in this task.

- [ ] **Step 7: Run the UI-focused tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --filter "FullyQualifiedName~FavoritesFlowTests" --verbosity minimal
```

Expected: PASS.

- [ ] **Step 8: Commit only scoped UI hunks**

```powershell
git add t/Controllers/FavoritesController.cs
git add t/Views/Favorites/Index.cshtml
git add t/Views/Shared/_LuxeTopbar.cshtml
git add t/wwwroot/css/luxe-haven.css
git add t.Tests/Integration/FavoritesFlowTests.cs
git commit -m "feat: complete favorites page navigation and layout"
```

When working outside an isolated clean worktree, use a reviewed cached patch for any file that still contains unrelated hunks.

---

### Task 7: Verify Scope, Quality, And Manual Behavior

**Files:**
- Inspect: all changed files

- [ ] **Step 1: Search for obsolete Favorite toggle callers**

Run:

```powershell
rg -n "Favorites.*Toggle|asp-action=\"Toggle\"|/Favorites/Toggle" t t.Tests t.PostgresTests
```

Expected: no Favorite caller remains on the old state-flipping action.

- [ ] **Step 2: Check diff quality**

Run:

```powershell
git diff --check
git status --short
git diff --stat
```

Expected:

- No new trailing whitespace.
- No accidental staging of site-wide tilt, scroll-to-top, progress-bar, or landing-page animation changes.
- Any pre-existing unrelated dirty files remain preserved and unstaged.
- Generated upload artifacts remain unstaged.

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build .\t\t.csproj --no-restore --verbosity minimal
```

Expected: build succeeds with `0` errors.

- [ ] **Step 4: Run automated suites**

Docker must be running for the PostgreSQL suite.

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --no-restore --verbosity minimal
dotnet test .\t.PostgresTests\t.PostgresTests.csproj --no-restore --verbosity minimal
```

Expected: all tests PASS.

- [ ] **Step 5: Perform manual browser verification**

1. Log in.
2. Open `/Home/Rentals`.
3. Favorite one apartment.
4. Click favorite repeatedly and confirm the button cannot double-submit while navigation is pending.
5. Remove the Favorite, add it again, and repeat.
6. Open `/Favorites`.
7. Confirm the topbar, active `Yêu thích` link, listing grid, and remove button.
8. Remove all Favorites.
9. Confirm the empty state and `Khám phá tin đăng` CTA.

- [ ] **Step 6: Review staged scope before the final commit**

Run:

```powershell
git diff --cached --stat
git diff --cached
```

Expected: staged changes match this plan only.

## Completion Criteria

The feature is complete only when:

- Sequential `add -> remove -> add` reuses one Favorite row.
- Repeated identical desired-state requests are no-ops.
- Synchronized concurrent inserts pass against PostgreSQL without surfacing `23505`.
- All Favorite forms post explicit desired state and lock against accidental resubmission.
- `/Favorites` renders its topbar, active link, cards, and empty state correctly.
- Fast tests, PostgreSQL tests, build, and `git diff --check` pass.
- Unrelated site-wide animation work remains outside the scoped commits.
