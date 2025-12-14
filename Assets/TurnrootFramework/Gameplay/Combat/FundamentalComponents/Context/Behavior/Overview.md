# Behavior System Overview

## Core Personality Traits

Every unit has five personality traits that define how they behave in battle:

**Soldier/Lone Wolf** - Whether the unit fights as part of a team or operates independently
- Soldiers stay near allies, coordinate attacks, and retreat together
- Lone Wolves strike out alone, avoid crowded spaces, and prioritize self-preservation

**Mindless/Cunning** - How strategically the unit thinks
- Mindless units attack the nearest enemy without much thought
- Cunning units assess weapon advantages, terrain benefits, and tactical opportunities

**Selfish/Selfless** - Whether the unit prioritizes themselves or their allies
- Selfish units focus on survival and personal gain
- Selfless units protect teammates and heal wounded allies

**Brash/Wary** - How aggressively the unit approaches danger
- Brash units charge into combat and take risks
- Wary units maintain safe distances and retreat when threatened

**Bloodthirst/Greed** - What motivates the unit
- Bloodthirsty units seek combat and eliminating enemies
- Greedy units prioritize treasure, villages, and material rewards

## Standard Unit Archetypes

**Mindless Berserker**
Attacks the closest enemy without regard for personal safety or strategy. Forms small groups but doesn't coordinate well. Will continue attacking even when badly wounded unless surrounded.

**Cunning Assassin**
Works alone to eliminate high-value targets using terrain and weapon advantages. Highly cautious and strategic. Retreats when outnumbered or wounded. Ignores treasure to focus on kills.

**Loyal Guardian**
Stays close to allies and prioritizes their protection over personal safety. Will position themselves between enemies and vulnerable teammates. Rarely pursues treasure or distant objectives.

**Wary Protector**
Similar to the Guardian but more strategic and self-preserving. Protects allies while maintaining safe positions. Evaluates threats carefully before engaging.

**Greedy Coward**
Avoids combat whenever possible, prioritizing treasure and villages. Retreats at the first sign of danger. Only fights when cornered or when enemies are severely weakened.

**Vengeful Warrior**
Generally balanced and strategic, but becomes extremely aggressive when allies are killed. Will charge enemies recklessly after witnessing a teammate's death.

**Reckless Duelist**
Seeks out enemies independently without considering personal safety or ally positions. Focuses on one-on-one combat rather than coordinated attacks.

**Balanced Veteran**
A well-rounded fighter who adapts to situations. Balances offense and defense, cooperates when beneficial, and makes tactical decisions based on circumstances.

## Combat Goals

Units evaluate multiple potential actions each turn and choose the most valuable based on their personality:

**Direct Combat**
- Simple attacks on nearby enemies (mindless units)
- Strategic attacks considering weapon advantages and terrain (cunning units)
- Focus-firing wounded enemies for kills (aggressive units)
- Coordinated attacks with allies (soldiers)

**Support Actions**
- Healing wounded allies (selfless units)
- Positioning to protect vulnerable teammates (guardians)
- Moving to shield allies from enemy attacks (selfless soldiers)

**Strategic Positioning**
- Advancing toward objectives enemies aren't supposed to reach
- Blocking enemy movement routes
- Securing advantageous terrain
- Maintaining formation with allies (soldiers)

**Resource Collection**
- Gathering treasure (greedy units)
- Visiting villages for supplies (cunning + greedy combinations)

**Defensive Actions**
- Retreating to safety when wounded (wary units)
- Healing themselves (selfish units)
- Falling back to ally positions (wounded soldiers)

## Situational Behaviors & Emergent Responses

**When Wounded**
- Selfish units become more self-focused and seek healing
- Wary units become more cautious and retreat-focused
- Brash units are minimally affected unless critically wounded
- Lone Wolves prioritize self-healing over everything

**When Surrounded**
- Most units feel desperate and panicked
- Selfless units look for healing or retreat to allies
- Aggressive units may attempt desperate attacks on weak enemies
- Cunning units calculate the safest escape route
- Lone Wolves become extremely self-centered

