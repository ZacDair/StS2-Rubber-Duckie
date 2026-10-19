using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace RubberDuckie.RubberDuckieCode;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), new[]
{
    typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
    typeof(ValueProp), typeof(Creature), typeof(CardModel)
})]
public static class ZeroDamageAttackPatch
{
    public static void Prefix(Creature? dealer, out bool __state)
    {
        // The game returns empty damage results for a dead dealer without
        // resolving a hit. Capture this before damage hooks can kill the dealer.
        __state = dealer?.IsDead != true;
    }

    public static void Postfix(bool __state, ref Task<IEnumerable<DamageResult>> __result)
    {
        if (__state)
            __result = PlaySqueaksAfterDamage(__result);
    }

    private static async Task<IEnumerable<DamageResult>> PlaySqueaksAfterDamage(
        Task<IEnumerable<DamageResult>> damageTask)
    {
        var results = await damageTask;

        // An intercepted hit returns separate results for Osty and his owner. Check
        // them together so the owner's zero damage does not hide damage to Osty.
        foreach (var targetResults in results.GroupBy(result =>
                     result.Receiver.PetOwner?.Creature ?? result.Receiver))
        {
            if (targetResults.All(ShouldSqueak))
                DuckAudio.PlaySqueak();
        }

        return results;
    }

    private static bool ShouldSqueak(DamageResult result)
    {
        return result.UnblockedDamage == 0
               && result.OverkillDamage == 0
               && result.BlockedDamage == 0
               && !result.WasFullyBlocked;
    }
}
