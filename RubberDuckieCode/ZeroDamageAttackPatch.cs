using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace RubberDuckie.RubberDuckieCode;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class ZeroDamageAttackPatch
{
    public static void Postfix(
        PlayerChoiceContext choiceContext,
        IRunState runState,
        CombatState? combatState,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!ShouldSqueak(result))
            return;
        
        DuckAudio.PlaySqueak();
    }

    private static bool ShouldSqueak(DamageResult result)
    {
        return result.UnblockedDamage == 0
               && result.BlockedDamage == 0
               && !result.WasFullyBlocked;
    }
}