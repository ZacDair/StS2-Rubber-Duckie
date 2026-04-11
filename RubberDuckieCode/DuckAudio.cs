using Godot;

namespace RubberDuckie.RubberDuckieCode;

public static class DuckAudio
{
    private const string SqueakPath = "res://RubberDuckie/audio/SFX_squeak.wav";
    private static AudioStream? _squeak;

    public static void PlaySqueak()
    {
        _squeak ??= ResourceLoader.Load<AudioStream>(SqueakPath);

        if (_squeak is null)
        {
            GD.PrintErr($"[RubberDuckie] Failed to load squeak: {SqueakPath}");
            return;
        }

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root is null)
        {
            GD.PrintErr("[RubberDuckie] No SceneTree root available");
            return;
        }

        var player = new AudioStreamPlayer
        {
            Stream = _squeak,
            PitchScale = (float)GD.RandRange(0.92, 1.08),
            VolumeDb = 0f
        };

        tree.Root.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
    }
}