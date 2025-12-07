# Understanding the Turnroot Character and Class System

Let me walk you through this framework step by step, starting with the fundamental concepts and building up to how everything connects together.

## The Template-Instance Pattern: The Foundation of Everything

The most important concept to grasp first is the **template versus instance** pattern that runs throughout this entire system. Think of it like the difference between a recipe and an actual cake you've baked. The recipe (template) tells you what the cake should be like, but the actual cake (instance) is what exists in your game at runtime and can change as you play.

**CharacterData** is your recipe—it's a ScriptableObject asset you create in Unity's Project window. It defines everything about what a character should be at the start: their name, starting stats, what class they begin with, their appearance, and so on. You create one of these in your project, configure it in the Inspector, and it stays the same throughout your game.

**CharacterInstance** is the actual character in your game right now. When your game starts or when a battle begins, the system creates CharacterInstance objects based on those CharacterData templates. As your game runs, these instances change—they level up, their HP goes down when hit, they learn new skills, and they gain experience. The template never changes, but the instance is constantly evolving.

The same pattern applies to classes. **CharacterClassData** is the template that describes what a Cavalier or Mage or Swordmaster class should be like—what stats it provides, what weapons it can use, what skills it grants. **CharacterClassDataInstance** is the runtime version that exists on a specific character, handling things like tracking how many battles they've fought with that class or which mastery skills they've unlocked.

## Creating Your First Character

Let me show you the practical workflow. To create a character, you right-click in your Project window and navigate to Create → Turnroot → Character → CharacterData. This creates a new ScriptableObject asset. When you select it and look at the Inspector, you'll see a wealth of options organized into foldout sections.

In the Identity section, you set basic information like their display name and full name. The "Which" field determines if they're an Avatar (player character), Ally, Enemy, or NPC—this affects which roster they belong to during battles. The description field lets you add flavor text.

The Demographics section handles pronouns and physical attributes like height and birthday. The framework has a pronoun system that lets you choose he/him, she/her, or they/them, and conversations can use these pronouns automatically.

The Stats & Progression section is where the numbers live. You'll see lists for BoundedStats (things with min/max like HP) and UnboundedStats (things that can grow indefinitely like Strength or Speed). When you first create a character, these lists might be empty. The system will automatically populate them from your DefaultCharacterStats configuration the first time the asset loads. Each stat has a current value that represents what the character starts with.

The Starting Class field is crucial—this determines what class the character begins with. You need to have created some CharacterClassData assets first, which I'll explain next.

## Creating Classes

Classes work similarly. Right-click and choose Create → Turnroot → Character → Class Data to make a new CharacterClassData asset. When you configure a class, you're defining several categories of information.

The Identity section includes the class name and tier. The tier system (Base, Intermediate, Advanced, etc.) determines progression restrictions—characters can't jump directly from a Base class to an Advanced class, for example.

The Stats section is where classes get interesting. You have four types of stat effects. **Stat Minimums** ensure a character's stats never fall below certain values while in this class. **Stat Caps** set maximum values stats can reach in this class. **Stat Bonuses** are temporary boosts that apply while the class is equipped but disappear when you change classes. **Class Change Bonuses** are permanent one-time stat increases that happen the first time a character takes this class.

The Skills & Abilities section lets you define innate skills (always active in this class) and masteries (skills learned after meeting certain conditions like completing a number of battles or reaching a level threshold).

The Requirements & Certification section controls who can access the class. You can restrict by species, set experience rank requirements, specify which weapon types the class can use, and define the certification item needed to unlock the class.

## How Characters and Classes Connect at Runtime

Here's where the magic happens. When your game runs, the **GamewideContextBrain** is responsible for creating CharacterInstance objects from your CharacterData templates. This usually happens when a roster is instantiated—more on rosters in a moment.

When a CharacterInstance is created, it makes a deep copy of all the template's data. The instance gets its own copies of stats, inventory, skills, and support relationships that can change independently. The template remains unchanged in your project.

If a character has a starting class defined in their CharacterData, the instance will create a **CharacterClassDataInstance** for that class during initialization. This class instance is stored in the `_currentClass` field of the CharacterInstance. The class instance handles several important jobs: it manages the visual representation of the class (the outfit mesh and materials), it applies stat bonuses and enforces caps, and it tracks mastery progress.

When you change a character's class by calling `ChangeClass()` on the CharacterInstance, the system first removes the old class's bonuses, then creates a new CharacterClassDataInstance for the new class, applies its bonuses, enforces its minimums and caps, and if this is the first time the character has used this class, applies the one-time class change bonuses.

## The Brain Architecture: The Nervous System of Your Game

