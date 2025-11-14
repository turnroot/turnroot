# Settings ScriptableObjects

## CharacterPrototypeSettings

**Namespace:** `Turnroot.Characters`  
**Inherits:** `ScriptableObject`  
**Location:** `Resources/GameSettings/*/CharacterPrototypeSettings`

Global settings for character system.

### Creation
```csharp
Assets > Create > Game Settings > CharacterPrototypeSettings
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| *(none)* | - | - | No configurable properties |

### Behavior

**OnValidate (Editor Only):**
- Automatically updates all `CharacterData` assets when settings change
- Finds all CharacterData assets via AssetDatabase
- Marks them dirty and saves changes
- Deferred to avoid import conflicts

---

## GraphicsPrototypesSettings

**Inherits:** `ScriptableObject`  
**Location:** `Resources/GraphicsPrototypesSettings`

Global settings for graphics and portrait rendering.

### Creation
```csharp
Assets > Create > Game Settings > GraphicsPrototypesSettings
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `portraitRenderWidth` | `int` | `512` | Portrait texture width in pixels |
| `portraitRenderHeight` | `int` | `512` | Portrait texture height in pixels |

### Behavior

**OnValidate (Editor Only):**
- Updates all `ImageStack` assets when settings change
- Marks stacks dirty for re-rendering
- Deferred to avoid import conflicts

### Notes
- Used by `ImageStack.PreRender()` for texture dimensions
- Changes affect all portrait rendering
- All image stacks automatically updated on change

---