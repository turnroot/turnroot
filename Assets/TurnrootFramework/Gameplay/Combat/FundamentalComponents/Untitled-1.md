
**Top Findings (What Is Missing For FE-Style Functional Battles)**

1. Critical: No actual battle completion publisher path found
- You define the lifecycle event at BattleLifecycleBrainEvents.cs, but there is no publish method for battle completed in that file.
- I also found no call sites invoking OnBattleCompleted in code search.
- Impact: victory/defeat listeners can be wired forever, but never receive completion; battle flow can stall at mission end.

2. Critical: Condition results are emitted but not bridged to battle exit
- Conditions only emit condition events in BattleConditions.cs.
- Condition event definitions exist in BattleConditionEvents.cs.
- I found no subscribers to OnBattleConditionMet or OnBattleConditionFailed.
- Impact: objectives can become satisfied/failed internally, but there is no central resolver to conclude battle with victory/defeat.

3. High: Damage condition channels are effectively dead
- BattleGameObject subscribes to OnAllyDamaged and OnEnemyDamaged in ConnectionHelpers.cs.
- Those publisher methods exist in BattleUnitActionEvents.cs, but I found no call sites.
- DamageCommand publishes typed UnitDamagedEvent instead at DamageCommand.cs, and there are no UnitDamagedEvent subscribers.
- Impact: conditions like total ally damage, total enemy damage, and related fail/win logic do not reliably run.

4. High: Combat legality checks are not enforced where combat is executed
- Attack entry point is ContextCommands.cs, but it does not validate CanAttack/allegiance/range before damage exchange.
- CanAttack exists in ContextParticipants.cs, but is not used by AttackTarget.
- AI execution always does move then attack/endturn in Execution.cs, without checking move success.
- Impact: if AI goal data is stale/wrong, units can proceed with invalid combat flows.

5. High: Action animation pipeline for attack/heal/item is not integrated end-to-end
- PlayerTurnFlow emits action-start events in PlayerTurnFlow.cs.
- I found no subscribers to OnAttackStarted/OnHealStarted/OnUseItemStarted.
- Input path immediately executes combat then ends turn in BattleInputControllerBrain.ActionMethods.cs.
- Impact: FE-style “lock camera on action until visual resolution” is incomplete outside move/swap.

6. Medium: Event unsubscription bug likely leaks handlers
- Subscribed lambdas in ConnectionHelpers.cs are removed with new lambdas at ConnectionHelpers.cs, which does not unsubscribe the originals.
- Impact: duplicate handlers/state drift across battle sessions.

7. Medium: Command unit lookup does not search third-party roster
- FindUnit in Commands.cs checks active, targets, allies only.
- Impact: commands involving non-active third-party units can fail lookup unexpectedly.

8. Medium: Monster objective conditions are partially stubbed
- Unfinished TODO path in DefeatMonsterBattleCondition.cs.
- Impact: monster objective maps are not production-ready.

9. Low/Medium: Enemy phase-start event ordering can confuse listeners
- Enemy phase event published in TurnRotisserie.cs before first enemy activation and AI coroutine kickoff at TurnRotisserie.cs.
- Impact: any listener expecting active enemy context during OnEnemyTurnStarted can read stale unit state.

**What Is Already Good**
- TurnRotisserie now does run enemy and third-party actions via coroutine, and has move/swap visual deferral before advancing end turn in TurnRotisserie.cs. That was the big previous blocker, and it appears fixed.

**Recommended Priority Order**
1. Build a single BattleOutcomeResolver that subscribes to OnBattleConditionMet/Failed and publishes battle completion (Victory/Defeat) exactly once.
2. Unify damage/death event channels: either publish legacy events from DamageCommand or migrate all condition handlers to typed UnitDamagedEvent/UnitDefeatedEvent consistently.
3. Add hard legality checks to AttackTarget (allegiance + range + alive checks), and have AI goal execution branch on move success.
4. Wire attack/heal/item animation completion events and gate turn progression/camera similarly to move/swap.
5. Fix lambda unsubscription bug and third-party lookup gaps.
6. Finish monster objective condition TODOs.
