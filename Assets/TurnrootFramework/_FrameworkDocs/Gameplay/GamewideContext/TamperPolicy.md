# GamewideContext — Tamper Policy & ledger behavior

This document explains the tamper detection and policy options supported by the GamewideContextBrain and associated helpers.

Overview
- The system stores a wrapper with payload, version and a deterministic FNV-1a 64-bit hash.
- A "ledger" entry is stored in LongTermMemory under an obfuscated key (FNV-1a hash of a raw key) and contains the expected wrapper Hash for that instance.
- On decode, the framework performs two checks:
  1. RecomputeHash(payload + version) and compare to wrapper.Hash — protects the payload content from accidental corruption or casual edits.
  2. Compare wrapper.Hash to ledger entry in LongTermMemory — stops casual attackers who edit only the payload in memory or on disk but do not update the ledger.

TamperPolicy modes
- NotifyOnly: When mismatch detected, the system logs / raises an event but returns the object; good for discoverability and non-strict workflows.
- Reject: When mismatch is detected, decoding fails and no replacement object is returned; useful for strict environments.
- Replace: When mismatch is detected the system constructs a safe default instance and returns it (and optionally updates the ledger). This is useful for non-authoritative systems that must keep running.

Operational checklists
- Before forcing a Replace policy to production, ensure your replacement logic creates a safe, minimal instance and does not cause cascading failures.
- For sensitive data, consider strengthening the protection beyond ledger-only (e.g., HMAC keyed machine secret or server-signed claims).

Common troubleshooting
- False positives:
  - Reason: serializer settings changed, or JSON structure changed. Ensure that the hash computation is made only from the stored payload string and not from a re-serialized object.
  - Fix: Recompute the wrapper or update the ledger intentionally with the new wrapper.Hash (audit & record change). Prefer NotifyOnly first to get visibility.

What to edit in code when changing policy behavior
- GamewideContextBrain.cs — where Policy is evaluated and tamper handling occurs
- GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper — update default instance behavior for new types
