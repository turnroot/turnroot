# DefaultCharacterStats — short ref

ScriptableObject that defines default `BoundedCharacterStat` and `CharacterStat` instances for new characters. Stored under `Resources/GameSettings` so it is auto-discovered.

Key points
- Used only during `CharacterData.OnEnable()` when stat lists are empty
- Does not change existing characters; only applies to uninitialized ones

When to use
- Create one global default set to initialize new character assets consistently

Where to look
- Source: `Assets/TurnrootFramework/Characters/DefaultCharacterStats.cs`
