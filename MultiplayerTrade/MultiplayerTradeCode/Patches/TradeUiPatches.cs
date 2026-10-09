using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MultiplayerTrade.MultiplayerTradeCode.Net;
using MultiplayerTrade.MultiplayerTradeCode.Rules;
using MultiplayerTrade.MultiplayerTradeCode.UI;

namespace MultiplayerTrade.MultiplayerTradeCode.Patches;

/// <summary>
/// Harmony patches that wire <see cref="TradeSessionSynchronizer"/> into <see cref="RunManager"/> lifecycle
/// and inject hand-painted stone-and-brass "Trade Cards" plaque buttons into <see cref="NMerchantRoom"/>,
/// <see cref="NRestSiteRoom"/>, and <see cref="NMultiplayerPlayerExpandedState"/> during multiplayer runs.
/// </summary>
[HarmonyPatch]
public static class TradeUiPatches
{
    private static readonly FieldInfo? ExpandedStatePlayerField =
        AccessTools.Field(typeof(NMultiplayerPlayerExpandedState), "_player");

    /// <summary>
    /// Initializes <see cref="TradeSessionSynchronizer"/> when a run sets up its lobby and synchronizers.
    /// </summary>
    /// <param name="__instance">The active <see cref="RunManager"/> singleton.</param>
    /// <param name="netService">The run's network game service.</param>
    /// <param name="state">The run state.</param>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.InitializeRunLobby))]
    [HarmonyPostfix]
    public static void RunManager_InitializeRunLobby_Postfix(
        RunManager __instance,
        INetGameService netService,
        RunState state)
    {
        if (state.Players.Count <= 1)
        {
            return;
        }

        TradeSessionSynchronizer.InitializeForRun(
            __instance.RunLocationTargetedBuffer,
            netService,
            state,
            netService.NetId);

        if (TradeSessionSynchronizer.Instance != null)
        {
            TradeSessionSynchronizer.Instance.IncomingInviteReceived += NTradeBarterModal.OpenForIncomingInvite;
        }
    }

    /// <summary>
    /// Cancels any open trade negotiation when exiting the current Shop or Rest Site room.
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "ExitCurrentRooms")]
    [HarmonyPrefix]
    public static void RunManager_ExitCurrentRooms_Prefix()
    {
        TradeSessionSynchronizer.Instance?.CancelActiveSessionOnRoomExit();
    }

    /// <summary>
    /// Disposes <see cref="TradeSessionSynchronizer"/> when the run cleans up.
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
    [HarmonyPrefix]
    public static void RunManager_CleanUp_Prefix()
    {
        TradeSessionSynchronizer.Shutdown();
    }

    /// <summary>
    /// Injects a "Trade Cards" stone-and-brass plaque button into the Shop room (<see cref="NMerchantRoom"/>) in multiplayer runs.
    /// </summary>
    /// <param name="__instance">The shop room UI node.</param>
    [HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
    [HarmonyPostfix]
    public static void NMerchantRoom_Ready_Postfix(NMerchantRoom __instance)
    {
        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || runState.Players.Count <= 1)
        {
            return;
        }

        AddRoomTradeButton(__instance, "MerchantTradeCardsButton", "Trade Cards");
    }

    /// <summary>
    /// Injects a "Trade Cards" stone-and-brass plaque button into the Rest Site room (<see cref="NRestSiteRoom"/>) in multiplayer runs.
    /// </summary>
    /// <param name="__instance">The rest site room UI node.</param>
    [HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))]
    [HarmonyPostfix]
    public static void NRestSiteRoom_Ready_Postfix(NRestSiteRoom __instance)
    {
        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || runState.Players.Count <= 1)
        {
            return;
        }

        AddRoomTradeButton(__instance, "RestSiteTradeCardsButton", "Trade Cards");
    }

    /// <summary>
    /// Injects a direct "Trade Cards" stone-and-brass plaque button into <see cref="NMultiplayerPlayerExpandedState"/>
    /// when inspecting a remote teammate inside a Shop or Rest Site room.
    /// </summary>
    /// <param name="__instance">The expanded player state screen.</param>
    [HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), nameof(NMultiplayerPlayerExpandedState._Ready))]
    [HarmonyPostfix]
    public static void NMultiplayerPlayerExpandedState_Ready_Postfix(NMultiplayerPlayerExpandedState __instance)
    {
        IRunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (!TradeEligibility.CanTradeInCurrentRoom(runState))
        {
            return;
        }

        Player? inspectedPlayer = ExpandedStatePlayerField?.GetValue(__instance) as Player;
        if (inspectedPlayer == null || LocalContext.IsMe(inspectedPlayer))
        {
            return;
        }

        if (__instance.GetNodeOrNull<Button>("ExpandedStateTradeCardsButton") != null)
        {
            return;
        }

        Button tradeBtn = TradeUiStyle.CreatePlaqueButton(
            name: "ExpandedStateTradeCardsButton",
            text: "Trade Cards",
            minSize: new Vector2(260f, 64f),
            fontSize: 20,
            iconFileName: "trade_icon.png",
            iconSize: new Vector2(40f, 40f));

        tradeBtn.ZIndex = 20;
        tradeBtn.AnchorLeft = 1f;
        tradeBtn.AnchorRight = 1f;
        tradeBtn.AnchorTop = 0f;
        tradeBtn.AnchorBottom = 0f;
        tradeBtn.OffsetLeft = -310f;
        tradeBtn.OffsetRight = -42f;
        tradeBtn.OffsetTop = 28f;
        tradeBtn.OffsetBottom = 92f;

        ulong partnerNetId = inspectedPlayer.NetId;
        tradeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            NTradeBarterModal.OpenTradeModal(RunManager.Instance.DebugOnlyGetState(), partnerNetId);
        }));

        __instance.AddChildSafely(tradeBtn);
    }

    private static void AddRoomTradeButton(Control roomNode, string buttonName, string labelText)
    {
        if (roomNode.GetNodeOrNull<Button>(buttonName) != null)
        {
            return;
        }

        Button tradeBtn = TradeUiStyle.CreatePlaqueButton(
            name: buttonName,
            text: labelText,
            minSize: new Vector2(264f, 68f),
            fontSize: 21,
            iconFileName: "trade_icon.png",
            iconSize: new Vector2(42f, 42f));

        tradeBtn.ZIndex = 20;
        // Position in the bottom-left corner, clear of the top-left multiplayer player panels and bottom-right Proceed button.
        tradeBtn.AnchorLeft = 0f;
        tradeBtn.AnchorRight = 0f;
        tradeBtn.AnchorTop = 1f;
        tradeBtn.AnchorBottom = 1f;
        tradeBtn.OffsetLeft = 44f;
        tradeBtn.OffsetRight = 308f;
        tradeBtn.OffsetTop = -112f;
        tradeBtn.OffsetBottom = -44f;

        tradeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(delegate
        {
            NTradeBarterModal.OpenTradeModal(RunManager.Instance.DebugOnlyGetState());
        }));

        roomNode.AddChildSafely(tradeBtn);
    }
}
