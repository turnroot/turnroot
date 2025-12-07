# GamewideContext — Serialization & wrapper format

This document explains how the GamewideContext serializes runtime instances, the wrapper format used for tamper checks, and where to look for custom converters.

Core format
- Serialized objects are wrapped in a small payload envelope to make tamper checking deterministic and simple to audit:

  SerializedWrapper {
    TypeName: string,   // typename (optional diagnostic)
    Payload: string,    // json payload (Newtonsoft.Json)
    Hash: string,       // FNV-1a 64-bit hex over Payload + Version
    Version: string     // 16-digit hex ticks value
  }

- The wrapper is then encoded as UTF-8 and Base64 for storage/transfer.

Hashing
- The framework uses a deterministic FNV-1a 64-bit hex string (lowercase) so the hash is stable between platforms.
- Hash is computed over: jsonPayload + "|v:" + versionHex
- Version is encoded as a 16-digit hex, usually based on DateTime.UtcNow.Ticks at encode-time.

JSON settings & converters
- Json.NET is used (Newtonsoft.Json) with TypeNameHandling.Auto and a list of custom converters in GamewideContextBrainHelpers.GetJsonSerializerSettings().
- Important converters:
  - UnityObjectJsonConverter — serializes UnityEngine.Object references by asset path / guid / name so editor/runtime rehydration works.
  - CharacterInstanceJsonConverter — ensures CharacterInstance uses the right constructor or Create factory during read.
  - ObjectItemInstanceJsonConverter (and generic SampleInstanceJsonConverter) — handle instances backed by ScriptableObject templates so constructors run on deserialization.

Where to look in code
- Helpers, converters and wrapper helpers are in:
  - Assets/TurnrootFramework/Gameplay/Brain/Components/GamewideContext/GamewideContextBrainHelpers.cs
  - Assets/TurnrootFramework/Gameplay/Brain/Components/JSON/* (individual converters)

Operating notes
- When updating converters or changing JSON structure, update the modification-check hash computation accordingly — it derives from the serialized payload string, so changes in serializer settings or type names may change hashes.
- Ensure all read-time converters construct instances through their public factory (e.g., CharacterInstance.Create) to maintain runtime invariants (like `IsUnique`).

Quick sample — encode/decode
```csharp
// Encoding an instance to a Base64 string:
var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
var payloadJson = JsonConvert.SerializeObject(instance, settings);
var versionHex = DateTime.UtcNow.Ticks.ToString("x16");
var hash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(payloadJson + "|v:" + versionHex);
var wrapper = new GamewideContextBrainHelpers.SerializedWrapper
{
    TypeName = instance.GetType().FullName,
    Payload = payloadJson,
    Hash = hash,
    Version = versionHex
};
var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(wrapper)));

// Decoding happens through GamewideContextBrain which validates the wrapper hash and ledger.
```

Notes on deterministic re-serialization
- To avoid false tamper detections, hashes should be computed from the wrapper payload string and the version hex explicitly — do not rely on post-deserialize re-serialization to compute a check value.
