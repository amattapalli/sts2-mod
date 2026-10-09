using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MultiplayerTrade.MultiplayerTradeCode;

/// <summary>
/// Entry point for the MultiplayerTrade mod.
/// Applies Harmony patches on initialization without touching <c>ReflectionHelper.ModTypes</c>
/// or <c>MessageTypes</c> prematurely during <c>ModManager.Initialize()</c>.
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    /// <summary>
    /// Unique identifier for the MultiplayerTrade mod.
    /// </summary>
    public const string ModId = "MultiplayerTrade";

    /// <summary>
    /// Godot resource root path for the mod.
    /// </summary>
    public const string ResPath = $"res://{ModId}";

    /// <summary>
    /// Shared logger instance for MultiplayerTrade diagnostics.
    /// </summary>
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.GameSync);

    /// <summary>
    /// Initializes Harmony patches for MultiplayerTrade.
    /// </summary>
    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);
        Logger.Info("MultiplayerTrade initialized Harmony patches.");
    }
}
