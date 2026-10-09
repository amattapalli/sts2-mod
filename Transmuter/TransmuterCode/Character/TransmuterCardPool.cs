using BaseLib.Abstracts;
using Transmuter.TransmuterCode.Extensions;
using Godot;

namespace Transmuter.TransmuterCode.Character;

public class TransmuterCardPool : CustomCardPoolModel
{
    public override string Title => Transmuter.CharacterId; //This is not a display name.
    
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 0.11f; //Hue; amber/gold
    public override float S => 0.85f; //Saturation
    public override float V => 0.95f; //Brightness
    
    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load Transmuter/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => Transmuter.Color;
    public override Color EnergyOutlineColor => new("4a2606");
    
    public override bool IsColorless => false;
}