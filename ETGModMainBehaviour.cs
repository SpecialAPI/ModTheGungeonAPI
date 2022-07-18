using BepInEx;
using ETGGUI;
using HarmonyLib;
using SGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[HarmonyPatch]
[BepInPlugin(GUID, NAME, VERSION)]
public class ETGModMainBehaviour : BaseUnityPlugin
{
    public const string GUID = "etgmodding.etg.mtgapi";
    public const string NAME = "Mod the Gungeon API";
    public const string VERSION = "1.0.0";
    public static ETGModMainBehaviour Instance;
    public readonly static List<Action<GameManager>> OnGameManagerAwake = new();
    public readonly static List<Action<GameManager>> OnGameManagerStart = new();

    public void Awake()
    {
        Instance = this;
        if (GameManager.HasInstance && GameUIRoot.Instance != null)
        {
            HarmonyPatches.AddLevelLoadListener(GameManager.Instance);
            HarmonyPatches.InvokeOnAwakeBehaviours(GameManager.Instance);
            HarmonyPatches.InvokeOnStartBehaviours(GameManager.Instance);
        }
        new Harmony(GUID).PatchAll();
        ETGMod.StartGlobalCoroutine = StartCoroutine;
        ETGMod.StopGlobalCoroutine = StopCoroutine;
        Gungeon.Game.Initialize();
        Application.logMessageReceived += ETGModDebugLogMenu.Logger;
        SGUIIMBackend.GetFont = (SGUIIMBackend backend) => FontConverter.GetFontFromdfFont((dfFont)dfControl.ActiveInstances[0].GUIManager.DefaultFont, 2);
        SGUIRoot.Setup();
        ETGModGUI.Create();
        ETGModGUI.Start();
        ETGMod.Assets.SetupSpritesFromFolder(ETGMod.SpriteReplacementDirectory);
    }

    public static void WaitForGameManagerStart(Action<GameManager> onStart)
    {
        if(GameManager.HasInstance && GameUIRoot.Instance != null)
        {
            onStart?.Invoke(GameManager.Instance);
        }
        else
        {
            OnGameManagerStart.Add(onStart);
        }
    }

    public static void WaitForGameManagerAwake(Action<GameManager> onAwake)
    {
        if(GameManager.HasInstance && GameUIRoot.Instance != null)
        {
            onAwake?.Invoke(GameManager.Instance);
        }
        else
        {
            OnGameManagerAwake.Add(onAwake);
        }
    }

