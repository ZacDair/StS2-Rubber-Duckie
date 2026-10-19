using Godot;

public partial class Runner : Node
{
    public override async void _Ready()
    {
        try
        {
            GetTree().Quit(await Program.Run());
        }
        catch (System.Exception error)
        {
            GD.PrintErr(error);
            GetTree().Quit(1);
        }
    }
}
