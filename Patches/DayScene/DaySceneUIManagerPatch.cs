using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Dynamic.Utils;

using DayScene.UI;
using DEYU.AdpUISystem.Managers;

using SgrYuki.Utils;
using Common.UI;
using GameData.RunTime.DaySceneUtility.Collection;

using static MetaMystia.Patch.HarmonyPrefixFlow;

namespace MetaMystia.Patch;

[HarmonyPatch(typeof(DayScene.UI.UIManager))]
[AutoLog]
public partial class DaySceneUIManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(
        nameof(UIManager.OpenAfterChatMenu),
        typeof(Il2CppReferenceArray<DaySceneChatSelectionPannel.GetSelectionConfigurationCallback>),
        typeof(string),
        typeof(DaySceneChatSelectionPannel.GeneralOpenContext.EndButtonCallback),
        typeof(Il2CppSystem.Action),
        typeof(int),
        typeof(AdpUIPanelManager.PanelVisualMode))]
    public static void OpenAfterChatMenu_Prefix(
        ref Il2CppReferenceArray<DaySceneChatSelectionPannel.GetSelectionConfigurationCallback> configurationCallbacks,
        string endButtonTitleKey,
        Il2CppSystem.Action onExitCallback,
        int indexToSelct) // ignore: typo
    {
        if (!CollabBehaviourComponentPatch.PendingCollabMenu.TryConsume()) return;
        configurationCallbacks = configurationCallbacks
            .ToIl2CppReferenceArray()
            .AddLast(StoryReplayManager.CreateCollabMenuSelection())
            .ToIl2CppReferenceArray();
    }

    [HarmonyPatch(nameof(DayScene.UI.UIManager.OpenShopPannel))]
    [HarmonyPrefix]
    public static bool OpenSoldOutResourceExMerchantDialog_Prefix(TrackedMerchant merchantData, Il2CppSystem.Action onFinishCallback)
    {
        if (merchantData == null)
            return RunOriginal;

        var merchantKey = merchantData.key;
        if (!ResourceExManager.IsTelephoneMerchant(merchantKey) || ResourceExManager.HasSellableProducts(merchantData.products))
            return RunOriginal;

        if (!ResourceExManager.TryGetMerchantNullDialog(merchantKey, out var dialog))
        {
            Log.Warning($"ResourceEx merchant {merchantKey} is sold out but has no null dialog package.");
            onFinishCallback?.Invoke();
            return SkipOriginal;
        }

        Log.Info($"Open sold-out ResourceEx merchant dialog before shop panel: {merchantKey}, dialog={dialog?.name}");
        UniversalGameManager.OpenDialogMenu(
            dialog,
            onFinishCallback: onFinishCallback,
            overrideReplaceTextCallback: null,
            previousPanelVisualMode: AdpUIPanelManager.PanelVisualMode.HideVisual);
        return SkipOriginal;
    }
}
