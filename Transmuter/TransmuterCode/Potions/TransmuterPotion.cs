using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Transmuter.TransmuterCode.Character;
using Transmuter.TransmuterCode.Extensions;

namespace Transmuter.TransmuterCode.Potions;

[Pool(typeof(TransmuterPotionPool))]
public abstract class TransmuterPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}