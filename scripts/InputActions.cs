using Godot;

namespace MistyInn;

/// <summary>
/// Registers the default key bindings at startup (D-015), so the project runs without editing the Input Map.
/// Actions already defined in Project Settings are left alone.
/// </summary>
public static class InputActions
{
    public const string PanLeft = "camera_pan_left";
    public const string PanRight = "camera_pan_right";
    public const string PanUp = "camera_pan_up";
    public const string PanDown = "camera_pan_down";
    public const string RotateLeft = "camera_rotate_left";
    public const string RotateRight = "camera_rotate_right";
    public const string Follow = "camera_follow";
    public const string PauseMenu = "pause_menu";
    public const string RotateFixture = "rotate_fixture";

    public static void EnsureDefaults()
    {
        Add(PanLeft, Key.A, Key.Left);
        Add(PanRight, Key.D, Key.Right);
        Add(PanUp, Key.W, Key.Up);
        Add(PanDown, Key.S, Key.Down);
        Add(RotateLeft, Key.Q);
        Add(RotateRight, Key.E);
        Add(Follow, Key.F);
        Add(PauseMenu, Key.Escape);
        Add(RotateFixture, Key.R);
    }

    private static void Add(string action, params Key[] keys)
    {
        if (InputMap.HasAction(action))
        {
            return;
        }

        InputMap.AddAction(action);
        foreach (var key in keys)
        {
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
    }
}
