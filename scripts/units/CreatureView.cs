using Godot;
using MistyInn.Core.Content;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Units;

/// <summary>
/// A creature on the board, tinted by its plane. Its position comes from Core every frame (SIM-03); its label shows
/// agitation and state, and once it has turned, its escalation stage and health (UI-05, until the UI pass).
/// </summary>
public partial class CreatureView : Node3D
{
    [Export] public MeshInstance3D Body { get; set; } = null!;

    [Export] public Label3D Label { get; set; } = null!;

    private StandardMaterial3D _material = null!;
    private Color _planeColor;
    private string _shown = "";
    private double _flashSeconds;
    private Color _flashColor;
    private Color _labelColor = Colors.White;

    public CreatureId Id { get; private set; }

    public string DisplayName { get; private set; } = "";

    public void Setup(CreatureId id, string name, Color planeColor)
    {
        Id = id;
        DisplayName = name;
        _planeColor = planeColor;
        Label.Text = name;
        _material = new StandardMaterial3D
        {
            AlbedoColor = planeColor.Darkened(0.2f),
            EmissionEnabled = true,
            Emission = planeColor,
            EmissionEnergyMultiplier = 0.6f,
        };
        Body.MaterialOverride = _material;
    }

    public void SetStatus(CreatureSnapshot creature)
    {
        var text = $"{DisplayName}\n{creature.State} {(int)creature.Agitation}{(creature.ServedBy is not null ? "  (served)" : "")}";
        if (creature.Stage is { } stage)
        {
            text = $"{DisplayName}\n{Tr(StageName(stage))}  {Mathf.CeilToInt(creature.Health)}/{Mathf.CeilToInt(creature.MaxHealth)}";
        }
        else if (creature.Health < creature.MaxHealth)
        {
            text += $"\n{Mathf.CeilToInt(creature.Health)}/{Mathf.CeilToInt(creature.MaxHealth)}";
        }

        if (text == _shown)
        {
            return;
        }

        _shown = text;
        var state = creature.State;
        _labelColor = state switch
        {
            CreatureState.Calm => new Color(0.6f, 0.9f, 0.6f),
            CreatureState.Restless => new Color(0.95f, 0.85f, 0.4f),
            CreatureState.Menacing => new Color(1f, 0.55f, 0.25f),
            _ => new Color(1f, 0.2f, 0.2f),
        };

        Label.Text = text;
        Label.Modulate = _labelColor;
        _material.EmissionEnergyMultiplier = state switch
        {
            CreatureState.Calm => 0.6f,
            CreatureState.Restless => 1.0f,
            CreatureState.Menacing => 1.6f,
            _ => 2.4f,
        };
        _material.Emission = state == CreatureState.Turned ? new Color(1f, 0.15f, 0.1f) : _planeColor;
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
        Label.Modulate = _flashSeconds > 0 ? _flashColor : _labelColor;
    }

    private static string StageName(EscalationStage stage) => stage switch
    {
        EscalationStage.Wreck => "Wrecking",
        EscalationStage.Prey => "Preying",
        _ => "Hunting the innkeeper",
    };
}
