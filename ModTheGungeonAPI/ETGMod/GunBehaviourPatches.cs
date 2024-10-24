using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

[HarmonyPatch]
internal static class GunBehaviourPatches
{
    #region Harmony Patches - Postfixes
    [HarmonyPatch(typeof(Gun), nameof(Gun.ThrowGun))]
    [HarmonyPostfix]
    private static void OnGunThrown_Postfix(Gun __instance)
    {
        if (__instance == null)
            return;

        var proj = __instance.GetComponentInParent<Projectile>();
        var behavs = __instance.GetComponents<GunBehaviour>();

        if (proj == null || behavs == null || behavs.Length <= 0)
            return;

        var own = proj.Owner;

        if (own == null)
            return;

        foreach (var advanced in behavs)
        {
            advanced.OnGunThrown(__instance, own, proj);

            if (own is PlayerController player)
                advanced.OnGunThrownPlayer(__instance, player, proj);

            else if (own is AIActor enemy)
                advanced.OnGunThrownEnemy(__instance, enemy, proj);
        }
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.BeginFiringBeam))]
    [HarmonyPostfix]
    private static void AddBeamTracker_Postfix(Gun __instance)
    {
        if (__instance == null)
            return;

        var behavs = __instance.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0 || __instance.LastProjectile == null || __instance.LastProjectile.GetComponent<BeamController>() == null)
            return;

        foreach (var advanced in behavs)
        {
            if (!advanced)
                continue;

            __instance.LastProjectile.gameObject.AddComponent<BeamBehaviourTracker>().gunBehaviour = advanced;
        }
    }

    [HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.Start))]
    [HarmonyPostfix]
    private static void PostProcessBeam_Postfix(BasicBeamController __instance)
    {
        if (__instance == null)
            return;

        var trackers = __instance.GetComponents<BeamBehaviourTracker>();

        if (trackers == null || trackers.Length <= 0)
            return;

        foreach (var tracker in trackers)
        {
            if (tracker.gunBehaviour != null)
                tracker.gunBehaviour.PostProcessBeam(__instance);
        }
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.ThrowGun))]
    [HarmonyPostfix]
    private static void OnDroppedOnThrow_Postfix(Gun __instance)
    {
        if (__instance == null)
            return;

        var behavs = __instance.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return;

        foreach (var behav in behavs)
        {
            if (!behav)
                continue;

            behav.OnDropped();
            behav.InternalOnDropped();
        }
    }

    [HarmonyPatch(typeof(GunInventory), nameof(GunInventory.FrameUpdate))]
    [HarmonyPostfix]
    private static void OwnedUpdate_Postfix(GunInventory __instance)
    {
        if (__instance.AllGuns == null || __instance.AllGuns.Count <= 0)
            return;

        var own = __instance.Owner;

        foreach (var g in __instance.AllGuns)
        {
            if (g == null)
                continue;

            var behavs = g.GetComponents<GunBehaviour>();

            if (behavs == null || behavs.Length <= 0)
                continue;

            foreach (var behav in behavs)
            {
                if (!behav)
                    continue;

                behav.OwnedUpdate(own, __instance);

                if (own is PlayerController player)
                    behav.OwnedUpdatePlayer(player, __instance);

                else if (own is AIActor enemy)
                    behav.OwnedUpdateEnemy(enemy, __instance);
            }
        }
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.FinishReload))]
    [HarmonyPostfix]
    private static void FinishReloading_Postfix(Gun __instance, bool silent)
    {
        if (__instance == null || silent)
            return;

        var behavs = __instance.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return;

        foreach (var behav in behavs)
        {
            if (behav == null)
                continue;

            var own = behav.GenericOwner;

            behav.OnReloadEnded(behav.GenericOwner, __instance);

            if (own is PlayerController player)
                behav.OnReloadEndedPlayer(player, __instance);

            else if (own is AIActor enemy)
                behav.OnReloadEndedEnemy(enemy, __instance);
        }
    }
    #endregion

    #region Harmony Patches - Transpilers
    [HarmonyPatch(typeof(BeamController), nameof(BeamController.HandleChanceTick))]
    [HarmonyILManipulator]
    private static void PostProcessBeamChanceTick_Transpiler(ILContext ctx)
    {
        var cursor = new ILCursor(ctx);

        while (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<PlayerController>(nameof(PlayerController.DoPostProcessBeamChanceTick))))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, ppbct_mc);
        }
    }

    private static void PostProcessBeamChanceTick_MethodCall(BeamController beam)
    {
        if (beam == null)
            return;

        var trackers = beam.GetComponents<BeamBehaviourTracker>();

        if (trackers == null || trackers.Length <= 0)
            return;

        foreach (var tracker in trackers)
        {
            if (tracker.gunBehaviour != null)
                tracker.gunBehaviour.PostProcessBeamChanceTick(beam);
        }
    }

    [HarmonyPatch(typeof(BasicBeamController), nameof(BasicBeamController.FrameUpdate))]
    [HarmonyILManipulator]
    private static void PostProcessBeamTick_Transpiler(ILContext ctx)
    {
        var cursor = new ILCursor(ctx);

        for (var i = 0; i < 8; i++)
        {
            if (!cursor.TryGotoNext(MoveType.After, x => x.MatchIsinst<AIActor>()))
                return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc_S, (byte)7);

        cursor.Emit(OpCodes.Call, ppbt_mc);
    }

    private static AIActor PostProcessBeamTick_MethodCall(AIActor curr, BeamController beam, SpeculativeRigidbody hitRigidbody)
    {
        if (beam == null || beam.projectile.baseData.damage == 0)
            return curr;

        var trackers = beam.GetComponents<BeamBehaviourTracker>();

        if (trackers == null || trackers.Length <= 0)
            return curr;

        var deltatime = BraveTime.DeltaTime;

        foreach (var tracker in trackers)
        {
            if (tracker.gunBehaviour != null)
                tracker.gunBehaviour.PostProcessBeamTick(beam, hitRigidbody, deltatime);
        }

        return curr;
    }

    [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateAmmoUIForModule))]
    [HarmonyILManipulator]
    private static void ModifyClipCount_Transpiler(ILContext ctx)
    {
        var cursor = new ILCursor(ctx);

        if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Equality")))
        {
            cursor.Emit(OpCodes.Ldarg, 8);
            cursor.Emit(OpCodes.Ldarg, 7);
            cursor.Emit(OpCodes.Ldloca, 0);
            cursor.Emit(OpCodes.Ldloca, 1);
            cursor.Emit(OpCodes.Call, mcc_mc);
        }
    }

    private static bool ModifyClipCount_MethodCall(bool curr, Gun gun, ProjectileModule mod, ref int currentModuleAmmo, ref int maxModuleAmmo)
    {
        if (gun == null)
            return curr;

        var behavs = gun.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return curr;

        foreach (var advanced in behavs)
        {
            if (!advanced)
                continue;

            advanced.ModifyClipCount(gun, mod, advanced.PlayerOwner, ref currentModuleAmmo, ref maxModuleAmmo);
        }

        return curr;
    }

    [HarmonyPatch(typeof(GunInventory), nameof(GunInventory.ChangeGun))]
    [HarmonyILManipulator]
    private static void OnChangedToAndAwayFrom_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        if (!crs.TryGotoNext(MoveType.After, x => x.MatchLdfld<GunInventory>(nameof(GunInventory.OnGunChanged))))
            return;

        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloc_0);
        crs.Emit(OpCodes.Ldloc_1);
        crs.Emit(OpCodes.Ldloc_2);

        crs.Emit(OpCodes.Call, octaaf_mc);
    }

    private static GunInventory.OnGunChangedEvent OnChangedToAndAwayFrom_MethodCall(GunInventory.OnGunChangedEvent curr, GunInventory inv, Gun previous, Gun previousSecondary, bool isNewGun)
    {
        var owner = inv.Owner;

        var current = inv.CurrentGun;
        var currentSecondary = inv.CurrentSecondaryGun;

        if (previous != current)
        {
            OnChangedToAndAwayFrom_CallSwitchedToAndAwayFrom(owner, inv, previous, current, isNewGun, true);
            OnChangedToAndAwayFrom_CallSwitchedToAndAwayFrom(owner, inv, current, previous, isNewGun, false);
        }

        if (previousSecondary != currentSecondary)
        {
            OnChangedToAndAwayFrom_CallSwitchedToAndAwayFrom(owner, inv, previousSecondary, currentSecondary, isNewGun, true);
            OnChangedToAndAwayFrom_CallSwitchedToAndAwayFrom(owner, inv, currentSecondary, previousSecondary, isNewGun, false);
        }

        return curr;
    }

    private static void OnChangedToAndAwayFrom_CallSwitchedToAndAwayFrom(GameActor owner, GunInventory inventory, Gun gun, Gun other, bool isNewGun, bool switchedAwayFrom)
    {
        if (gun == null)
            return;

        var behavs = gun.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return;

        foreach (var behav in behavs)
        {
            if (!behav)
                continue;

            if (switchedAwayFrom)
                behav.OnSwitchedAwayFrom(owner, inventory, other, isNewGun);

            else
                behav.OnSwitchedTo(owner, inventory, other, isNewGun);

            if (owner is PlayerController player)
            {
                if (switchedAwayFrom)
                    behav.OnSwitchedAwayFromPlayer(player, inventory, other, isNewGun);

                else
                    behav.OnSwitchedToPlayer(player, inventory, other, isNewGun);
            }
        }
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.Reload))]
    [HarmonyILManipulator]
    private static void OnReload_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        for (int i = 0; i < 3; i++)
        {
            if (!crs.TryGotoNext(MoveType.After, x => x.MatchIsinst<PlayerController>()))
                return;
        }

        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Call, or_mc);
    }

    private static PlayerController OnReload_MethodCall(PlayerController curr, Gun gun)
    {
        if (gun == null)
            return curr;

        var behavs = gun.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return curr;

        foreach (var behav in behavs)
        {
            if (behav == null)
                continue;

            var owner = behav.GenericOwner;

            behav.OnReloaded(owner, gun);

            if (owner is PlayerController player)
                behav.OnReloadedPlayer(player, gun);

            else if (owner is AIActor enemy)
                behav.OnReloadedEnemy(enemy, gun);
        }

        return curr;
    }

    [HarmonyPatch(typeof(AmmoPickup), nameof(AmmoPickup.Interact))]
    [HarmonyILManipulator]
    private static void CanCollectAmmo_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        if (!crs.TryGotoNext(x => x.MatchBeq(out _)))
            return;

        var stateLoc = new VariableDefinition(ctx.Import(typeof(bool)));
        var popupStateLoc = new VariableDefinition(ctx.Import(typeof(bool)));
        crs.Body.Variables.Add(stateLoc);
        crs.Body.Variables.Add(popupStateLoc);

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloca_S, stateLoc);
        crs.Emit(OpCodes.Ldloca_S, popupStateLoc);

        crs.Emit(OpCodes.Call, cca_beq);

        if (!crs.TryGotoNext(x => x.MatchBrtrue(out _)))
            return;

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloca_S, stateLoc);
        crs.Emit(OpCodes.Ldloca_S, popupStateLoc);

        crs.Emit(OpCodes.Call, cca_bt);

        if (!crs.TryGotoNext(x => x.MatchBrfalse(out _)))
            return;

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloca_S, stateLoc);
        crs.Emit(OpCodes.Ldloca_S, popupStateLoc);

        crs.Emit(OpCodes.Call, cca_bf);

        if (!crs.TryGotoNext(x => x.MatchBrfalse(out _)))
            return;

        if (!crs.TryGotoNext(x => x.MatchBrfalse(out _)))
            return;

        crs.Emit(OpCodes.Ldloc_S, popupStateLoc);
        crs.Emit(OpCodes.Call, cca_pfp);
    }

    private static int CanCollectAmmo_BEQ(int maxAmmo, PlayerController player, AmmoPickup ammo, ref bool save_shouldPickUp, ref bool save_shouldDoPopup)
    {
        if (player == null)
            return maxAmmo;

        var g = player.CurrentGun;

        if (g == null)
            return maxAmmo;

        var canCollect = g.ammo != maxAmmo;

        if (canCollect)
            return maxAmmo;

        if (save_shouldPickUp)
            return g.ammo + 1;

        save_shouldDoPopup = true;
        canCollect = CanCollectAmmo_RunEvents(player, ammo, canCollect, false, ref save_shouldPickUp, ref save_shouldDoPopup);

        if (!canCollect)
            return maxAmmo;

        return g.ammo + 1;
    }

    private static bool CanCollectAmmo_BrTrue(bool pickupFails, PlayerController player, AmmoPickup ammo, ref bool save_shouldPickUp, ref bool save_shouldDoPopup)
    {
        var canCollect = !pickupFails;

        if (canCollect)
            return pickupFails;

        if (save_shouldPickUp)
            return false;

        save_shouldDoPopup = true;
        canCollect = CanCollectAmmo_RunEvents(player, ammo, canCollect, false, ref save_shouldPickUp, ref save_shouldDoPopup);

        return !canCollect;
    }

    private static bool CanCollectAmmo_BrFalse(bool pickupFails, PlayerController player, AmmoPickup ammo, ref bool save_shouldPickUp, ref bool save_shouldDoPopup)
    {
        var canCollect = !pickupFails;

        if (player != null && player.CurrentGun != null)
            canCollect &= player.CurrentGun.CanGainAmmo;

        if (save_shouldPickUp)
            return false;

        save_shouldDoPopup = true;
        canCollect = CanCollectAmmo_RunEvents(player, ammo, canCollect, false, ref save_shouldPickUp, ref save_shouldDoPopup);

        return !canCollect;
    }

    private static bool CanCollectAmmo_PreventFullPopup(bool current, bool shouldDoPopup)
    {
        return current && shouldDoPopup;
    }

    private static bool CanCollectAmmo_RunEvents(PlayerController player, AmmoPickup ammo, bool canCollect, bool checkCache, ref bool save_shouldPickUp, ref bool save_shouldDoPopup)
    {
        if (player == null)
            return canCollect;

        var g = player.CurrentGun;

        if (g == null)
            return canCollect;

        var behavs = g.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return canCollect;

        if (checkCache)
        {
            foreach (var behav in behavs)
            {
                if (behav == null)
                    continue;

                if (behav.cache_ammoCollectedSuccessfully)
                    save_shouldPickUp = true;
            }

            if (save_shouldPickUp)
                return true;
        }

        foreach (var behav in behavs)
        {
            if (behav == null)
                continue;

            behav.CanCollectAmmoPickup(player, g, ammo, ref canCollect, ref save_shouldDoPopup);
        }

        if (canCollect)
            save_shouldPickUp = true;

        if (!checkCache)
        {
            foreach (var behav in behavs)
            {
                if (behav == null)
                    continue;

                behav.cache_ammoCollectedSuccessfully = canCollect;
            }
        }

        return canCollect;
    }

    [HarmonyPatch(typeof(AmmoPickup), nameof(AmmoPickup.Pickup))]
    [HarmonyILManipulator]
    private static void CanCollectAmmoPickup_OnAmmoCollected_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        if (!crs.TryGotoNext(x => x.MatchBeq(out _)))
            return;

        var stateLoc = new VariableDefinition(ctx.Import(typeof(bool)));
        crs.Body.Variables.Add(stateLoc);

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloca_S, stateLoc);

        crs.Emit(OpCodes.Call, cca_pt_beq);

        if (!crs.TryGotoNext(x => x.MatchBrtrue(out _)))
            return;

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Ldloca_S, stateLoc);

        crs.Emit(OpCodes.Call, cca_pt_bt);

        if (!crs.TryGotoNext(x => x.MatchCallOrCallvirt<UnityEngine.Object>(nameof(UnityEngine.Object.Destroy))))
            return;

        crs.Emit(OpCodes.Ldarg_1);
        crs.Emit(OpCodes.Ldarg_0);

        crs.Emit(OpCodes.Call, oac_mc);
    }

    private static int CanCollectAmmo_PartTwo_BEQ(int maxAmmo, PlayerController player, AmmoPickup ammo, ref bool save_shouldPickUp)
    {
        if (player == null)
            return maxAmmo;

        var g = player.CurrentGun;

        if (g == null)
            return maxAmmo;

        var canCollect = g.ammo != maxAmmo;

        if (canCollect)
            return maxAmmo;

        if (save_shouldPickUp)
            return g.ammo + 1;

        var temp_shouldDoPopup = true;
        canCollect = CanCollectAmmo_RunEvents(player, ammo, canCollect, true, ref save_shouldPickUp, ref temp_shouldDoPopup);

        if (!canCollect)
            return maxAmmo;

        return g.ammo + 1;
    }

    private static bool CanCollectAmmo_PartTwo_BrTrue(bool canCollect, PlayerController player, AmmoPickup ammo, ref bool save_shouldPickUp)
    {
        if (save_shouldPickUp)
            return true;

        var temp_shouldDoPopup = true;
        canCollect = CanCollectAmmo_RunEvents(player, ammo, canCollect, true, ref save_shouldPickUp, ref temp_shouldDoPopup);

        return canCollect;
    }

    private static GameObject OnAmmoCollected_MethodCall(GameObject curr, PlayerController player, AmmoPickup ammo)
    {
        if (player == null)
            return curr;

        var g = player.CurrentGun;

        if (g == null)
            return curr;

        var behavs = g.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return curr;

        foreach (var behav in behavs)
        {
            if (behav == null)
                continue;

            behav.OnAmmoCollected(player, g, ammo);
        }

        return curr;
    }

    [HarmonyPatch(typeof(GunInventory), nameof(GunInventory.FrameUpdate))]
    [HarmonyILManipulator]
    private static void OnInventoryReload_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);
        var listGetItem = AccessTools.Method(typeof(List<Gun>), "get_Item");

        for(var i = 0; i < 6; i++)
        {
            if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(listGetItem)))
                return;
        }

        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Call, oir_mc);
    }

    private static Gun OnInventoryReload_MethodCall(Gun gun, GunInventory inventory)
    {
        if (gun == null || inventory == null)
            return gun;

        var behavs = gun.GetComponents<GunBehaviour>();
        var own = inventory.Owner;

        if (behavs == null || behavs.Length <= 0)
            return gun;

        if(!gun.HaveAmmoToReloadWith())
            return gun;

        var mods = gun.Volley != null ? gun.Volley.projectiles : [gun.singleModule];

        if (!mods.Any(x => x != null && gun.RuntimeModuleData.TryGetValue(x, out var dat) && dat.numberShotsFired > 0))
            return gun;

        foreach (var behav in behavs)
        {
            if (behav == null)
                continue;

            behav.OnInventoryReload(own, inventory, gun);

            if (own is PlayerController player)
                behav.OnInventoryReloadPlayer(player, inventory, gun);

            else if (own is AIActor enemy)
                behav.OnInventoryReloadEnemy(enemy, inventory, gun);
        }

        return gun;
    }

    [HarmonyPatch(typeof(Gun), nameof(Gun.CeaseAttack))]
    [HarmonyILManipulator]
    private static void AutoreloadOnEmptyClip_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<ProjectileModule>(nameof(ProjectileModule.GetModNumberOfShotsInClip))))
            return;

        crs.Emit(OpCodes.Ldarg_0);
        crs.Emit(OpCodes.Call, aoec_mc);
    }

    private static int AutoreloadOnEmptyClip_MethodCall(int curr, Gun g)
    {
        if(g == null || g.ClipShotsRemaining > 0)
            return curr;

        var behavs = g.GetComponents<GunBehaviour>();

        if(behavs == null || behavs.Length <= 0)
            return curr;

        var shouldAutoreload = curr == 1;

        foreach(var behav in behavs)
        {
            if(behav == null)
                continue;

            behav.AutoreloadOnEmptyClip(g.CurrentOwner, g, ref shouldAutoreload);
        }

        foreach(var behav in behavs)
        {
            if (behav == null)
                continue;

            behav.cache_shouldAutoreload = shouldAutoreload;
        }

        if (shouldAutoreload)
            return 1;

        if (!shouldAutoreload && curr == 1)
            return 2;

        return curr;
    }

    [HarmonyPatch(typeof(GameUIRoot), nameof(GameUIRoot.UpdateGunDataInternal))]
    [HarmonyILManipulator]
    private static void AutoreloadOnEmptyClip_ModifyReloadLabel_Transpiler(ILContext ctx)
    {
        var crs = new ILCursor(ctx);

        if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Gun>($"get_{nameof(Gun.ClipCapacity)}")))
            return;

        crs.Emit(OpCodes.Ldloc_0);
        crs.Emit(OpCodes.Call, aoec_mrl_m);
    }

    private static int AutoreloadOnEmptyClip_ModifyReloadLabel_Modify(int curr, Gun g)
    {
        if (g == null || g.ammo <= 0)
            return curr;

        var behavs = g.GetComponents<GunBehaviour>();

        if (behavs == null || behavs.Length <= 0)
            return curr;

        var autoreloads = curr <= 1;

        foreach (var behav in behavs)
        {
            if (behav == null || behav.cache_shouldAutoreload == null)
                continue;

            autoreloads = behav.cache_shouldAutoreload ?? false;
            break;
        }

        if (autoreloads)
            return 1;

        if (!autoreloads && curr <= 1)
            return 2;

        return curr;
    }

    private static readonly MethodInfo ppbct_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(PostProcessBeamChanceTick_MethodCall));
    private static readonly MethodInfo ppbt_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(PostProcessBeamTick_MethodCall));
    private static readonly MethodInfo mcc_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(ModifyClipCount_MethodCall));
    private static readonly MethodInfo octaaf_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(OnChangedToAndAwayFrom_MethodCall));
    private static readonly MethodInfo or_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(OnReload_MethodCall));
    private static readonly MethodInfo cca_beq = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_BEQ));
    private static readonly MethodInfo cca_bt = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_BrTrue));
    private static readonly MethodInfo cca_bf = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_BrFalse));
    private static readonly MethodInfo cca_pfp = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_PreventFullPopup));
    private static readonly MethodInfo cca_pt_bt = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_PartTwo_BrTrue));
    private static readonly MethodInfo cca_pt_beq = AccessTools.Method(typeof(GunBehaviourPatches), nameof(CanCollectAmmo_PartTwo_BEQ));
    private static readonly MethodInfo oac_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(OnAmmoCollected_MethodCall));
    private static readonly MethodInfo oir_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(OnInventoryReload_MethodCall));
    private static readonly MethodInfo aoec_mc = AccessTools.Method(typeof(GunBehaviourPatches), nameof(AutoreloadOnEmptyClip_MethodCall));
    private static readonly MethodInfo aoec_mrl_m = AccessTools.Method(typeof(GunBehaviourPatches), nameof(AutoreloadOnEmptyClip_ModifyReloadLabel_Modify));
    #endregion
}