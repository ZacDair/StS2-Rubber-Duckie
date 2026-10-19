using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using RubberDuckie.RubberDuckieCode;

internal static class Program
{
    private static int _squeaks;
    private static readonly BlockingPlayerChoiceContext Choice = new();

    public static async Task<int> Run()
    {
        // Use the game's noninteractive test mode and real combat implementation.
        // Only audio playback is replaced; damage and interception run normally.
        TestMode.IsOn = true;
        // Godot normally performs mod discovery before model initialization.
        // This isolated test project loads only the game and this mod.
        AccessTools.Property(typeof(ModManager), nameof(ModManager.State))
            .SetValue(null, ModManagerState.Skipped);
        ModelDb.Init();
        ModelIdSerializationCache.Init();
        ModelDb.InitIds();
        var harmony = new Harmony("RubberDuckie.RegressionTests");
        harmony.Patch(AccessTools.Method(typeof(DuckAudio), nameof(DuckAudio.PlaySqueak)),
            prefix: new HarmonyMethod(typeof(Program), nameof(RecordSqueak)));
        MainFile.Initialize();

        var cases = new (string Name, Func<Task> Run)[]
        {
            ("Osty absorbs the entire hit", () => Hit(8, 10, 0, 0, 2, 0)),
            ("Osty takes exactly lethal damage", () => Hit(8, 8, 0, 0, 0, 0)),
            ("Osty dies and excess damage reaches the player", () => Hit(8, 3, 0, 5, 0, 0)),
            ("Block and Osty together absorb the hit", () => Hit(8, 10, 3, 0, 5, 0)),
            ("Ordinary block absorbs the entire hit with Osty present", () => Hit(8, 10, 10, 0, 10, 0)),
            ("Ordinary block absorbs the entire hit without Osty", () => Hit(8, null, 10, 0, null, 0)),
            ("An unblocked hit deals damage", () => Hit(8, null, 0, 8, null, 0)),
            ("A genuinely zero-damage hit squeaks without Osty", () => Hit(0, null, 0, 0, null, 1)),
            ("A genuinely zero-damage hit squeaks once with Osty", () => Hit(0, 10, 0, 0, 10, 1)),
            ("Zero damage with block and Osty stays silent", () => Hit(0, 10, 10, 0, 10, 0)),
            ("Strength loss reduces an attack to zero", StrengthLoss),
            ("Area attacks evaluate each player separately", AreaAttack),
            ("Each hit in a multi-hit attack is evaluated separately", MultiHit),
            ("A dead dealer does not resolve a hit or squeak", DeadDealer),
            ("A dead target does not resolve a hit or squeak", DeadTarget),
            ("An empty target list does not squeak", EmptyTargets),
            ("The wrapper waits for damage and preserves its results", AsyncCompletion),
            ("The wrapper preserves damage failures", DamageFailure),
        };

        var failures = 0;
        foreach (var (name, run) in cases)
        {
            _squeaks = 0;
            try
            {
                await run();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception error)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {name}: {error}");
            }
        }

        Console.WriteLine($"{cases.Length - failures}/{cases.Length} regression tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static bool RecordSqueak()
    {
        _squeaks++;
        return false;
    }

    private static (CombatState Combat, Player[] Players) CreateCombat(int playerCount = 1)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(id => Player.CreateForNewRun<Necrobinder>(UnlockState.all, (ulong)id))
            .ToArray();
        var run = RunState.CreateForTest(players, acts: [], seed: "RUBBERDUCKIE");
        var combat = new CombatState(runState: run);
        foreach (var player in players)
        {
            player.ResetCombatState();
            combat.AddPlayer(player);
        }
        return (combat, players);
    }

    private static Creature AddOsty(CombatState combat, Player owner, int hp)
    {
        var osty = combat.CreateCreature(ModelDb.Monster<Osty>().ToMutable(), CombatSide.Player, null);
        combat.AddCreature(osty);
        owner.PlayerCombatState!.AddPetInternal(osty);
        osty.SetMaxHpInternal(hp);
        osty.SetCurrentHpInternal(hp);
        ModelDb.Power<DieForYouPower>().ToMutable().ApplyInternal(osty, 1, true);
        return osty;
    }

