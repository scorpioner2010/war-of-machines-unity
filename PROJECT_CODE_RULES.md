# Project Code Rules

## Runtime Components
- Required components must be created and configured in prefabs or scenes.
- Runtime gameplay code must use serialized inspector references to existing components.
- Do not use `AddComponent`, `GetComponent*`, `FindObject*`, `FindObjects*`, `GameObject.Find`, tag searches, or `Resources.FindObjectsOfTypeAll` in gameplay/client/server runtime code.
- If a reference is missing at runtime, log a clear configuration error and skip the feature instead of silently creating or searching for components.

## Editor Exceptions
- Editor-only migration tools may create or find components when fixing prefabs/scenes.
- Do not leave migration-only code in runtime assemblies.
