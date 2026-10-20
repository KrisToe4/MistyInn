using Godot;

namespace MistyInn.Board;

/// <summary>A plane's mist gate at the edge of the clearing, tinted in the plane's colour (PLN-04). Placeholder art.</summary>
public partial class MistGateView : Node3D
{
    [Export] public MeshInstance3D Body { get; set; } = null!;

    [Export] public OmniLight3D Glow { get; set; } = null!;

    public void SetColor(Color color)
    {
        Body.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color with { A = 0.6f },
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 1.5f,
        };
        Glow.LightColor = color;
    }
}
