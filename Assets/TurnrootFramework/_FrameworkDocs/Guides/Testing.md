# Testing & CI — what to check

This document lists tests that relate to serialization, tamper handling, uniqueness and editor utilities, and gives guidance on running tests locally in Unity.

Relevant test files
- Assets/TurnrootFramework/Tests/Editor/TamperDetectionTests.cs — tamper and ledger edge-cases
- Assets/TurnrootFramework/Tests/Editor/CharacterInstanceRoundTripTests.cs — round-trip serialization tests
- Assets/TurnrootFramework/Tests/Editor/CharacterUniqueInstanceTests.cs — uniqueness enforcement tests

Running tests locally
- Unity Editor: use the Test Runner window for Edit Mode and Play Mode tests.
- Run headless (CI) with the Unity Test Runner via command line if your CI provider supports it.

Test writing tips
- Make tests deterministic: clear UniqueInstanceRegistry in TearDown when testing uniqueness.
- For tamper tests, use the helper encode/decode helpers in GamewideContextBrain to create simulated tampered payloads.
- When changing serialization settings, consider updating tests to assert that the stored ledger entry matches the expected wrapper.Hash for new format.

Troubleshooting test failures
- False tamper positives: If a serializer setting changed, update the expected wrapper or compute repro hash from wrapper.Payload (what's stored), instead of re-serializing a reconstructed instance.