    public void Update()
    {
        ETGMod.Assets.Packer.Apply();
        if (GameManager.HasInstance && GameManager.Instance.AllPlayers != null)
        {
            foreach (PlayerController play in GameManager.Instance.AllPlayers)
            {
                if (play != null && play.gameObject != null)
                {
                    if (play.InfiniteAmmo != null)
                    {
                        play.InfiniteAmmo.SetOverride("debug_infinite_ammo", InfiniteAmmo, null);
                    }
                    if (play.AdditionalCanDodgeRollWhileFlying != null)
                    {
                        play.AdditionalCanDodgeRollWhileFlying.SetOverride("debug_flight", Flight);
                    }
                    if (Flight && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && !play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasFlighted)
                    {
                        play.SetIsFlying(true, "debug_flight", true, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasFlighted = true;
                    }
                    else if (!Flight && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasFlighted)
                    {
                        play.SetIsFlying(false, "debug_flight", true, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasFlighted = false;
                    }
                    if (NoClip && play.specRigidbody != null && play.specRigidbody.PixelColliders != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null)
                    {
                        foreach (PixelCollider col in play.specRigidbody.PixelColliders)
                        {
                            col.Enabled = false;
                        }
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoClipped = true;
                    }
                    else if (!NoClip && play.specRigidbody != null && play.specRigidbody.PixelColliders != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoClipped)
                    {
                        foreach (PixelCollider col in play.specRigidbody.PixelColliders)
                        {
                            col.Enabled = true;
                        }
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoClipped = false;
                    }
                    if (InfiniteAmmo && play.inventory != null && play.inventory.AllGuns != null)
                    {
                        foreach (Gun g in play.inventory.AllGuns)
                        {
                            g.ammo = Mathf.Max(g.ammo, 1);
                        }
                    }
                    if (Godmode && play.healthHaver != null)
                    {
                        play.healthHaver.IsVulnerable = false;
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasGodmoded = true;
                    }
                    else if (!Godmode && play.healthHaver != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasGodmoded)
                    {
                        play.healthHaver.IsVulnerable = true;
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasGodmoded = false;
                    }
                    if (HighDamage && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && !play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasHighDamaged && play.stats != null)
                    {
                        StatModifier hdMod = StatModifier.Create(PlayerStats.StatType.Damage, StatModifier.ModifyMethod.ADDITIVE, 100000f);
                        play.ownerlessStatModifiers.Add(hdMod);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedHighDamageModifier = hdMod;
                        play.stats.RecalculateStats(play, false, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasHighDamaged = true;
                    }
                    else if (!HighDamage && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasHighDamaged && play.stats != null)
                    {
                        if (play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedHighDamageModifier != null && play.ownerlessStatModifiers.Contains(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedHighDamageModifier))
                        {
                            play.ownerlessStatModifiers.Remove(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedHighDamageModifier);
                        }
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedHighDamageModifier = null;
                        play.stats.RecalculateStats(play, false, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasHighDamaged = false;
                    }
                    if (PerfectAim && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && !play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasPerfectAimed && play.stats != null)
                    {
                        StatModifier hdMod = StatModifier.Create(PlayerStats.StatType.Accuracy, StatModifier.ModifyMethod.MULTIPLICATIVE, 0f);
                        play.ownerlessStatModifiers.Add(hdMod);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedPerfectAimModifier = hdMod;
                        play.stats.RecalculateStats(play, false, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasPerfectAimed = true;
                    }
                    else if (!PerfectAim && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasPerfectAimed && play.stats != null)
                    {
                        if (play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedPerfectAimModifier != null && play.ownerlessStatModifiers.Contains(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedPerfectAimModifier))
                        {
                            play.ownerlessStatModifiers.Remove(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedPerfectAimModifier);
                        }
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedPerfectAimModifier = null;
                        play.stats.RecalculateStats(play, false, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasPerfectAimed = false;
                    }
                    if (NoCurse && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null)
                    {
                        if (!play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoCursed && play.stats != null)
                        {
                            StatModifier ncMod = StatModifier.Create(PlayerStats.StatType.Curse, StatModifier.ModifyMethod.ADDITIVE, -play.stats.GetStatValue(PlayerStats.StatType.Curse));
                            play.ownerlessStatModifiers.Add(ncMod);
                            play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier = ncMod;
                            play.stats.RecalculateStats(play, false, false);
                            play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoCursed = true;
                            play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CurseToRemove = 0f;
                        }
                        else if (play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier != null && play.stats != null && play.stats.GetStatValue(PlayerStats.StatType.Curse) != 0f)
                        {
                            play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier.amount -= play.stats.GetStatValue(PlayerStats.StatType.Curse);
                            play.stats.RecalculateStats(play, false, false);
                        }
                        if (SuperReaperController.Instance != null)
                        {
                            Destroy(SuperReaperController.Instance.gameObject);
                        }
                        if (GameManager.Instance.Dungeon != null && GameManager.Instance.Dungeon.CurseReaperActive)
                        {
                            GameManager.Instance.Dungeon.CurseReaperActive = false;
                        }
                    }
                    else if (!NoCurse && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>() != null && play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoCursed && play.stats != null)
                    {
                        if (play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier != null && play.ownerlessStatModifiers.Contains(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier))
                        {
                            play.ownerlessStatModifiers.Remove(play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier);
                        }
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().CachedNoCurseModifier = null;
                        play.stats.RecalculateStats(play, false, false);
                        play.gameObject.GetOrAddComponent<ETGDebugPlayerFlags>().WasNoCursed = false;
                    }
                    if (InstaCharge && play.activeItems != null)
                    {
                        foreach (PlayerItem it in play.activeItems)
                        {
                            it.CurrentTimeCooldown = Mathf.Min(it.CurrentTimeCooldown, 0f);
                            it.CurrentDamageCooldown = Mathf.Min(it.CurrentDamageCooldown, 0f);
                            it.CurrentRoomCooldown = Mathf.Min(it.CurrentRoomCooldown, 0);
                        }
                    }
                    if (Quickkill && play.CurrentRoom != null && !GameManager.IsBossIntro)
                    {
                        List<AIActor> enemies = new List<AIActor>();
                        play.CurrentRoom.GetActiveEnemies(Dungeonator.RoomHandler.ActiveEnemyType.All, ref enemies);
                        foreach (AIActor a in enemies)
                        {
                            if (a != null && a.healthHaver != null && a.healthHaver.IsAlive)
                            {
                                a.healthHaver.IsVulnerable = true;
                                a.healthHaver.minimumHealth = 0f;
                                a.healthHaver.ApplyDamage(999999f, Vector2.zero, "DEBUG QUICK KILL", CoreDamageTypes.None, DamageCategory.Unstoppable, true, null, true);
                            }
                        }
                    }
                }
            }
        }
    }

    public void Start()
    {
    }

    [HarmonyPatch(typeof(MainMenuFoyerController), nameof(MainMenuFoyerController.Awake))]
    [HarmonyPostfix]
    public static void AddToVersion(MainMenuFoyerController __instance)
    {
        __instance.VersionLabel.Text += $" | BepInEx {typeof(Paths).Assembly.GetName().Version} | Modding API {VERSION}";
    }

    public static bool Godmode;
    public static bool Quickkill;
    public static bool InstaCharge;
    public static bool HighDamage;
    public static bool NoCurse;
    public static bool PerfectAim;
    public static bool InfiniteAmmo;
    public static bool NoClip;
    public static bool Flight;

    private class ETGDebugPlayerFlags : MonoBehaviour
    {
        public bool WasGodmoded;
        public bool WasHighDamaged;
        public bool WasNoCursed;
        public bool WasPerfectAimed;
        public bool WasFlighted;
        public bool WasNoClipped;
        public float CurseToRemove;
        public StatModifier CachedNoCurseModifier;
        public StatModifier CachedHighDamageModifier;
        public StatModifier CachedPerfectAimModifier;
    }
}
