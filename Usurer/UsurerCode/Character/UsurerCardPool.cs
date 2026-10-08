using BaseLib.Abstracts;
using Godot;
using Usurer.UsurerCode.Extensions;

namespace Usurer.UsurerCode.Character;

/// <summary>
/// Card pool for The Usurer (crimson-gold color scheme).
/// </summary>
public class UsurerCardPool : CustomCardPoolModel
{
    public override string Title => Usurer.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    public override float H => 0.12f;
    public override float S => 0.88f;
    public override float V => 0.90f;

    public override Color DeckEntryCardColor => Usurer.Color;

    public override bool IsColorless => false;
}
