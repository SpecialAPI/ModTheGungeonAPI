using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dungeonator;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
internal static class HarmonyPatches
{
    // aiactor patches
    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
    [HarmonyPrefix]
    private static void AIActor_Start_Prefix(AIActor __instance)
    {
        ETGMod.AIActor.OnPreStart?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.Start))]
    [HarmonyPostfix]
    private static void AIActor_Start_Postfix(AIActor __instance)
    {
        ETGMod.AIActor.OnPostStart?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(AIActor), nameof(AIActor.CheckForBlackPhantomness))]
    [HarmonyPrefix]
    private static void AIActor_CheckForBlackPhantomness(AIActor __instance)
    {
        ETGMod.AIActor.OnBlackPhantomnessCheck?.Invoke(__instance);
    }

    // chest patches
    [HarmonyPatch(typeof(Chest), nameof(Chest.Spawn), typeof(Chest), typeof(Vector3), typeof(RoomHandler), typeof(bool))]
    [HarmonyPostfix]
    private static void Chest_Spawn(Chest __instance)
    {
        ETGMod.Chest.OnPostSpawn?.Invoke(__instance);
    }

    [HarmonyPatch(typeof(Chest), nameof(Chest.Open))]
    [HarmonyPrefix]
    private static bool Chest_Open_Prefix(Chest __instance, ref bool __state, PlayerController player)
    {
        return __state = ETGMod.Chest.OnPreOpen.RunHook(true, new object[]
        {
            __instance,
            player
        });
    }

    [HarmonyPatch(typeof(Chest), nameof(Chest.Open))]
    [HarmonyPostfix]
    private static void Chest_Open_Postfix(Chest __instance, bool __state, PlayerController player)
    {
        ETGMod.Chest.OnPostOpen?.Invoke(__instance, player);
    }

    //gamemanager patches
    [HarmonyPatch(typeof(GameManager), MethodType.Constructor)]
    [HarmonyPostfix]
    internal static void AddLevelLoadListener(GameManager __instance)
    {
        try
        {
            __instance.BraveLevelLoadedListeners = __instance.BraveLevelLoadedListeners.Concat(
                AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetTypes().Where(x => x?.GetInterfaces() != null && !x.IsInterface && x.GetInterfaces().Contains(typeof(ILevelLoadedListener)))).SelectMany(x => x)).Distinct().ToArray();
        }
        catch { }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    [HarmonyPrefix]
    internal static void InvokeOnAwakeBehaviours(GameManager __instance)
    {
        if(GameUIRoot.Instance == null)
        {
            return;
        }
        if(GameManager.mr_manager != null && GameManager.mr_manager != __instance)
        {
            return;
        }
        try
        {
            foreach (var act in ETGModMainBehaviour.OnGameManagerAwake)
            {
                if (__instance == null)
                {
                    break;
                }
                try
                {
                    act?.Invoke(__instance);
                }
                catch (Exception ex)
                {
                    ETGModConsole.Log("An error occured when doing OnGameManagerAwake: " + ex);
                }
            }
            ETGModMainBehaviour.OnGameManagerAwake.Clear();
        }
        catch(Exception ex)
        {
            ETGModConsole.Log("An error occured when doing OnGameManagerAwake: " + ex);
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
    [HarmonyPostfix]
    internal static void InvokeOnStartBehaviours(GameManager __instance)
    {
        if (GameUIRoot.Instance == null)
        {
            return;
        }
        if (GameManager.mr_manager != null && GameManager.Instance != __instance)
        {
            return;
        }
        try
        {
            foreach (var act in ETGModMainBehaviour.OnGameManagerStart)
            {
                if (__instance == null)
                {
                    break;
                }
                try
                {
                    act?.Invoke(__instance);
                }
                catch (Exception ex)
                {
                    ETGModConsole.Log("An error occured when doing OnGameManagerStart: " + ex);
                }
            }
            ETGModMainBehaviour.OnGameManagerStart.Clear();
        }
        catch (Exception ex)
        {
            ETGModConsole.Log("An error occured when doing OnGameManagerStart: " + ex);
        }
    }

    //gameuiroot patches
    [HarmonyPatch(typeof(dfGUIManager), nameof(dfGUIManager.Awake))]
    [HarmonyPrefix]
    internal static void GMUIRInvokeOnAwakeBehaviours(dfGUIManager __instance)
    {
        if (__instance.GetComponent<GameUIRoot>() == null || (GameUIRoot.Instance != null && GameUIRoot.Instance != __instance.GetComponent<GameUIRoot>()) || !GameManager.HasInstance)
        {
            return;
        }
        try
        {
            foreach (var act in ETGModMainBehaviour.OnGameManagerAwake)
            {
                if (!GameManager.HasInstance)
                {
                    break;
                }
                try
                {
                    act?.Invoke(GameManager.Instance);
                }
                catch (Exception ex)
                {
                    ETGModConsole.Log("An error occured when doing OnGameManagerAwake: " + ex);
                }
            }
            ETGModMainBehaviour.OnGameManagerAwake.Clear();
        }
        catch (Exception ex)
        {
            ETGModConsole.Log("An error occured when doing OnGameManagerAwake: " + ex);
        }
    }

    [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.Start))]
    [HarmonyPostfix]
    internal static void GMUIRInvokeOnStartBehaviours(GameUIRoot __instance)
    {
        if (!GameManager.HasInstance || (GameUIRoot.Instance != null && GameUIRoot.Instance != __instance))
        {
            if (__instance.GetComponent<GameUIRoot>() != null)
            {
            }
            return;
        }
        try
        {
            foreach (var act in ETGModMainBehaviour.OnGameManagerStart)
            {
                if (!GameManager.HasInstance)
                {
                    break;
                }
                try
                {
                    act?.Invoke(GameManager.Instance);
                }
                catch (Exception ex)
                {
                    ETGModConsole.Log("An error occured when doing OnGameManagerStart: " + ex);
                }
            }
            ETGModMainBehaviour.OnGameManagerStart.Clear();
        }
        catch (Exception ex)
        {
            ETGModConsole.Log("An error occured when doing OnGameManagerStart: " + ex);
        }
    }

    [HarmonyPatch(typeof(tk2dBaseSprite), nameof(tk2dBaseSprite.Collection), MethodType.Setter)]
    [HarmonyPrefix]
    private static void AddMissingReplacements(tk2dSpriteCollectionData value)
    {
        if (value == null)
            return;

        var coll = value;

        if (ETGMod.Assets.unprocessedSingleTextureReplacements.Count > 0 && ETGMod.Assets.unprocessedSingleTextureReplacements.TryGetValue(coll.spriteCollectionName.Replace("_", " "), out var tex))
        {
            ETGMod.Assets.unprocessedSingleTextureReplacements.Remove(coll.spriteCollectionName.Replace("_", " "));

            ETGMod.Assets.HandleCollectionSheetReplacement(coll, tex);
        }

        if (ETGMod.Assets.unprocessedReplacements.Count > 0 && ETGMod.Assets.unprocessedReplacements.TryGetValue(coll.spriteCollectionName.Replace("_", " "), out var replacements))
        {
            ETGMod.Assets.unprocessedReplacements.Remove(coll.spriteCollectionName.Replace("_", " "));

            ETGMod.Assets.HandleCollectionDefinitionReplacements(coll, replacements);
        }

        if (ETGMod.Assets.unprocessedJsons.Count > 0 && ETGMod.Assets.unprocessedJsons.TryGetValue(coll.spriteCollectionName.Replace("_", " "), out var jsons))
        {
            ETGMod.Assets.unprocessedJsons.Remove(coll.spriteCollectionName.Replace("_", " "));

            ETGMod.Assets.HandleCollectionAttachPoints(coll, jsons);
        }

        if (!ETGMod.Assets.Collections.Contains(coll))
            ETGMod.Assets.Collections.Add(coll);
    }

    [HarmonyPatch(typeof(tk2dBaseSprite), nameof(tk2dBaseSprite.Awake))]
    [HarmonyPrefix]
    private static void AddMissingReplacementsAwake(tk2dBaseSprite __instance)
    {
        AddMissingReplacements(__instance.Collection);
    }

    [HarmonyPatch(typeof(tk2dBaseSprite), nameof(tk2dBaseSprite.SetSprite), typeof(tk2dSpriteCollectionData), typeof(int))]
    [HarmonyPrefix]
    private static void AddMissingReplacementsSSI(tk2dSpriteCollectionData newCollection)
    {
        AddMissingReplacements(newCollection);
    }

    [HarmonyPatch(typeof(tk2dBaseSprite), nameof(tk2dBaseSprite.SetSprite), typeof(tk2dSpriteCollectionData), typeof(string))]
    [HarmonyPrefix]
    private static void AddMissingReplacementsSSS(tk2dSpriteCollectionData newCollection)
    {
        AddMissingReplacements(newCollection);
    }
}