**After an Ally Dies**
- Selfless units become vengeful and more aggressive
- Soldiers become slightly more reckless
- Selfish units are unaffected
- Very selfless units may charge enemies who killed their teammate

**After Getting a Kill**
- Brash units become cockier and more aggressive
- Bloodthirsty units get more reckless
- These units lower their caution levels temporarily

**After Finding Treasure**
- Greedy units become braver and more willing to take risks
- The success emboldens them to seek more rewards

**Being Attacked**
- Cunning units become more cautious
- Mindless units barely notice or adapt
- Wary units increase their defensive focus

**Formation Effects**
- Soldiers receive utility bonuses when near allies
- Multiple soldiers near each other coordinate better
- Lone Wolves perform worse when crowded

## Advanced Behavior Combinations

**Greedy + Cunning**: Carefully evaluates treasure opportunities, only taking risks for high-value rewards with low danger

**Bloodthirsty + Cunning**: Identifies and eliminates strategic targets efficiently, focusing on killing high-priority enemies

**Selfless + Soldier**: The ultimate team player—stays with the group, protects wounded allies, and coordinates attacks

**Wary + Lone Wolf**: Extremely cautious solo operative who avoids all unnecessary combat and maintains maximum distance from threats

**Brash + Mindless**: Pure aggression without thought—attacks anything nearby regardless of circumstances

**Cunning + Selfless + Soldier**: Highly coordinated team fighter who protects allies while making smart tactical decisions

## Terrain & Environmental Awareness

Cunning and wary units consider:
- Defense bonuses from forests, forts, and high ground
- Movement penalties from rough terrain
- Health regeneration from healing tiles
- Positioning relative to movement types (fliers ignore terrain, cavalry prefers open ground)

Mindless units largely ignore terrain except for impassable obstacles.

# Behavior Scenarios

## Scenario 1: The Ambush

**Setup**: Three enemy units are moving through a forest pass. Your player units suddenly appear from behind trees on both sides.

**Mindless Berserker Response**
Immediately attacks the closest player unit without evaluating the situation. Doesn't recognize the ambush as dangerous. Continues fighting even as health drops, only retreating if surrounded by 3+ enemies while critically wounded.

**Cunning Assassin Response**
Instantly recognizes the trap. Calculates the safest escape route—likely backward through the pass. Checks for weapon disadvantages against the ambushers. If a clear path exists, retreats immediately. If cornered, targets the weakest-looking enemy to create an opening.

**Loyal Guardian Response**
Positions between the other two enemy units and the player attackers. Doesn't flee even if taking heavy damage, prioritizing keeping allies alive. If one ally is wounded, moves to shield them rather than attacking.

**Greedy Coward Response**
Panics and runs immediately. Doesn't even consider fighting. Flees toward the nearest map edge or safe tile, abandoning the other two units completely. May use items to boost movement speed.

**Balanced Veteran Response**
Assesses the situation: checks enemy numbers, positions, and health. If outnumbered 2-to-1 or worse, retreats while attacking targets of opportunity. If the numbers are close, positions defensively and coordinates with nearby allies.

## Scenario 2: The Treasure Chest

**Setup**: A treasure chest sits in an open field. Player units are 4 tiles away. An enemy unit is 3 tiles away.

**Greedy Coward**
Makes a beeline for the treasure, completely ignoring the nearby player units. Only deviates if a player unit moves directly into the path. After grabbing treasure, becomes emboldened and might actually engage in combat if the odds look favorable.

**Mindless Berserker**
Ignores the treasure entirely. Marches directly toward the nearest player unit to attack. Doesn't even register that the treasure exists.

**Cunning Assassin**
Evaluates the risk: "Can I reach the treasure before the player units reach me?" If yes and the route is safe, goes for it. If no, ignores the treasure and focuses on positioning advantageously against the player units. May circle around to attack from a better angle.

