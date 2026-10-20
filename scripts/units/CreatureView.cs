using Godot;
using MistyInn.Core.Entities;

namespace MistyInn.Units;

/// <summary>A creature on the board, tinted by its plane. Its position comes from Core every frame (SIM-03).</summary>
public partial class CreatureView : Node3D
{
    [Export] public MeshInstance3D Body { get; set; } = null!;

    [Export] public Label3D Label { get; set; } = null!;

    public CreatureId Id { get; private set; }

    public string DisplayName { get; private set; } = "";

    public void Setup(CreatureId id, string name, Color planeColor)
    {
        Id = id;
        DisplayName = name;
        Label.Text = name;
        Body.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = planeColor.Darkened(0.2f),
            EmissionEnabled = true,
            Emission = planeColor,
            EmissionEnergyMultiplier = 0.6f,
        };
    }
}
