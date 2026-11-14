using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes.Conditions;
using Turnroot.Skills.Nodes.Events;
using Turnroot.Skills.Nodes.Flow;

namespace Turnroot.Skills.Nodes
{
    public static class BattleTargetIntentMap
    {
        private static readonly Dictionary<BattleTargetAction, BattleTargetIntent> ActionIntents =
            new()
            {
                [BattleTargetAction.Attack] = BattleTargetIntent.Offensive,
                [BattleTargetAction.Steal] = BattleTargetIntent.Offensive,
                [BattleTargetAction.Heal] = BattleTargetIntent.Supportive,
                [BattleTargetAction.Trade] = BattleTargetIntent.Supportive,
                [BattleTargetAction.Talk] = BattleTargetIntent.Supportive,
                [BattleTargetAction.Support] = BattleTargetIntent.Supportive,
                [BattleTargetAction.Buff] = BattleTargetIntent.Supportive,
                [BattleTargetAction.AreaOfEffectAttack] = BattleTargetIntent.Offensive,
                [BattleTargetAction.AreaOfEffectBuff] = BattleTargetIntent.Supportive,
            };

        private static readonly Dictionary<PlayerTurnStates, BattleTargetIntent> TurnStateIntents =
            new()
            {
                [PlayerTurnStates.AttackActionChosenChoosingTarget] = BattleTargetIntent.Offensive,
                [PlayerTurnStates.AttackActionChosenTargetSelected] = BattleTargetIntent.Offensive,
                [PlayerTurnStates.HealActionChosenChoosingTarget] = BattleTargetIntent.Supportive,
                [PlayerTurnStates.HealActionChosenTargetSelected] = BattleTargetIntent.Supportive,
                [PlayerTurnStates.TradeActionChosenChoosingTarget] = BattleTargetIntent.Supportive,
                [PlayerTurnStates.TradeActionChosenTargetSelected] = BattleTargetIntent.Supportive,
                [PlayerTurnStates.TalkActionChosenChoosingTarget] = BattleTargetIntent.Supportive,
                [PlayerTurnStates.TalkActionChosenTargetSelected] = BattleTargetIntent.Supportive,
            };

        private static readonly Dictionary<Type, BattleTargetIntent> NodeIntents = new()
        {
            [typeof(AffectUnitWeaponUsesNode)] = BattleTargetIntent.Offensive,
            [typeof(AreaOfEffectBuffNode)] = BattleTargetIntent.Supportive,
            [typeof(AreaOfEffectDamageNode)] = BattleTargetIntent.Offensive,
            [typeof(BreakWeaponNode)] = BattleTargetIntent.Offensive,
            [typeof(ChangeBattleOrderNode)] = BattleTargetIntent.Offensive,
            [typeof(DealAdditionalDamageNode)] = BattleTargetIntent.Offensive,
            [typeof(DealDebuffAreaOfEffectNode)] = BattleTargetIntent.Offensive,
            [typeof(DisableEnemyFollowupNode)] = BattleTargetIntent.Offensive,
            [typeof(EnemyDistanceNode)] = BattleTargetIntent.Offensive,
            [typeof(EnemyWeaponTypeNode)] = BattleTargetIntent.Offensive,
            [typeof(EnemyWouldKillUnitNode)] = BattleTargetIntent.Offensive,
            [typeof(ForEachAllyInRadiusNode)] = BattleTargetIntent.Supportive,
            [typeof(ForEachAllyNode)] = BattleTargetIntent.Supportive,
            [typeof(ForEachEnemyInRadiusNode)] = BattleTargetIntent.Offensive,
            [typeof(ForEachEnemyNode)] = BattleTargetIntent.Offensive,
            [typeof(KillTargetNode)] = BattleTargetIntent.Offensive,
            [typeof(StealNode)] = BattleTargetIntent.Offensive,
            [typeof(SwapUnitWithTargetNode)] = BattleTargetIntent.Offensive,
            [typeof(UnmountEnemyNode)] = BattleTargetIntent.Offensive,
            [typeof(WeaponAdvantageNode)] = BattleTargetIntent.Offensive,
            [typeof(WeaponRangeNode)] = BattleTargetIntent.Offensive,
        };

        private static readonly Dictionary<Type, BattleTargetAction> NodeActions = new()
        {
            [typeof(StealNode)] = BattleTargetAction.Steal,
        };

        public static BattleTargetIntent GetIntent(BattleTargetAction action) =>
            ActionIntents[action];

        public static bool TryGetIntent(
            PlayerTurnStates state,
            out BattleTargetIntent intent
        ) => TurnStateIntents.TryGetValue(state, out intent);

        public static BattleTargetIntent GetIntent(Type nodeType)
        {
            for (var type = nodeType; type != null; type = type.BaseType)
            {
                if (NodeIntents.TryGetValue(type, out var intent))
                {
                    return intent;
                }
            }

            throw new InvalidOperationException(
                $"No battle target intent is registered for skill node {nodeType?.FullName ?? "<null>"}."
            );
        }

        public static bool TryGetAction(Type nodeType, out BattleTargetAction action)
        {
            for (var type = nodeType; type != null; type = type.BaseType)
            {
                if (NodeActions.TryGetValue(type, out action))
                {
                    return true;
                }
            }

            action = default;
            return false;
        }
    }
}
