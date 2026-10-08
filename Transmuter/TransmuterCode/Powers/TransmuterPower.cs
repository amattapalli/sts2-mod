using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Transmuter.TransmuterCode.Extensions;
using Transmuter.TransmuterCode.Util;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Transmuter.TransmuterCode.Powers;

/// <summary>
/// This is the base class for your mod's powers, which is set up to load the power's images from your mod's resources.
/// When creating a power, right click the Powers folder and create a new file with the Custom Power template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
public abstract class TransmuterPower : CustomPowerModel
{
    //Loads from Transmuter/images/powers/your_power.png
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    /// <summary>
    /// Whether this power is a buff or debuff.
    /// </summary>
    public abstract override PowerType Type { get; }
    
    /// <summary>
    /// How this power stacks if reapplied. Counter is the most common type, where applying the power again just
    /// adds to the amount. Single means the power does not stack, like Barricade. None functions identically to
    /// Single, but you're suggested to use Single as it is more explicit about how it will work.
    /// </summary>
    public abstract override PowerStackType StackType { get; }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        TransmuterHoverTips.ForPower(Id.Entry, GetType());

    /// <summary>
    /// Exposes the protected <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Flash"/> method publicly
    /// so external systems (like <see cref="Util.ReactionEngine"/>) can trigger the visual power flash effect.
    /// </summary>
    public new void Flash()
    {
        base.Flash();
    }
}