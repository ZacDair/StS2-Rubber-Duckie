using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RubberDuckie.RubberDuckieCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "RubberDuckie";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        GD.Randomize();

        var harmony = new Harmony(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        GD.Print("[RubberDuckie] Initialized");
    }
}