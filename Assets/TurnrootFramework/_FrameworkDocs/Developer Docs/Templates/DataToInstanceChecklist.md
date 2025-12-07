# Data → Instance Checklist (template)

Use this checklist when adding a new Data (ScriptableObject) → Instance (runtime) pair.

1. Data asset (ScriptableObject)
   - Put the asset in Resources/ (or add an IAssetResolver implementation for Addressables).
   - Make fields serializable and provide sensible defaults in OnEnable/OnValidate.

2. Instance (runtime object)
   - Add `[Serializable]` and `SerializeField` backing fields.
   - If construction requires a Data template and heavy initialization, add a constructor `MyInstance(MyData template)` and write a custom JsonConverter that calls the constructor during deserialization.
   - Otherwise, provide a parameterless constructor and implement `Turnroot.Serialization.IPostDeserialize` so `OnAfterDeserialize()` can repair lists/arrays and finalize initialization.
   - Avoid calling heavy runtime-only systems in constructor. Use `OnAfterDeserialize` to re-hook lightweight state.

3. Serialization & Converters
   - If you need to run the instance constructor at deserialization time, implement a `JsonConverter` (see sample) and register it via `GamewideContextBrain.GetJsonSerializerSettings()`.
   - Otherwise rely on default Json.NET behavior + `IPostDeserialize`.

4. Tests
   - Add an editor test that round-trips the instance using `GamewideContextBrain.EncodeInstanceToString()` and `DecodeInstanceFromString()` and asserts the important runtime fields (IDs, level, inventory counts, etc.).

5. Documentation
   - Add a short entry describing the Data→Instance pair under `_FrameworkDocs/` and link to the main GamewideContextBrain doc.


## Sample usage notes
- Use the `Resources` folder for runtime asset resolution, or implement `IAssetResolver` if using Addressables.
- Keep the encoded representation opaque (Base64 wrapper) to discourage casual editing.
- Version the wrapper to handle future migrations.
