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

## Rigging / +X Layers (per-character)

The repository now supports a per-character "+X" extra-bone layer concept to allow characters to have a base (N) skeleton plus additional bones specific to that character. CharacterData exposes the following fields:

- `HasExtraBoneLayer` (bool) — whether this character includes an additional bone layer.
- `CustomAvatar` (Avatar) — optional Avatar asset to use for characters whose skeleton differs from the project default.
- `AdditionalBonesMask` (AvatarMask) — an AvatarMask marking only the +X bones. Use this with an extra Animator layer (masked) to animate only those bones.
- `AdditionalBoneNames` (string[]) — convenient list of bone names for editor tooling/validation.
- `ExtraLayerController` (RuntimeAnimatorController) — optional per-character controller or override containing specific +X animations.

Usage notes:

- Keep the base animation set (N bones) consistent across characters — create shared animations targeting those bones.
- Put per-character tweaks (the +X bones) on a separate animator layer with an AvatarMask (the `AdditionalBonesMask`) so you can apply character-unique animations without affecting base N bones.
- At runtime your character instantiation flow (CharacterInstance) should:
	- choose `CustomAvatar` if present (or fall back to shared Avatar),
	- create/apply an extra Animator layer using `AdditionalBonesMask`,
	- optionally blend or use `ExtraLayerController` for +X-only animations.

These fields are serialized in `CharacterData` so designers can set them in the inspector. Editor validation checks are performed during OnValidate so designers receive warnings if `HasExtraBoneLayer` is enabled but supporting data is missing.