The **Brain** is the central hub that coordinates everything. Think of it like the nervous system—information flows in, gets processed, and events flow back out. The Brain itself doesn't do much work; instead, it has specialized components attached to it that handle different domains.

**StateBrain** manages high-level game states like MainMenu, Combat, WorldMap, and Paused. It knows what state your game is in and publishes events when states change. During battle, it manages substates like PlayerTurn, EnemyTurn, and PostBattle.

**GamewideContextBrain** is the memory and instance manager. It knows about all character instances that exist in your game, handles serialization to save character progress, manages rosters (groups of characters), and provides lookup functions to find character instances by their templates. When you need to find "the runtime instance of this CharacterData template," you ask the GamewideContextBrain.

**CharactersBrain** handles character progression and lifecycle. When characters level up, learn skills, change classes, or gain experience, the CharactersBrain coordinates these operations and publishes events so other systems can react. It also manages battle statistics like tracking kills and turns survived.

**BattleBrain** manages the current battle. It knows about the player team roster, enemy roster, and third-party roster active in the battle. It handles turn progression and battle initialization.

**ConversationalBrain** manages dialogue and conversation systems, though its implementation appears minimal in these files.

**LongTermMemory** is the persistence layer. It stores data to disk using a custom JSON-based system that obfuscates keys for light tamper resistance. The GamewideContextBrain uses it to save and restore character progress for unique characters.

## Rosters: Organizing Your Characters

A **Roster** is a ScriptableObject that holds an array of CharacterData references. Think of it as a list of characters that should exist together—maybe your player's party, or the enemy forces in a battle, or all the recruitable characters in your game.

At runtime, the GamewideContextBrain creates a **RosterInstance** for each Roster. The RosterInstance is a MonoBehaviour (a GameObject component) that contains the actual CharacterInstance objects for all characters in that roster. When a battle starts, the BattleBrain finds RosterInstance components in the scene and uses them to populate the battle.

The workflow looks like this: You create a Roster asset and add CharacterData references to it. You assign this Roster to the GamewideContextBrain's rosters list in the Inspector (on the Brain GameObject). When your game starts, the GamewideContextBrain calls `InstantiateRoster()` for each configured roster. This creates a GameObject with a RosterInstance component, and that RosterInstance creates CharacterInstance objects for each CharacterData in the roster. These instances are then available throughout your game.

## Where to Watch Things Happen: The Event System

The Brain uses C# events extensively to let different parts of your game react to what's happening. Every significant action publishes an event. For example, when a character levels up, the CharactersBrain calls `_brain.PublishCharacterLevelUp(character)`, which triggers the `OnCharacterLevelUp` event on the Brain. Any system that subscribed to that event receives the notification and can respond.

To subscribe to events, you need a reference to the Brain. The cleanest pattern is to inherit from **BrainComponent**, which is a base class that automatically gets the Brain reference and provides virtual methods for subscribing and unsubscribing. Here's the pattern:

```csharp
public class MySystem : BrainComponent
{
    protected override void SubscribeToBrainEvents()
    {
        _brain.OnCharacterLevelUp += HandleLevelUp;
        _brain.OnCharacterLearnedSkill += HandleSkillLearned;
    }
    
    protected override void UnsubscribeFromBrainEvents()
    {
        _brain.OnCharacterLevelUp -= HandleLevelUp;
        _brain.OnCharacterLearnedSkill -= HandleSkillLearned;
    }
    
    private void HandleLevelUp(CharacterInstance character)
    {
        Debug.Log($"{character.CharacterTemplate.DisplayName} leveled up!");
        // Your custom logic here
    }
    
    private void HandleSkillLearned(CharacterInstance character, Skill skill)
    {
        Debug.Log($"{character.CharacterTemplate.DisplayName} learned {skill.SkillName}!");
        // Your custom logic here
    }
}
```

You attach this component to any GameObject (including the Brain GameObject itself), and it will automatically hook into the event system. When events fire, your handler methods get called with the relevant data.

The available events cover character progression (`OnCharacterLevelUp`, `OnCharacterClassChanged`, `OnCharacterLearnedSkill`), battle lifecycle (`OnStartBattle`, `OnExitBattle`, `OnPlayerTurnStarted`), state changes (`OnStateChanged`, `OnPaused`, `OnResumed`), and more. You can see all available events by looking at the Brain.cs file in the public event Action declarations.

## Practical Example: Leveling Up a Character

Let me walk through what happens when a character levels up to show how all these pieces work together. Suppose during a battle, an enemy is defeated and your character gains enough experience to level up.

