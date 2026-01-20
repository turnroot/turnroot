using System;
using System.Collections.Generic;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Lightweight cache for UIFade components on GameObjects to avoid repeated TryGetComponent calls.
    /// Uses WeakReference to avoid pinning components longer than necessary.
    /// </summary>
    public static class UIFadeCache
    {
        private static readonly Dictionary<int, WeakReference<UIFade>> Cache =
            new Dictionary<int, WeakReference<UIFade>>();

        public static UIFade Get(GameObject instance)
        {
            if (instance == null)
            {
                return null;
            }

            int id = instance.GetInstanceID();
            if (
                Cache.TryGetValue(id, out var wr)
                && wr.TryGetTarget(out var cached)
                && cached != null
            )
            {
                return cached;
            }

            if (instance.TryGetComponent<UIFade>(out var uiFade))
            {
                Cache[id] = new WeakReference<UIFade>(uiFade);
                return uiFade;
            }

            return null;
        }

        public static UIFade GetOrCreate(GameObject instance, float lerpTime = 0f)
        {
            if (instance == null)
            {
                return null;
            }

            int id = instance.GetInstanceID();
            if (
                Cache.TryGetValue(id, out var wr)
                && wr.TryGetTarget(out var cached)
                && cached != null
            )
            {
                if (lerpTime > 0f)
                {
                    cached.lerpTime = lerpTime;
                }
                return cached;
            }

            if (instance.TryGetComponent<UIFade>(out var uiFade))
            {
                if (lerpTime > 0f)
                {
                    uiFade.lerpTime = lerpTime;
                }
                Cache[id] = new WeakReference<UIFade>(uiFade);
                return uiFade;
            }

            uiFade = instance.AddComponent<UIFade>();
            if (lerpTime > 0f)
            {
                uiFade.lerpTime = lerpTime;
            }
            Cache[id] = new WeakReference<UIFade>(uiFade);
            return uiFade;
        }

        public static void Remove(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }
            Cache.Remove(instance.GetInstanceID());
        }

        public static void Clear() => Cache.Clear();
    }
}