**Balanced Veteran**
Weighs the treasure value against combat advantage. If the detour costs tactical positioning, ignores it. If it can be grabbed without compromising combat readiness, takes it opportunistically.

**Wary Protector** 
Ignores the treasure unless allies need it. Instead positions between the treasure and player units, protecting the path. If an ally is moving toward the treasure, stays close to guard them.

## Scenario 3: The Wounded Ally

**Setup**: An enemy healer and a wounded enemy soldier are positioned together. Player units are advancing on their position.

**Selfish Healer (Greedy Coward with healing)**
Heals the soldier only if it's convenient or if failing to do so threatens their own safety. Primarily focuses on escape routes and personal survival. May abandon the wounded soldier to flee.

**Selfless Healer (Wary Protector with healing)**
Immediately heals the wounded soldier even if it means forgoing an attack or safer positioning. Stays close to vulnerable allies. Continues healing even when personally threatened, only retreating when critically wounded.

**Soldier Healer (Loyal Guardian with healing)**
Prioritizes keeping the formation together. Heals the wounded soldier and positions to protect them. Won't abandon teammates unless the entire group retreats together.

**Cunning Healer (Cunning Assassin with healing)**
Evaluates whether the wounded soldier can meaningfully contribute if healed. If yes, heals them. If they're too damaged to matter or if enemies are too close, abandons them and repositions strategically. Cold but efficient.

## Scenario 4: The Last Stand

**Setup**: An enemy unit is alone, down to 30% health, surrounded by three player units.

**Mindless Berserker**
Emotion: Desperate. Attacks the nearest enemy with whatever weapon is equipped. Makes no attempt to escape. Fights until defeated, possibly taking one player unit down with them.

**Cunning Assassin**
Emotion: Desperate. Immediately looks for the weakest player unit or any gap in the encirclement. Uses terrain and movement to try breaking through. If no escape exists, targets the unit that can be killed in one hit—going for a tactical trade rather than a meaningless death.

**Reckless Duelist**
Emotion: Neutral or Cocky. Doesn't recognize the danger. Picks the strongest-looking opponent and attacks them anyway, seeking an "honorable" death in single combat. May actually prefer this scenario.

**Wary Protector**
Emotion: Desperate. Pure panic mode. Attempts to flee even if the odds are impossible. Moves toward the weakest player unit to try breaking through. Uses any defensive items available. Extremely focused on survival.

**Vengeful Warrior**
Response depends on whether allies died recently:
- **If no recent deaths**: Emotion: Cautious. Tries to escape intelligently, similar to the Balanced Veteran.
- **If an ally died this turn**: Emotion: Enraged. Charges the unit that killed their ally, completely ignoring personal safety. Fights to the death seeking revenge.

## Scenario 5: The Fort Defense

**Setup**: Enemy units need to prevent player units from crossing a specific line. A fort sits on that line providing defensive bonuses.

**Soldier Units (Loyal Guardian, Balanced Veteran)**
Form up inside and around the fort. Maintain formation with allies. Stay within supporting distance of each other. Hold the defensive line as a coordinated group. Very effective at this objective.

**Lone Wolf Units (Cunning Assassin, Reckless Duelist)**
Ignore formation. The Cunning Assassin uses the fort opportunistically but may abandon it for a better position. The Reckless Duelist charges past the fort entirely to engage enemies directly. Poor at holding coordinated defensive lines.

**Wary Units**
Love the fort—it's safe and provides defensive bonuses. Camp inside it and rarely leave unless forced. Excellent for holding but won't pursue enemies.

**Brash Units**
Use the fort as a starting point but quickly abandon it to chase player units. Poor defenders because they won't stay in position.

**Cunning + Soldier Combination**
The ideal defenders. Use the fort intelligently, maintain formation, understand the objective. Cover each other and hold the line effectively.

## Scenario 6: The Split Decision

**Setup**: An enemy unit can either attack a wounded player mage (easy kill) or collect treasure that's slightly out of the way.

