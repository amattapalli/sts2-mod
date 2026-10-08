using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.HoverTips;
using Usurer.UsurerCode.Character;
using Usurer.UsurerCode.Extensions;
using Usurer.UsurerCode.Util;

namespace Usurer.UsurerCode.Relics;

/// <summary>
/// Base class for all Usurer relics, wiring icon paths and automatic keyword hover tips.
/// </summary>
[Pool(typeof(UsurerRelicPool))]
public abstract class UsurerRelic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        UsurerHoverTips.ForRelic(Id.Entry);
}
