using MistyInn.Core.Content;

namespace MistyInn.Core.Run;

/// <summary>
/// This cycle's omen as the forecast shows it (OMN-01, CYC-01): its definition, the plane a Surge picked (D-087), and
/// its effects as they apply this cycle.
/// </summary>
public sealed record OmenView(OmenDef Def, string? SurgePlane, IReadOnlyList<EffectDef> Effects);
