# Operational Checklists

Short checklists to help maintenance of the GamewideContext serialization and uniqueness features.

Before making a serialization change
- Update GamewideContextBrainHelpers.GetJsonSerializerSettings() if new converters are required.
- Add or update unit/editor tests to assert round-trip behavior and ledger/hash values.
- Consider NotifyOnly first in staging when changing hash format so you can monitor real data and avoid production failures.

When changing tamper behavior or ledger format
- Update tamper_policy.md and add an annotated migration path in docs if required.
- If ledger storage format changes, append migration tests and an auditor script that can update the LTM ledger for existing items.

When making template IsUnique changes
- Ensure all code paths (converters, factory uses) use CharacterInstance.Create.
- Add tests for both unique and non-unique templates.

Before merging a breaking change
- Run full edit-mode tests; if you modify code that touches AssetDatabase or Resources.Load paths, run editor play-mode tests as well.
- Make sure to clear UniqueInstanceRegistry in test TearDown to avoid cross-test failures.

Debugging quick steps
- If tamper detection fires unexpectedly:
  - Check serializer settings and registered converters.
  - Confirm wrapper.Payload is unchanged (compare payloads / re-compute hashes from payload + version).
  - Check LongTermMemory key generation logic (BuildHashLedgerKey) — ensure id extraction is stable.
