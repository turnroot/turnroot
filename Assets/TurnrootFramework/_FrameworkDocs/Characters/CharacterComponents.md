# Character Components
# Character Components — short ref

Small data containers used by `CharacterData`.

Key utilities
- Pronouns — simple serializable set for substitution (`they`, `she`, `he`). Use `Use(text)` to substitute pronoun placeholders.
- SupportRelationship — tracks relationship rank (None..S), progress points and speed multiplier.
- HereditaryTraits — flags for trait inheritance (colors, growths, aptitudes) at character creation.
- CharacterWhich — enumeration for allegiance (Avatar, NPC, Enemy, Friend).
- SerializableDictionary<TKey,TValue> — inspector-friendly dictionary wrapper; stores keys/values as parallel lists and provides Add/TryGetValue.

Where to look
- Source: `Assets/TurnrootFramework/Characters/Components/*`

SupportRelationship
- Tracks relationship level (None..S), points, speed multiplier.
- Used by characters to model support progression; operations on `SupportPoints` drive level ups.

HereditaryTraits
- Flags controlling which traits (colors, growths, aptitudes) are passed to child units.
- Holds bools only — actual values live on parent `CharacterData`.

CharacterWhich
- Enum for allegiance types (Avatar, NPC, Enemy, Friend).

SerializableDictionary<TKey,TValue>
- Inspector-friendly dictionary wrapper; stores keys/values as parallel lists.
- Lazily builds runtime `Dictionary` on access; supports Add/Remove/TryGetValue.

Where to look
- Source: `Assets/TurnrootFramework/Characters/Components` and `Assets/AbstractScripts/SerializableDictionary.cs`
Public methods
- Pronouns: `SetPronounType(string)`, `Get(string)`, `Use(string)` — set/get and render pronouns in text
- SupportRelationship: `InitializeDefaults()`; properties: `Character`, `SupportLevel`, `MaxLevel`, `SupportSpeed`
- HereditaryTraits: exposes bool properties `HairColor`, `FaceShape`, `EyeColor`, `SkinColor`, `Height`, `Aptitudes`, `StatGrowths`
- SerializableDictionary<TKey,TValue>: `ContainsKey`, `TryGetValue`, `Add`, `Remove`, `Clear`, indexer `this[TKey]`, `Dictionary` property

See also
- [Character](./Character.md) — CharacterData and usage of these components
- [Portrait](./Portraits/Portrait.md) — Portrait integration with pronouns/accents
rel.SupportPoints += 10 * rel.SupportSpeed;
