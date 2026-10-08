using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Extensions;

namespace Usurer.UsurerCode.Potions;

[Pool(typeof(UsurerPotionPool))]
public abstract class UsurerPotion : CustomPotionModel
{
    public override string? CustomPackedImagePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
    public override string? CustomPackedOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}
