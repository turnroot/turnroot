using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Gameplay.Brain.Components;

namespace Turnroot.Gameplay.Brain
{
    public static partial class GamewideContextBrainHelpers
    {
        // Adds an id to a JSON list index stored in LTM if not already present.
        public static void AddToIndexIfMissing(LongTermMemory ltm, string indexKey, string id)
        {
            if (ltm == null || string.IsNullOrEmpty(indexKey) || string.IsNullOrEmpty(id))
            {
                return;
            }

            var indexJson = ltm.Recall(indexKey);
            var index = string.IsNullOrEmpty(indexJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(indexJson);

            if (!index.Contains(id))
            {
                index.Add(id);
                ltm.Remember(indexKey, JsonConvert.SerializeObject(index));
            }
        }
    }
}
