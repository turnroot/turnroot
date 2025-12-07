# ConversationInstance (scene wrapper)

ConversationInstance is a small MonoBehaviour designed to be placed on scene GameObjects and used to create scene-local, per-conversation event hooks.

Why this exists
- Conversation assets (ScriptableObjects) are shared and cannot reliably hold references to scene objects. UnityEvents stored on ScriptableObjects will persist asset references and are not safe for scene-specific wiring.

When to use
- Add a `ConversationInstance` to a scene GameObject and assign your `Conversation` ScriptableObject to it.
- Use the `ConversationController`'s `ConversationInstances` list to reference one or more `ConversationInstance` objects and select which to run at runtime.

What it provides
- `OnConversationStart` and `OnConversationFinished` UnityEvents on the instance — these are scene-local and can point to scene objects.
- `PerLayerEvents` list — assign events for specific layer indices (Start / Complete) so you can hook scene-only behaviours to layer lifecycle.

Controller integration
- `ConversationController` now accepts a list of `ConversationInstance` references and stores the currently selected conversation as an index. This selection is surfaced as a dropdown in the inspector showing instance and conversation names ("InstanceName -> ConversationName").

Notes
- Event lookup is based on the zero-based layer index. If layers are modified in the Conversation asset, verify your layer-index-based bindings remain correct.
