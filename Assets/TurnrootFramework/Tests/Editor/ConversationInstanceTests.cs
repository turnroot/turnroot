using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class ConversationInstanceTests
    {
        [Test]
        public void GetEventsForLayer_ReturnsCorrectBinding()
        {
            var go = new GameObject("convInst");
            var inst = go.AddComponent<Turnroot.Conversations.ConversationInstance>();
            var binding = new Turnroot.Conversations.ConversationInstance.LayerEvents
            {
                LayerIndex = 2,
            };
            inst.PerLayerEvents.Add(binding);

            var found = inst.GetEventsForLayer(2);
            Assert.IsNotNull(found);
            Assert.AreEqual(2, found.LayerIndex);

            var missing = inst.GetEventsForLayer(1);
            Assert.IsNull(missing);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_DropdownOptionsAndSelection_Works()
        {
            // create controller
            var go = new GameObject("controller");
            var controller = go.AddComponent<Turnroot.Conversations.ConversationController>();

            // create two instances
            var a = new GameObject("instA");
            var instA = a.AddComponent<Turnroot.Conversations.ConversationInstance>();
            var convA = ScriptableObject.CreateInstance<Turnroot.Conversations.Conversation>();
            convA.name = "ConvA";
            instA.Conversation = convA;

            var b = new GameObject("instB");
            var instB = b.AddComponent<Turnroot.Conversations.ConversationInstance>();
            var convB = ScriptableObject.CreateInstance<Turnroot.Conversations.Conversation>();
            convB.name = "ConvB";
            instB.Conversation = convB;

            // assign private field _conversationInstances via reflection
            var fi = typeof(Turnroot.Conversations.ConversationController).GetField(
                "_conversationInstances",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var list =
                new System.Collections.Generic.List<Turnroot.Conversations.ConversationInstance>
                {
                    instA,
                    instB,
                };
            fi.SetValue(controller, list);

            // call private dropdown helper
            var mi = typeof(Turnroot.Conversations.ConversationController).GetMethod(
                "GetConversationInstanceNames",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var options = mi.Invoke(controller, null) as string[];
            Assert.IsNotNull(options);
            Assert.AreEqual(2, options.Length);
            StringAssert.Contains("ConvA", options[0]);
            StringAssert.Contains("ConvB", options[1]);

            // set selected index and ensure SelectedConversationInstance returns correct object
            var fiIndex = typeof(Turnroot.Conversations.ConversationController).GetField(
                "_currentConversation",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            fiIndex.SetValue(controller, 1);

            var prop = typeof(Turnroot.Conversations.ConversationController).GetProperty(
                "SelectedConversationInstance",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var selected = prop.GetValue(controller) as Turnroot.Conversations.ConversationInstance;
            Assert.AreEqual(instB, selected);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
