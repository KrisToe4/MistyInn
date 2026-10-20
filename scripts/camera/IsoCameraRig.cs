using Godot;

namespace MistyInn.Camera;

/// <summary>
/// The isometric-style camera (CAM-10): an orthographic camera looking down at the inn. Pan with WASD, the arrow keys,
/// the middle mouse button or the screen edges; zoom with the wheel; rotate in 90° steps with Q and E. It stays inside
/// the clearing. It can follow a selected unit or creature (CAM-12).
/// </summary>
public partial class IsoCameraRig : Node3D
{
    [Export] public Camera3D Camera { get; set; } = null!;

    [Export] public Node3D Pitch { get; set; } = null!;

    [Export] public float PitchDegrees { get; set; } = -50f;

    [Export] public float StartYawDegrees { get; set; } = 45f;

    [Export] public float PanSpeed { get; set; } = 14f;

    [Export] public bool EdgePanning { get; set; } = true;

    [Export] public float EdgeMarginPixels { get; set; } = 12f;

    [Export] public float MinZoom { get; set; } = 8f;

    [Export] public float MaxZoom { get; set; } = 34f;

    [Export] public float ZoomStep { get; set; } = 1.12f;

    private float _targetYaw;
    private bool _middleDragging;
    private Vector2 _min = new(-20, -20);
    private Vector2 _max = new(20, 20);

    /// <summary>When set, the camera keeps this node centred until the player pans.</summary>
    public Node3D? FollowTarget { get; set; }

    public override void _Ready()
    {
        Pitch.RotationDegrees = new Vector3(PitchDegrees, 0, 0);
        _targetYaw = Mathf.DegToRad(StartYawDegrees);
        Rotation = new Vector3(0, _targetYaw, 0);
    }

    /// <summary>Keeps the camera's focus point inside this rectangle of the ground (x, z).</summary>
    public void SetBounds(Vector2 min, Vector2 max)
    {
        _min = min;
        _max = max;
        ClampPosition();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                Zoom(1 / ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                Zoom(ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } middle:
                _middleDragging = middle.Pressed;
                break;
            case InputEventMouseMotion motion when _middleDragging:
                var worldPerPixel = Camera.Size / GetViewport().GetVisibleRect().Size.Y;
                PanBy(-motion.Relative * worldPerPixel);
                break;
        }

        if (@event.IsActionPressed(InputActions.RotateLeft))
        {
            _targetYaw += Mathf.Pi / 2;
        }
        else if (@event.IsActionPressed(InputActions.RotateRight))
        {
            _targetYaw -= Mathf.Pi / 2;
        }
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var input = Input.GetVector(InputActions.PanLeft, InputActions.PanRight, InputActions.PanUp, InputActions.PanDown);

        if (EdgePanning && GetWindow().HasFocus())
        {
            var mouse = GetViewport().GetMousePosition();
            var size = GetViewport().GetVisibleRect().Size;
            if (mouse.X >= 0 && mouse.Y >= 0 && mouse.X <= size.X && mouse.Y <= size.Y)
            {
                if (mouse.X < EdgeMarginPixels) input.X -= 1;
                if (mouse.X > size.X - EdgeMarginPixels) input.X += 1;
                if (mouse.Y < EdgeMarginPixels) input.Y -= 1;
                if (mouse.Y > size.Y - EdgeMarginPixels) input.Y += 1;
            }
        }

        if (input != Vector2.Zero)
        {
            FollowTarget = null;
            PanBy(input.LimitLength(1) * PanSpeed * dt * (Camera.Size / 20f));
        }

        if (FollowTarget is not null)
        {
            if (IsInstanceValid(FollowTarget))
            {
                var p = FollowTarget.GlobalPosition;
                Position = new Vector3(p.X, 0, p.Z);
            }
            else
            {
                FollowTarget = null;
            }
        }

        var yaw = Mathf.LerpAngle(Rotation.Y, _targetYaw, 1 - Mathf.Exp(-12 * dt));
        Rotation = new Vector3(0, yaw, 0);
        ClampPosition();
    }

    /// <summary>Moves the focus point by a screen-space amount (x right, y down), turned to match the camera's yaw.</summary>
    private void PanBy(Vector2 screen)
    {
        var right = GlobalBasis.X with { Y = 0 };
        var forward = (-GlobalBasis.Z) with { Y = 0 };
        Position += right.Normalized() * screen.X - forward.Normalized() * screen.Y;
    }

    private void Zoom(float factor) => Camera.Size = Mathf.Clamp(Camera.Size * factor, MinZoom, MaxZoom);

    private void ClampPosition() =>
        Position = new Vector3(Mathf.Clamp(Position.X, _min.X, _max.X), 0, Mathf.Clamp(Position.Z, _min.Y, _max.Y));
}