**Bloodthirsty + Any Trait**
Ignores the treasure completely. Goes for the kill. Combat is always the priority.

**Greedy + Mindless**
Goes for the treasure without even considering the mage. Doesn't have the strategic thinking to weigh the options.

**Greedy + Cunning**
Calculates which is more valuable. If the mage is nearly dead and on the path anyway, takes the kill then grabs treasure. If the treasure requires a significant detour and other enemies can kill the mage, goes for treasure. Makes the optimal choice.

**Balanced Veteran**
Prioritizes the kill because eliminating threats is strategically sound, but notes the treasure location for later if the opportunity arises.

**Greedy Coward**
Treasure, 100%. Avoids combat when possible. The wounded mage might fight back—the treasure won't.

## Scenario 7: The Chain Reaction

**Setup**: Player units kill an enemy soldier. Several other enemy units witness this.

**Nearby Selfless Units**
Emotion: Enraged. Immediately become more aggressive. The Loyal Guardian charges toward the killer despite being wounded. The Wary Protector who was retreating suddenly turns around and attacks. Their Bloodthirst slider temporarily decreases (more aggressive) and if very selfless, their Wariness also drops.

**Nearby Selfish Units**
No reaction. Continue with their existing plans. May even see it as an opportunity—one less unit to share treasure with.

**Nearby Mindless Units**
No reaction. Don't have the intelligence to process that an ally died. Continue attacking whatever is closest.

**Nearby Cunning Units**
Tactical reassessment. Become more cautious, recognizing the player units are dangerous. May retreat or reposition rather than engaging the killer directly. Smart enough to avoid revenge but not emotional enough to seek it.

**The Snowball Effect**
If multiple selfless soldiers are grouped together and one dies, it can trigger a chain reaction: The group becomes enraged, charges recklessly, and potentially loses more members, making the survivors even more aggressive. A disciplined formation can collapse into a suicide charge.

## Scenario 8: The Healing Sanctuary

**Setup**: A village provides healing (restores health per turn). Multiple wounded enemy units are nearby.

**Selfish + Wounded Units**
Make a beeline for the village. Sit inside it healing even while player units approach. Extremely reluctant to leave until fully healed. May ignore combat entirely to keep healing.

**Selfless + Wounded Units**
Only use the village if allies are safe. If an ally is threatened, leave the village to help even while still wounded. Sacrifice their own recovery for team benefit.

**Cunning + Wounded Units**
Use the village efficiently: heal for 1-2 turns until healthy enough to fight, then leave to engage. Don't overstay—recognize when diminishing returns set in.

**Mindless + Wounded Units**
May walk past the village entirely without recognizing it can heal them. If they happen to end a turn there, enjoy the healing but don't seek it out deliberately.

**Soldier Groups**
Rotate through the village: one heals while others defend. Once healthy, swap positions. Coordinated and efficient.

**Lone Wolf Wounded**
Camp in the village and refuse to leave until fully healed, even if it means abandoning objectives. Extreme self-preservation.

## Scenario 9: The Weapon Triangle

**Setup**: An enemy swordsman faces a player lancer (lances beat swords). A player archer (weak to swords) is nearby.

**Mindless Swordsman**
Attacks the lancer anyway. Doesn't understand weapon disadvantage. Gets destroyed while dealing minimal damage.

**Cunning Swordsman**
Recognizes the disadvantage immediately. Ignores the lancer and targets the archer instead. If the archer is too far away, repositions or uses terrain to avoid the lancer entirely. May retreat if no good options exist.

**Brash Swordsman**
Sees the disadvantage but attacks the lancer anyway out of overconfidence. Takes more damage than expected but doesn't retreat unless critically wounded.

**Wary Swordsman**
Sees the lancer and immediately retreats or repositions. Heavily penalizes engaging disadvantageous matchups. Will take a long detour to avoid the lancer.

**Balanced Swordsman**
Acknowledges the disadvantage and looks for alternatives. If the archer is close, switches targets. If not, may still engage the lancer if no better options exist, but plays defensively.
