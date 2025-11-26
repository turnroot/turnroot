using Newtonsoft.Json.Linq;

namespace Turnroot.Tests.Editor.Helpers
{
    public static class TamperTestUtils
    {
        /// <summary>
        /// Increment the _currentLevel property inside the encoded wrapper payload and
        /// optionally update the wrapper.Hash so the wrapper remains internally consistent.
        /// Returns a re-encoded Base64 wrapper string.
        /// </summary>
        public static string IncrementPayloadLevel(string encodedWrapper, bool updateHash = false)
        {
            var wrapper =
                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.DecodeWrapperAsJObject(
                    encodedWrapper
                );
            if (wrapper == null)
                return null;
            var payload = JObject.Parse((string)wrapper["Payload"]);
            var lvlToken = payload.SelectToken("_currentLevel");
            if (lvlToken != null && lvlToken.Type == JTokenType.Integer)
            {
                var lvl = lvlToken.ToObject<int>();
                payload["_currentLevel"] = lvl + 1;
            }
            wrapper["Payload"] = payload.ToString(Newtonsoft.Json.Formatting.None);
            if (updateHash)
            {
                wrapper["Hash"] =
                    Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.RecomputeHashFromWrapperJObject(
                        wrapper
                    );
            }
            return Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.EncodeJObjectToBase64(
                wrapper
            );
        }
    }
}
