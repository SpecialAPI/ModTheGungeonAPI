using BepInEx;
using ETGGUI;
using HarmonyLib;
using SGUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// The main behaviour of MTG API.
/// </summary>
[HarmonyPatch]
[BepInPlugin(GUID, NAME, VERSION)]
public class ETGModMainBehaviour : BaseUnityPlugin
{
    /// <summary>
    /// MTG API's guid. Can be set as your mod's dependency if your mod depends on the MTG API.
    /// </summary>
    public const string GUID = "etgmodding.etg.mtgapi";
    public const string NAME = "Mod the Gungeon API";
    /// <summary>
    /// The current version of the MTG API.
    /// </summary>
    public const string VERSION = "1.8.4";
    /// <summary>
    /// Current instance of the MTG API behaviour.
    /// </summary>
    public static ETGModMainBehaviour Instance;
    internal readonly static List<Action<GameManager>> OnGameManagerAwake = new();
    internal readonly static List<Action<GameManager>> OnGameManagerStart = new();
    internal static Harmony harmony;

    public static void EnsureHarmonyInitialized()
    {
        if(harmony != null)
        {
            return;
        }
        (harmony = new Harmony(GUID)).PatchAll();
    }

    public void Awake()
    {
        Instance = this;
        EnsureHarmonyInitialized();

        if (GameManager.HasInstance && GameUIRoot.Instance != null)
        {
            HarmonyPatches.AddLevelLoadListener(GameManager.Instance);
            HarmonyPatches.InvokeOnAwakeBehaviours(GameManager.Instance);
            HarmonyPatches.InvokeOnStartBehaviours(GameManager.Instance);
        }

        ETGModGUI.ConsoleEnabled = Config.Bind("ModTheGungeonUI", "Console", true, "Whether or not the Mod the Gungeon console is enabled or not.").Value;
        ETGModGUI.LogEnabled = Config.Bind("ModTheGungeonUI", "DebugLog", false, "Whether or not the Mod the Gungeon debug log is enabled or not.").Value;
        ETGModGUI.LoaderEnabled = Config.Bind("ModTheGungeonUI", "ModsMenu", true, "Whether or not the Mod the Gungeon mods menu is enabled or not.").Value;

        Gungeon.Game.Initialize();

        Application.logMessageReceived += ETGModDebugLogMenu.Logger;
        SGUIIMBackend.GetFont = (SGUIIMBackend backend) => FontConverter.GetFontFromdfFont((dfFont)dfControl.ActiveInstances[0].GUIManager.DefaultFont, 2);

        SGUIRoot.Setup();
        ETGModGUI.Create();
        ETGModGUI.Start();

        ETGMod.Assets.SetupSpritesFromFolder(ETGMod.SpriteReplacementDirectory);

        var dirs = Directory.GetDirectories(Paths.PluginPath, "sprites", SearchOption.AllDirectories);

        foreach(var dir in dirs)
            ETGMod.Assets.SetupSpritesFromFolder(dir);
    }

    /// <summary>
    /// Delays the given action until both GameManager and GameUIRoot exist and one of them is running Start().
    /// </summary>
    /// <param name="onStart">The action to delay.</param>
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

    /// <summary>
    /// Delays the given action until both GameManager and GameUIRoot exist and one of them is running Awake().
    /// </summary>
    /// <param name="onAwake">The action to delay.</param>
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

    private void Update()
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

    [HarmonyPatch(typeof(MainMenuFoyerController), nameof(MainMenuFoyerController.Awake))]
    [HarmonyPostfix]
    private static void AddToVersion(MainMenuFoyerController __instance)
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
