# GamewideContext — Examples & Recipes

Short copy-paste samples and step-by-step recipes for GamewideContext serialization and tamper testing.

Examples

1) Encode and write to LongTermMemory ledger (example)

```csharp
// Helper function used by GamewideContextBrain
public static string EncodeInstanceToBase64<T>(T instance)
{
    var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
    var payload = JsonConvert.SerializeObject(instance, settings);
    var versionHex = DateTime.UtcNow.Ticks.ToString("x16");
    var hash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(payload + "|v:" + versionHex);

    var wrapper = new GamewideContextBrainHelpers.SerializedWrapper
    {
        TypeName = typeof(T).FullName,
        Payload = payload,
        Hash = hash,
        Version = versionHex
    };
    var json = JsonConvert.SerializeObject(wrapper);
    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    // Persist ledger entry in LTM:
    var ledgerKey = GamewideContextBrainHelpers.BuildHashLedgerKey(instance, wrapper);
    LongTermMemory.Remember(ledgerKey, wrapper.Hash);

    return encoded;
}
```

2) Decode and validate (example)

```csharp
public static T DecodeInstanceFromBase64<T>(string encoded, out bool tampered)
{
    tampered = false;
    var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
    var wrapper = JsonConvert.DeserializeObject<GamewideContextBrainHelpers.SerializedWrapper>(wrapperJson);

    // recompute hash from wrapper payload & version
    var recomputed = GamewideContextBrainHelpers.ComputeFNV1a64Hex(wrapper.Payload + "|v:" + wrapper.Version);

    if (recomputed != wrapper.Hash)
    {
        tampered = true;
        // handle per-policy
        return GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(wrapper);
    }

    // optional ledger check:
    var ledgerKey = GamewideContextBrainHelpers.BuildHashLedgerKey(default(T), wrapper);
    var ledgerVal = LongTermMemory.Recall(ledgerKey);
    if (!string.IsNullOrEmpty(ledgerVal) && ledgerVal != wrapper.Hash) {
        tampered = true;
        return GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(wrapper);
    }

    var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
    return JsonConvert.DeserializeObject<T>(wrapper.Payload, settings);
}
```

3) Simulating tampering for testing

- To simulate tampering:
  1. Encode instance into wrapper and Base64 string.
  2. Decode wrapper from Base64 and modify wrapper.Payload or wrapper.Hash to create a mismatch.
  3. Pass the altered Base64 string to DecodeInstanceFromBase64 and assert behavior per policy.
