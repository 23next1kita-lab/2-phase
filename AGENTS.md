# Agent Instructions

## Before pushing to remote

1. Check for Unity 6000.x deprecated APIs:
   ```powershell
   rg "FindObjectOfType|FindFirstObjectByType|FindObjectsOfType|FindObjectsSortMode" Assets/Scripts/ --include "*.cs"
   ```
   Must return no results in `Assets/Scripts/` (Fusion library under `Assets/Photon/` is exempt).

2. If warnings found, replace using this table:

   | Old (deprecated) | New (required) |
   |---|---|
   | `FindObjectOfType<T>()` | `FindAnyObjectByType<T>()` |
   | `FindFirstObjectByType<T>()` | `FindAnyObjectByType<T>()` |
   | `FindObjectsByType<T>(FindObjectsSortMode)` | `FindObjectsByType<T>()` |
   | `FindObjectsOfType<T>()` | `FindObjectsByType<T>()` |

3. Verify no missing `using` directives when adding Fusion type references (e.g., `NetworkRunner` requires `using Fusion;`).

4. After fixes, build in Unity Editor before committing.
