using BaseLib.Abstracts;
using Godot;
using Usurer.UsurerCode.Extensions;

namespace Usurer.UsurerCode.Character;

/// <summary>
/// Relic pool for The Usurer.
/// </summary>
public class UsurerRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Usurer.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
