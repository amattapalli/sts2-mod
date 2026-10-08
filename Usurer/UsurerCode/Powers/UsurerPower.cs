using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using Usurer.UsurerCode.Extensions;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Powers;

/// <summary>
/// Base class for all Usurer powers, wiring icon paths and automatic keyword hover tips.
/// </summary>
public abstract class UsurerPower : CustomPowerModel
{
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }

    public abstract override PowerStackType StackType { get; }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        UsurerHoverTips.ForPower(Id.Entry, GetType());

    /// <summary>
    /// Exposes the protected <see cref="MegaCrit.Sts2.Core.Models.Powers.PowerModel.Flash"/> method publicly
    /// so <see cref="DebtEngine"/> can trigger the visual power flash effect.
    /// </summary>
    public new void Flash()
    {
        base.Flash();
    }
}