First, something in your combat system calls `charactersBrain.LevelUpCharacter(characterInstance)`. This is a public method on the CharactersBrain that serves as the entry point for character progression.

Inside that method, the CharactersBrain calls `character.LevelUp()` on the instance. This is defined in CharacterInstance.cs. The LevelUp method increments `_currentLevel`, automatically increases HP by 1, and then applies random stat growth rolls. It looks at the character's effective growth rates (combining personal growth rates from the CharacterData with class growth rate modifiers from the current class), rolls randomly for each stat based on those percentages, and increments stats that succeed their rolls.

After the internal state updates, the CharactersBrain calls `_brain.PublishCharacterLevelUp(character)`. This triggers the `OnCharacterLevelUp` event on the Brain. Any systems that subscribed to this event receive the notification. Maybe your UI system displays a level-up animation, your audio system plays a level-up sound, or your achievement system checks if the character reached a milestone level.

If the character gained experience in their current class, their CharacterClassDataInstance might check mastery conditions. The class instance looks at its mastery array, checking if the character has fought enough battles or reached enough levels to unlock mastery skills. If conditions are met, it calls `character.AddSkill()` to add the skill, and logs a message about learning the new skill.

If the character is unique (marked with `IsUnique` on the CharacterData), the CharactersBrain ensures their progress is saved. At the end of the battle, when `SaveBattleParticipantsProgress()` runs, it calls `_gamewideContextBrain.SaveUniqueCharacterProgress(character)`, which serializes the character instance to JSON and stores it in LongTermMemory so progress persists across play sessions.

## Practical Example: Changing a Character's Class

Let's say you want to promote a character from Cavalier to Paladin. You'd call `charactersBrain.ChangeCharacterClass(characterInstance, paladinClassData)`. The CharactersBrain forwards this to `character.ChangeClass(paladinClassData)` on the instance.

Inside ChangeClass, the system first validates requirements—is the character high enough level? Does the class tier make sense? Do they have the required experience ranks? If validation passes, it removes the old class's bonuses by calling `_currentClass.RemoveClassBonuses(this)`. This subtracts the temporary stat bonuses that the Cavalier class was providing.

Next, it checks if the character has ever equipped the Paladin class before by looking in `_equippedClassHistory`. If this is their first time with Paladin, the `isFirstTime` flag is true.

A new CharacterClassDataInstance is created for Paladin and assigned to `_currentClass`. If you provided a MeshRenderer, it initializes the visual representation, loading the Paladin outfit mesh and textures and applying the character's custom colors.

The new class's bonuses are applied with `_currentClass.ApplyClassBonuses(this)`, adding the Paladin's stat bonuses. If this is the first time, `_currentClass.ApplyClassChangeBonuses(this)` applies permanent one-time stat increases—maybe +2 Strength and +1 Defense that the character keeps forever. The Paladin class is added to `_equippedClassHistory` so the system knows not to give those bonuses again if the character reclasses back to Paladin later.

