# Characters — UniqueInstance semantics

Template ScriptableObjects (CharacterData) may set `IsUnique` and require special handling to ensure there is only one active runtime instance.

Keypoints
- The `CharacterInstance` constructor is internal; use `CharacterInstance.Create(template)` to create or fetch a runtime instance.
- Unique instances are tracked in `UniqueInstanceRegistry` (in-memory map). Tests should call `UniqueInstanceRegistry.ClearAll()` in TearDown.

Rules for maintainers
- Always use `CharacterInstance.Create(template)` in code and converters when a CharacterInstance should be constructed.
- When you change the uniqueness behavior, add tests for: same-object behavior for unique templates; different-object for non-unique; `TryUnregister` behavior.
