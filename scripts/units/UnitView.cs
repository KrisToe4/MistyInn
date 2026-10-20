using Godot;
using MistyInn.Core.Entities;

namespace MistyInn.Units;

/// <summary>
/// A staff member or the innkeeper on the board, with what they are doing and their health. Placeholder art (ART-16):
/// a fallen unit lies on its side until dawn (RAT-05).
/// </summary>
public partial class UnitView : Node3D
{
    [Export] public Label3D Label { get; set; } = null!;

    private string _status = "";
    private double _flashSeconds;
    private Color _flashColor;

    public UnitId Id { get; private set; }

    public string DisplayName { get; private set; } = "";

    public void Setup(UnitId id, string name)
    {
        Id = id;
        DisplayName = name;
        Label.Text = name;
    }

    /// <summary>Shows what it is doing (UI-05, until the UI pass), its health once hurt, and whether it has fallen.</summary>
    public void SetStatus(string? activity, double health, double maxHealth, bool fallen)
    {
        Rotation = fallen ? new Vector3(0, 0, Mathf.Pi / 2) : Vector3.Zero;

        var text = DisplayName;
        if (!string.IsNullOrEmpty(activity))
        {
            text += $"\n{activity}";
        }

        if (health < maxHealth)
        {
            text += $"\n{Mathf.CeilToInt(health)}/{Mathf.CeilToInt(maxHealth)}";
        }

        if (text == _status)
        {
            return;
        }

        _status = text;
        Label.Text = text;
    }

    /// <summary>A short flash when hit: red with blood, white with the Blood filter on (RAT-09).</summary>
    public void Flash(Color color)
    {
        _flashColor = color;
        _flashSeconds = 0.18;
    }

    public override void _Process(double delta)
    {
        if (_flashSeconds <= 0)
        {
            return;
        }

        _flashSeconds -= delta;
        Label.Modulate = _flashSeconds > 0 ? _flashColor : Colors.White;
    }
}
