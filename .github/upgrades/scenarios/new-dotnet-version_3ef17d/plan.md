# .NET 10 Upgrade Plan — NetMailArchiver

## 1. Overview

| Parameter | Value |
|-----------|-------|
| **Solution** | `NetMailArchiver.sln` |
| **Current Framework** | .NET 8.0 |
| **Target Framework** | .NET 10.0 (LTS) |
| **Projects** | 4 |
| **Total Issues** | 11 (5 mandatory, 6 potential) |
| **Test Projects** | 0 |
| **global.json** | Not present — no changes needed |
| **Branch** | `upgrade-to-NET10` (from `master`) |

## 2. Project Dependency Graph & Upgrade Order

Projects will be upgraded in topological (dependency) order — foundations first, application last.

```
Level 0:  NetMailArchiver.Models          (no dependencies)
Level 1:  NetMailArchiver.DataAccess      (→ Models)
Level 2:  NetMailArchiver.Services        (→ DataAccess, Models)
Level 3:  NetMailArchiver.Web             (→ Services, DataAccess)  ← top-level app
```

## 3. Upgrade Steps

### 3.1 — NetMailArchiver.Models (Level 0)

| Action | Detail |
|--------|--------|
| Update `TargetFramework` | `net8.0` → `net10.0` |
| NuGet changes | None — no package references |
| Build & verify | Must compile cleanly before proceeding |

### 3.2 — NetMailArchiver.DataAccess (Level 1)

| Action | Detail |
|--------|--------|
| Update `TargetFramework` | `net8.0` → `net10.0` |
| Update `Microsoft.EntityFrameworkCore` | `9.0.0` → `10.0.5` |
| Update `Npgsql.EntityFrameworkCore.PostgreSQL` | `9.0.2` → `10.0.1` |
| Build & verify | Must compile cleanly before proceeding |

### 3.3 — NetMailArchiver.Services (Level 2)

| Action | Detail |
|--------|--------|
| Update `TargetFramework` | `net8.0` → `net10.0` |
| NuGet changes | None — `MailKit 4.9.0`, `Quartz 3.13.1`, `Quartz.AspNetCore 3.13.1` are already compatible |
| Build & verify | Must compile cleanly before proceeding |

### 3.4 — NetMailArchiver.Web (Level 3)

| Action | Detail |
|--------|--------|
| Update `TargetFramework` | `net8.0` → `net10.0` |
| Update `Microsoft.EntityFrameworkCore` | `9.0.0` → `10.0.5` |
| Update `Microsoft.EntityFrameworkCore.Design` | `9.0.0` → `10.0.5` |
| Update `Microsoft.EntityFrameworkCore.Tools` | `9.0.0` → `10.0.5` |
| Update `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | `1.21.0` → `1.23.0` (latest available; assessment flagged incompatibility at 1.21.0 — try update, remove if still fails) |
| Update `Npgsql.EntityFrameworkCore.PostgreSQL` | `9.0.2` → `10.0.1` |
| NuGet — no change needed | `NToastNotify 8.0.0`, `Quartz.Extensions.DependencyInjection 3.14.0` — compatible |
| **Review API behavioral changes** | See §4 below |
| Build & verify | Full solution build after this step |

## 4. API Behavioral Changes (Potential — Review Required)

Two `.NET 10` behavioral changes were detected in `NetMailArchiver.Web\Program.cs`:

### 4.1 `UseExceptionHandler("/Error")` — Line 50

**What changed in .NET 10:** The exception handler middleware behavior may differ at runtime. The API signature is unchanged, but the error-handling pipeline may produce different responses.

**Risk:** Low — this is a standard Razor Pages error handler pattern. The current usage (`app.UseExceptionHandler("/Error")`) remains valid. No code changes expected, but runtime behavior should be spot-checked after upgrade.

### 4.2 `AddHttpClient()` — Line 33

**What changed in .NET 10:** The `IHttpClientFactory` registration behavior may differ. The API signature is unchanged, but default configuration or lifetime behavior may change.

**Risk:** Low — this is a parameterless registration call. No code changes expected, but verify HTTP client functionality works correctly after upgrade.

**Action for both:** No code changes needed. Add a post-upgrade manual verification step to confirm exception handling and HTTP client behavior work as expected.

## 5. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` incompatibility | Medium | Try updating to 1.23.0. If still incompatible, remove the package (only affects Docker container tooling in VS, not runtime) |
| EF Core 10.0.5 breaking changes | Low | No EF Core API behavioral changes detected; standard DbContext usage |
| Npgsql 10.0.1 compatibility | Low | Validated as compatible with net10.0 by assessment tooling |
| No test projects | Medium | No automated regression testing available — manual verification recommended after upgrade |

## 6. Validation Strategy

Since there are no test projects, validation will rely on:

1. **Incremental builds** — Each project built and verified after its changes
2. **Full solution build** — After all projects are updated
3. **Post-upgrade review** — Manual check of the two API behavioral changes in `Program.cs`

## 7. Commit Strategy

One commit per project upgrade phase, plus a final solution-wide validation commit:

1. `Upgrade NetMailArchiver.Models to .NET 10`
2. `Upgrade NetMailArchiver.DataAccess to .NET 10 with EF Core 10.0.5`
3. `Upgrade NetMailArchiver.Services to .NET 10`
4. `Upgrade NetMailArchiver.Web to .NET 10 with all package updates`
5. `Verify full solution build on .NET 10`
