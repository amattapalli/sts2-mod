using BaseLib.Abstracts;
using Transmuter.TransmuterCode.Extensions;
using Godot;

namespace Transmuter.TransmuterCode.Character;

public class TransmuterPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Transmuter.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}