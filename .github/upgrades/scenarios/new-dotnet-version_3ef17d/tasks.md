# NetMailArchiver .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the NetMailArchiver solution upgrade from .NET 8.0 to .NET 10.0. All four projects will be upgraded simultaneously in a single atomic operation.

**Progress**: 0/1 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [▶] TASK-001: Atomic framework and dependency upgrade
**References**: Plan §3

- [▶] (1) Update TargetFramework to net10.0 in all project files per Plan §3 (NetMailArchiver.Models, NetMailArchiver.DataAccess, NetMailArchiver.Services, NetMailArchiver.Web)
- [ ] (2) All project files updated to net10.0 (**Verify**)
- [ ] (3) Update package references per Plan §3.2 and §3.4 (key packages: EF Core 10.0.5, Npgsql 10.0.1)
- [ ] (4) All package references updated (**Verify**)
- [ ] (5) Restore all dependencies
- [ ] (6) All dependencies restored successfully (**Verify**)
- [ ] (7) Build solution and fix any compilation errors
- [ ] (8) Solution builds with 0 errors (**Verify**)
- [ ] (9) Commit changes with message: "TASK-001: Upgrade NetMailArchiver to .NET 10.0"

---
