# Character stats — short reference

Two serializable containers:
- `CharacterStat`: unbounded values with bonus. Use for raw properties (Strength, Magic).
- `BoundedCharacterStat`: min/max, suitable for HP, stamina, experience.

Core behavior
- `Get()` => `Current + Bonus`, clamped for bounded stats
- Bounded exposes `Ratio` for UI bars
- Implicit int conversion is provided for quick arithmetic

Types
- `UnboundedStatType` and `BoundedStatType` — use enums to index stats

Where to look
- Source: `Assets/TurnrootFramework/Characters/Components/Stats`

Public methods (examples)
- `BoundedCharacterStat.SetCurrent(float)` / `SetMax(float)` / `SetMin(float)` — set values safely
- `BoundedCharacterStat.Get()` — current + bonus, clamped
- `CharacterStat.SetCurrent(float)` — assign unbounded stat

See also
- [Character](./Character.md) — how stats are referenced by CharacterData