    private static async Task Hit(int amount, int? ostyHp, int block,
        int playerHpLost, int? ostyHpLeft, int expectedSqueaks)
    {
        var (combat, players) = CreateCombat();
        var player = players[0];
        var initialHp = player.Creature.CurrentHp;
        var osty = ostyHp is { } hp ? AddOsty(combat, player, hp) : null;
        player.Creature.GainBlockInternal(block);

        var results = (await CreatureCmd.Damage(Choice, player.Creature, amount, ValueProp.Move,
            dealer: null, cardSource: null)).ToList();

        Equal(initialHp - playerHpLost, player.Creature.CurrentHp, "player HP");
        Equal(ostyHpLeft, osty?.CurrentHp, "Osty HP");
        Equal(expectedSqueaks, _squeaks, "squeak count");

        if (ostyHp is > 0 && amount > 0 && block == 0 && playerHpLost == 0)
        {
            // This is the false positive in v0.1.0, using actual game results.
            Equal(1, results.Count(result => result.UnblockedDamage == 0
                && result.BlockedDamage == 0 && !result.WasFullyBlocked), "legacy false positive");
        }
    }

    private static async Task StrengthLoss()
    {
        var (combat, players) = CreateCombat();
        var player = players[0];
        var dealer = combat.CreateCreature(ModelDb.Monster<Osty>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(dealer);
        ModelDb.Power<StrengthPower>().ToMutable().ApplyInternal(dealer, -10, true);
        await CreatureCmd.Damage(Choice, player.Creature, 8, ValueProp.Move, dealer, null);
        Equal(1, _squeaks, "squeak count");
    }

    private static async Task AreaAttack()
    {
        var (combat, players) = CreateCombat(2);
        AddOsty(combat, players[0], 10);
        // A separate player is immune through Buffer. Osty's real damage must
        // suppress only his owner's squeak, while the harmless hit still squeaks.
        ModelDb.Power<BufferPower>().ToMutable().ApplyInternal(players[1].Creature, 1, true);
        await CreatureCmd.Damage(Choice, players.Select(player => player.Creature), 8,
            ValueProp.Move, dealer: null, cardSource: null);
        Equal(1, _squeaks, "squeak count");
    }

    private static async Task MultiHit()
    {
        var (combat, players) = CreateCombat();
        var target = players[0].Creature;
        AddOsty(combat, players[0], 10);
        foreach (var amount in new[] { 3, 0, 3, 0 })
            await CreatureCmd.Damage(Choice, target, amount, ValueProp.Move, dealer: null, cardSource: null);
        Equal(2, _squeaks, "squeak count");
    }

    private static async Task AsyncCompletion()
    {
        var (_, players) = CreateCombat();
        IEnumerable<DamageResult> results = new[] { new DamageResult(players[0].Creature, ValueProp.Move) };
        var completion = new TaskCompletionSource<IEnumerable<DamageResult>>();
        var wrapped = completion.Task;
        ZeroDamageAttackPatch.Postfix(true, ref wrapped);
        Equal(false, wrapped.IsCompleted, "damage completion");
        Equal(0, _squeaks, "squeaks before damage completion");
        completion.SetResult(results);
        Equal(true, ReferenceEquals(results, await wrapped), "result identity");
        Equal(1, _squeaks, "squeak count");
    }

    private static async Task DamageFailure()
    {
        var failure = new InvalidOperationException("test damage failure");
        var wrapped = Task.FromException<IEnumerable<DamageResult>>(failure);
        ZeroDamageAttackPatch.Postfix(true, ref wrapped);
        try
        {
            await wrapped;
            throw new Exception("Expected the damage failure to propagate.");
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, failure))
        {
            Equal(0, _squeaks, "squeak count");
        }
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{label}: expected {expected}, got {actual}.");
    }

    private static async Task DeadDealer()
    {
        var (combat, players) = CreateCombat();
        var dealer = combat.CreateCreature(ModelDb.Monster<Osty>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(dealer);
        dealer.SetCurrentHpInternal(0);
        await CreatureCmd.Damage(Choice, players[0].Creature, 8, ValueProp.Move, dealer, null);
        Equal(0, _squeaks, "squeak count");
    }

    private static async Task DeadTarget()
    {
        var (_, players) = CreateCombat();
        players[0].Creature.SetCurrentHpInternal(0);
        await CreatureCmd.Damage(Choice, players[0].Creature, 8, ValueProp.Move, dealer: null, cardSource: null);
        Equal(0, _squeaks, "squeak count");
    }

    private static async Task EmptyTargets()
    {
        await CreatureCmd.Damage(Choice, Array.Empty<Creature>(), 8, ValueProp.Move, dealer: null, cardSource: null);
        Equal(0, _squeaks, "squeak count");
    }
}