Finally, stat minimums and caps are enforced. If the Paladin class has a minimum Defense of 10 and the character only has 8, their Defense is raised to 10. If it has a cap on Magic of 15 and the character has 20 Magic, they keep their 20 (the cap only affects future growth, it doesn't reduce existing stats).

After all this internal work, `CharactersBrain.ChangeCharacterClass` calls `_brain.PublishCharacterClassChanged(character)`, and the `OnCharacterClassChanged` event fires, notifying any interested systems.

## Where to Configure Global Settings

Several ScriptableObject assets control system-wide behavior. **DefaultCharacterStats** defines what stats exist in your game and their default values. When a new CharacterData is created, it automatically gets initialized with stats from this configuration. **GameplayGeneralSettings** controls things like whether the game uses weapon experience/aptitudes, whether Luck is enabled as a stat, how class selection works (promotion-based versus requirement-based), and what species types exist. **CharacterPrototypeSettings** appears to be for editor-time character configuration.

These assets should exist in a Resources/GameSettings folder so the system can find them using `GameSettingsLoader.LoadFirst<>()`. The CharacterSettings static class provides cached access to these settings so you don't repeatedly load them from Resources.

## Stats System Deep Dive

The stats system has two fundamental types. **BoundedCharacterStat** represents stats with a minimum and maximum, like HP, Stamina, or Level. These have a current value, max value, min value, and a bonus that applies temporarily. They're displayed as progress bars in the inspector. **CharacterStat** represents unbounded stats like Strength, Speed, or Magic that can grow indefinitely (though classes may impose soft caps).

Both types have a current value and a bonus. The distinction is important: the current value is the character's "real" stat that persists. The bonus is temporary, applied by equipped items or active classes, and goes away when you unequip things. When you call `stat.Get()`, you get current plus bonus. When you call `stat.GetCurrent()` on a BoundedCharacterStat, you get just the current value clamped to the min-max range.

Classes interact with stats through several mechanisms. **StatApplicationHelper** is a utility class that provides high-level methods for applying modifiers. Classes can have stat minimums (if your stat is below this, raise it), stat caps (your stat can't grow beyond this), stat bonuses (temporary boosts while this class is equipped), and class change bonuses (permanent one-time increases when first equipping the class).

The **StatExtensions** class provides extension methods on the IHasStats interface that let you perform batch operations. For example, `character.ApplyBoundedBonuses(modifiers)` applies a list of modifiers to all matching bounded stats on the character. This pattern eliminates repetitive loops and centralizes the stat manipulation logic.

## Serialization and Persistence

The GamewideContextBrain handles saving and loading character progress using a sophisticated serialization system. When it needs to save a unique character, it calls `EncodeInstanceToString()`, which uses Newtonsoft.Json to serialize the CharacterInstance to JSON, computes an FNV-1a hash of the payload for tamper detection, wraps everything in a SerializedWrapper with versioning information, encodes it to Base64, and stores it in LongTermMemory with a computed key.

The tamper detection system has three policies you can configure. **NotifyOnly** will detect tampering but still load the modified data. **Reject** will refuse to load tampered data and return null. **Replace** will detect tampering, create a fresh default instance, and replace the tampered data with the clean version. The system uses both payload hashing (checking the wrapper's internal consistency) and ledger hashing (comparing against a hash stored separately in LongTermMemory) to detect modifications.

Custom JsonConverters handle Unity-specific types. **UnityObjectJsonConverter** serializes UnityEngine.Object references by storing type information and asset paths, then resolving them via GUID in the editor or Resources.Load at runtime. **CharacterInstanceJsonConverter** ensures CharacterInstance objects are properly reconstructed using their constructor so all initialization logic runs. **ObjectItemInstanceJsonConverter** does the same for inventory items.

## Battle Flow and Character Lifecycle

When a battle starts, the flow works like this: The Brain publishes the `OnStartBattle` event. BattleBrain receives this event and looks for a BattleGameObject in the current scene. When found, it calls `InitializeBattleRosters()` on the BattleGameObject, which creates temporary RosterInstance components for the player team, enemy team, and optionally third-party team. It then calls `PopulateBattleRostersFromGamewideContext()`, which uses the GamewideContextBrain to find or create the CharacterInstance objects for each character in the battle.

The CharactersBrain receives the `OnStartBattle` event and calls `InitializeBattleStatistics()`, which iterates through all battle characters and calls `RecordBattleStart()` on each instance. This increments their total battles counter and resets turn-specific stats.

During battle, turn phases progress through PlayerTurn, EnemyTurn, and ThirdPartyTurn states. The BattleBrain has a **TurnRotisserie** component that manages turn progression. When `ProgressTurnOrder()` is called, it advances to the next phase and publishes the appropriate turn event. The CharactersBrain subscribes to these turn events and calls `IncrementTurnsAlive()` on all characters of the active faction, tracking how long each character survives.

When the battle ends, the Brain publishes `OnExitBattle` with a type (Victory, Defeat, or Bookmark). If the exit type is Victory or Bookmark, the CharactersBrain calls `SaveBattleParticipantsProgress()`, which iterates through all battle characters, increments their class battle counts, checks for mastery unlocks, and saves the progress of any unique characters through the GamewideContextBrain.

## Extending the System

The architecture is designed for extension without modification. If you want to add custom behavior when a character levels up, you create a new component that inherits from BrainComponent, subscribe to `OnCharacterLevelUp`, and implement your logic. You don't modify CharacterInstance or CharactersBrain.

If you want to add a new stat type, you'd add an enum value to UnboundedStatType or BoundedStatType, update the display name and description in the StatTypeExtensions, and update your DefaultCharacterStats configuration. The existing code will automatically handle the new stat type because it iterates over all stat types generically.

If you need custom data on characters, you could extend CharacterData with your own fields (though be careful about version control and merges), or create a separate ScriptableObject that references CharacterData and add your data there. For runtime data, you could add a dictionary to CharacterInstance that lets you attach arbitrary key-value data, or create a parallel runtime component that tracks your custom data separately.

The event system is your primary extension point. Nearly everything significant that happens publishes an event. By subscribing to these events, you can layer your own logic on top without touching the core framework code. This is the "farfalle" architecture mentioned in the comments—data flows in from many sources, gets processed centrally, and flows out to many consumers.