# GamewideContextBrain (Brain System)

This document explains GamewideContextBrain, how Data→Instance conversion and instance serialization works, and what to do when you introduce new Data/Instance types.

## Overview
GamewideContextBrain is the central place the framework uses to convert Design-time "Data" (ScriptableObject templates stored in Resources) into runtime "Instance" objects, and to serialize/deserialize instances to and from the `LongTermMemory` string storage.

Key features:
- Stores a single opaque string per instance (Base64 wrapper around JSON) to discourage casual editing.
- Contains the central factory/rehydration logic — all Data→Instance conversion should go through this brain.
- Uses Newtonsoft.Json with small helpers and converters to preserve UnityEngine.Object references (via `Resources` at runtime).

## Wrapper format
We keep the encoding opaque and compact:
- Wrapper JSON: { TypeName, Payload, Hash, Version }
- Base64 Encoded: JSON → UTF8 → Base64

Notes:
 - The `Hash` is a deterministic, non-cryptographic FNV-1a 64-bit hex over the JSON payload. It is stable across sessions and used for tamper detection. If the recomputed hash doesn't match the stored hash, `GamewideContextBrain` raises the `Brain.OnIllegallyModifiedFileDetected` event.
- `Version` helps future proof changes to the serialization layout.

## Unity object resolution
- At runtime we assume Data assets are available via `Resources.Load(name, type)` (because the project stores data assets in Resources).
- In the editor (developer workflows), converters may also resolve assets via `AssetDatabase` (GUID/path).
- If your project uses Addressables or another resolver, add an `IAssetResolver` adapter or update the converters accordingly.

## Editor test helpers

There is an editor-only helper component `GamewideContextBrainTester` under
`Assets/TurnrootFramework/Gameplay/Brain/Components/` that provides NaughtyAttributes
buttons for quick encode/decode/tamper tests. Use that component (place it on the same
GameObject as `GamewideContextBrain`) and assign a `CharacterData` to exercise the
round-trip and tamper detection features. The tester uses the same runtime converters and
serialization pipeline so it is a safe debugging tool.

## Adding a new Data→Instance pair
1. Data side: Create a `ScriptableObject` (e.g., `MyData : ScriptableObject`) and keep assets in `Resources/`.
2. Instance side: Implement a runtime class `MyInstance` with:
   - `[Serializable]` fields that will be persisted, and `SerializeField` for private fields.
   - A constructor that accepts the Data template (`MyInstance(MyData template)`) and performs initialization.
   - A parameterless constructor or make the class deserializable by a custom converter.
   - Implement `Turnroot.Serialization.IPostDeserialize.OnAfterDeserialize()` to repair lists/arrays or run light re-initialization after JSON deserialization (do NOT overwrite fields that came from the saved payload).
3. Registration: If you need special behavior (constructor must run), either:
   - Create a custom `JsonConverter` (see `Assets/TurnrootFramework/Gameplay/Brain/Components/CharacterInstanceJsonConverter.cs` as an example) that calls the constructor when a template is available and sets the private fields reflectively; or
   - Ensure your instance has a parameterless constructor and `OnAfterDeserialize()` so the default serializer will hydrate it correctly.
4. Add tests: Add an editor test that round-trips Data→Instance → EncodeInstanceToString → DecodeInstanceFromString and asserts that key runtime fields are preserved.

## Converters and helpers
- Converters live under `Assets/TurnrootFramework/Gameplay/Brain/Components/`.
- `UnityObjectJsonConverter` — serializes `UnityEngine.Object` references as compact tokens (type, name, assetPath, guid). Runtime reload uses `Resources.Load(name, type)`.
- `CharacterInstanceJsonConverter` — shows how to reconstruct an instance via the template constructor and reflectively set private fields.

## Best practices
- Keep Data assets in `Resources` (or write an adapter for Addressables) to guarantee runtime resolution.
- Prefer `IPostDeserialize` (OnAfterDeserialize) + parameterless construction unless you need constructor initialization; that keeps serialization simple.
- Keep the encoded string opaque (Base64) so players are less likely to tinker; don't rely on the hash for security.
- If you change serialized field names or structure, increment the wrapper `Version` and add migration code to handle older versions.

## Where to look in the code
- `Assets/TurnrootFramework/Gameplay/Brain/Segments/GamewideContextBrain.cs` — main Brain encoder/decoder and instantiation entry points.
- `Assets/TurnrootFramework/Gameplay/Brain/Components/UnityObjectJsonConverter.cs` — Unity object resolution.
- `Assets/TurnrootFramework/Gameplay/Brain/Components/CharacterInstanceJsonConverter.cs` — example for constructor-based rehydration.

If you'd like I can also add an `AddressablesAssetResolver` sample and an `IAssetResolver` interface to make runtime resolution pluggable.
