using Godot;
using MistyInn.Core.Entities;

namespace MistyInn.Units;

/// <summary>A staff member or the innkeeper on the board. Placeholder art (ART-16).</summary>
public partial class UnitView : Node3D
{
    [Export] public Label3D Label { get; set; } = null!;

    public UnitId Id { get; private set; }

    public string DisplayName { get; private set; } = "";

    public void Setup(UnitId id, string name)
    {
        Id = id;
        DisplayName = name;
        Label.Text = name;
    }
}
