#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;
using SGUI;
using Gungeon;
using System.Globalization;
using System.Linq;
using Dungeonator;

public static class PlayerControllerExt
{
    public static bool IsPlaying(this PlayerController player)
    {
        return GameManager.Instance.PrimaryPlayer == player || GameManager.Instance.SecondaryPlayer == player;
    }
    public static bool GiveItem(this PlayerController player, string id)
    {
        if (!player.IsPlaying()) throw new Exception("Tried to give item to inactive player controller");

        LootEngine.TryGivePrefabToPlayer(Gungeon.Game.Items[id].gameObject, player, false);
        return true;
    }
}
public class ETGModConsole : ETGModMenu
{
    public const int REVISION = 3;
    List<string> lastCommands = new List<string>();
    private string lastVal = string.Empty;
    private int currentCommandIndex = -1;
    public static readonly Dictionary<string, string> CommandDescriptions = new()
    {
        { "help", "Shows all commands and shows descriptions for commands that have them" },
        { "tp", "Teleports the player to the given coordinates. Coordinates are measured in tiles" },
        { "rtp", "Teleports the player to the given coordinates, relative to the player's current position. Coordinates are measured in tiles" },
        { "clear", "Clears all of the messages in the console" },
        { "character", "Switches the player to the given character. Doing so will make the player lose all of the items and guns" },
        { "spawn", "Spawns the given object in a random spot in the current room. If the second argument is a number, that number will represent the amount of objects to spawn" },
        { "godmode", "Toggles godmode. While in godmode, the player is unable to take any damage, other than damage from falling into pits" },
        { "quick_kill", "Toggles quick kill. While quick kill is active, all of the enemies in the player's room will instantly die" },
        { "instant_charge", "Toggles instant recharge. While instant recharge is active, the player's active item will instantly recharge after being used" },
        { "high_damage", "Toggles high damage. While high damage is active, the player's damage multiplier will be increased by a lot, to the point of instantly killing most enemies and bosses in the game" },
        { "no_curse", "Toggles no curse. While no curse is active, the player's curse will be set to 0, preventing any of curse's effects from working" },
        { "perfect_aim", "Toggles perfect aim. While perfect aim is active, the spread on the player's guns will be completely negated" },
        { "infinite_ammo", "Toggles infinite ammo. While infinite ammo is active, all of the player's guns will have infinite ammo, similarly to being in Magazine Rack's aura" },
        { "flight", "Toggles flight. While flight is active, the player will have flight" },
        { "no_clip", "Toggles no clip. While no clip is active, the player will be unable to collide with anything, be it walls, enemies or projectiles" },
        { "reveal_floor", "Reveals all rooms on the floor, including secret rooms" },
        { "visit_all_rooms", "Reveals all rooms on the floor and makes them act as if the player has visited them, allowing the player to teleport to them even if they haven't been cleared yet" },
        { "add_teleporters", "Adds a teleporter to every room on the floor that doesn't have one, even to boss rooms" },
        { "load_level", "Teleports the player to the given floor" },
        { "godmode value", "Shows if godmode is currently active" },
        { "quick_kill value", "Shows if quick kill is currently active" },
        { "instant_charge value", "Shows if instant recharge is currently active" },
        { "high_damage value", "Shows if high damage is currently active" },
        { "no_curse value", "Shows if no curse is currently active" },
        { "perfect_aim value", "Shows if perfect aim is currently active" },
        { "infinite_ammo value", "Shows if infinite ammo is currently active" },
        { "flight value", "Shows if flight is currently active" },
        { "no_clip value", "Shows if no clip is currently active" },
        { "load_level show_dictionary", "Shows all levels possible to load, as well as the internal names of currently available levels" },
        { "load_level ignore_dictionary", "Loads a level ignoring the level dictionary, forcing the player to enter the level's internal name. Useful for mod developers" },
        { "load_level ignore_dictionary glitched", "Loads a level under the effect of a glitched chest ignoring the level dictionary, forcing the player to enter the level's internal name. Useful for mod developers" },
        { "load_level glitched", "Loads a level under the effect of a glitched chest." },
        { "spawn chest", "Spawns a chest of the given type in a random spot near the player. Additional arguments can represent if the chest should be locked or become a mimic and also can represent the number of chests to spawn" },
        { "spawn item", "Spawns the given item on the ground. If the second argument is a number, that number will represent the amount of items to spawn" },
        { "spawn shrine", "Spawns a shrine near the player" },
        { "spawn npc", "Spawns an npc near the player" },
        { "spawn chest all_guns", "Spawns a chest of the given type in a random spot near the player. The chest will always have a gun in it. Additional arguments can represent if the chest should be locked or become a mimic and also can represent the number of chests to spawn" },
        { "spawn chest all_items", "Spawns a chest of the given type in a random spot near the player. The chest will always have an item in it. Additional arguments can represent if the chest should be locked or become a mimic and also can represent the number of chests to spawn" },
        { "give", "Gives the player an item. If the second argument is a number, that number will represent the amount of items to give" },
        { "tracked_stat", "The group for getting, setting and adding to tracked stats" },
        { "tracked_stat list", "Lists all available tracked stats" },
        { "tracked_stat get", "Gets the current value of a tracked stat. Will show the value for the current character, for the current session and also for the whole save file"},
        { "tracked_stat add", "Adds the given value to the current value of a tracked stat" },
        { "tracked_stat set", "Sets the given tracked stat's value to the given value" },
        { "stat", "The group for getting, setting, multiplying and adding to the player's stats" },
        { "stat get", "Gets the current value of a player's stat" },
        { "stat set", "Makes the given stat's value equal the given value"},
        { "stat set_exact", "Sets the given stat's base value to the given value. This means that the stat's value will equal the given value in addition to all other stat modifiers" },
        { "stat add", "Adds the given value to the current value of the given stat" },
        { "stat multiply", "Multiplies the given stat's current value by the given value" },
        { "stat remove_0_multipliers", "Remove all ownerless stat multipliers (doesn't remove multipliers given by items) that multiply a stat's value by 0" },
        { "skiplevel", "Skips the player to the next level" },
        { "charselect", "Returns the player to breach's character select screen" },
        { "conf", "The group for configuring various things" },
        { "conf close_console_on_command", "Switches whether the console should be closed when a command is entered. Default: false" },
        { "conf close_console_on_command value", "Shows if the console should be closed when a command is entered." }
    };
    public static readonly Dictionary<string, Chest> ModdedChests = new();
    public static readonly Dictionary<string, GameObject> ModdedShrines = new();
    public static readonly Dictionary<string, GameObject> ModdedNPCs = new();
    public static readonly string[] BaseGameChests = new string[]
    {
        "brown",
        "green",
        "blue",
        "red",
        "black",
        "synergy",
        "glitched",
        "rainbow",
        "hidden_rainbow",
        "rainbow_synergy",
        "rat",
        "truth",
        "high_dragunfire",
        "random",
        "mirror"
    };

    public static string[] BaseGameShrines = new string[]
        {
            "shrine_ammo",
            "shrine_beholster",
            "shrine_blank",
            "shrine_blood",
            "shrine_challenge",
            "shrine_cleanse",
            "shrine_companion",
            "shrine_demonface",
            "shrine_dice",
            "shrine_fallenangel",
            "shrine_glass",
            "shrine_health",
            "shrine_junk",
            "shrine_mirror",
            "shrine_yv"
        };

    public static Dictionary<string, string> BaseGameNPCs = new()
    {
        { "bowler", "npc_bowler" },
        { "daisuke", "npc_daisuke" },
        { "sorceress", "npc_sorceress_gang" },
        { "tonic", "npc_tonic" },
        { "winchester", "npc_artful_dodger" },
        { "frifle_and_grey_mauser", "npc_frifle_and_mauser" },
        { "gunsling_king", "npc_gunslingking" },
        { "manservantes", "npc_manservantes" },
        { "lost_adventurer", "npc_lostadventurer" },
        { "old_man", "npc_old_man" },
        { "monster_manuel", "npc_monster_manuel" },
        { "synergrace", "npc_synergrace" },
        { "synerscope_left", "npc_synerscope_left" },
        { "synerscope_right", "npc_synerscope_right" },
        { "brother_albern", "npc_truth_knower" },
        { "witches", "npc_witches" },
        { "muncher", "npc_gunbermuncher" },
        { "evil_muncher", "npc_gunbermuncher_evil" },
        { "vampire", "npc_vampire" }
    };


    public static readonly Dictionary<string, string> DungeonDictionary = new Dictionary<string, string>
        {
            { "keep", "tt_castle" },
            { "leadlordkeep", "tt_castle" },
            { "lead_lord_keep", "tt_castle" },
            { "proper", "tt5" },
            { "gungeon", "tt5" },
            { "gungeonproper", "tt5" },
            { "gungeon_proper", "tt5" },
            { "mines", "tt_mines" },
            { "mine", "tt_mines" },
            { "powdermines", "tt_mines" },
            { "powdermine", "tt_mines" },
            { "powder_mines", "tt_mines" },
            { "powder_mine", "tt_mines" },
            { "hollow", "tt_catacombs" },
            { "forge", "tt_forge" },
            { "hell", "tt_bullethell" },
            { "bullethell", "tt_bullethell" },
            { "bullet_hell", "tt_bullethell" },
            { "oubliette", "tt_sewer" },
            { "sewer", "tt_sewer" },
            { "sewers", "tt_sewer" },
            { "abbey", "tt_cathedral" },
            { "truegunabbbey", "tt_cathedral" },
            { "true_gun_abbey", "tt_cathedral" },
            { "ratlair", "ss_resourcefulrat" },
            { "ratden", "ss_resourcefulrat" },
            { "rat_lair", "ss_resourcefulrat" },
            { "rat_den", "ss_resourcefulrat" },
            { "rng", "tt_nakatomi" },
            { "r&g", "tt_nakatomi" },
            { "rngdept", "tt_nakatomi" },
            { "r&gdept", "tt_nakatomi" },
            { "rng_dept", "tt_nakatomi" },
            { "r&g_dept", "tt_nakatomi" },
            { "marinepast", "fs_soldier" },
            { "marine_past", "fs_soldier" },
            { "convictpast", "fs_convict" },
            { "convict_past", "fs_convict" },
            { "hunterpast", "fs_guide" },
            { "hunter_past", "fs_guide" },
            { "pilotpast", "fs_pilot" },
            { "pilot_past", "fs_pilot" },
            { "robotpast", "fs_robot" },
            { "robot_past", "fs_robot" },
            { "bulletpast", "fs_bullet" },
            { "bullet_past", "fs_bullet" },
            { "gunslingerpast", "tt_bullethell" },
            { "gunslinger_past", "tt_bullethell" },
            { "tutorial", "tt_tutorial" },
            { "halls", "tt_tutorial" },
            { "knowledgehalls", "tt_tutorial" },
            { "knowledge_halls", "tt_tutorial" },
            { "cultistpast", "fs_coop" },
            { "cultist_past", "fs_coop" },
            { "cooppast", "fs_coop" },
            { "coop_past", "fs_coop" },
        };

    public static readonly Dictionary<string, PlayerStats.StatType> PlayerStatDictionary = new()
    {
        { "move_speed", PlayerStats.StatType.MovementSpeed },
        { "firerate_mult", PlayerStats.StatType.RateOfFire },
        { "charge_speed_mult", PlayerStats.StatType.ChargeAmountMultiplier },
        { "reload_speed_mult", PlayerStats.StatType.ReloadSpeed },
        { "spread_mult", PlayerStats.StatType.Accuracy },
        { "health", PlayerStats.StatType.Health },
        { "damage_mult", PlayerStats.StatType.Damage },
        { "boss_damage_mult", PlayerStats.StatType.DamageToBosses },
        { "range_mult", PlayerStats.StatType.RangeMultiplier },
        { "proj_speed_mult", PlayerStats.StatType.ProjectileSpeed },
        { "proj_scale_mult", PlayerStats.StatType.PlayerBulletScale },
        { "knockback_mult", PlayerStats.StatType.KnockbackMultiplier },
        { "ammo_mult", PlayerStats.StatType.AmmoCapacityMultiplier },
        { "clip_size_mult", PlayerStats.StatType.AdditionalClipCapacityMultiplier },
        { "additional_active_slots", PlayerStats.StatType.AdditionalItemCapacity },
        { "additional_pierces", PlayerStats.StatType.AdditionalShotPiercing },
        { "additional_bounces", PlayerStats.StatType.AdditionalShotBounces },
        { "additional_blanks", PlayerStats.StatType.AdditionalBlanksPerFloor },
        { "curse", PlayerStats.StatType.Curse },
        { "coolness", PlayerStats.StatType.Coolness },
        { "shadow_bullet_chance", PlayerStats.StatType.ShadowBulletChance },
        { "yv_chance", PlayerStats.StatType.ExtremeShadowBulletChance },
        { "throw_damage_mult", PlayerStats.StatType.ThrownGunDamage },
        { "enemy_proj_speed_mult", PlayerStats.StatType.EnemyProjectileSpeedMultiplier },
        { "roll_damage_mult", PlayerStats.StatType.DodgeRollDamage },
        { "roll_distance_mult", PlayerStats.StatType.DodgeRollDistanceMultiplier },
        { "roll_speed_mult", PlayerStats.StatType.DodgeRollSpeedMultiplier },
        { "price_mult", PlayerStats.StatType.GlobalPriceMultiplier },
        { "money_drop_mult", PlayerStats.StatType.MoneyMultiplierFromEnemies },
    };

    public static readonly Dictionary<string, string> Characters = new()
    {
        { "pilot", "rogue" },
        { "convict", "convict" },
        { "hunter", "guide" },
        { "marine", "marine" },
        { "bullet", "bullet" },
        { "robot", "robot" },
        { "paradox", "eevee" },
        { "gunslinger", "gunslinger" },
        { "cultist", "coopcultist" },
        { "lamey", "lamey" },
        { "ninja", "ninja" },
        { "cosmonaut", "cosmonaut" }
    };

    public static ETGModConsole Instance { get; protected set; }
    public ETGModConsole()
    {
        Instance = this;
    }

    /// <summary>
    /// All commands supported by the ETGModConsole. Add your own commands here!
    /// </summary>
    public static ConsoleCommandGroup Commands = new ConsoleCommandGroup(delegate (string[] args)
    {
        Log("Command or group " + args[0] + " doesn't exist");
    });

    /// <summary>
    /// All items in the game, name sorted. Used for the give command.
    /// </summary>
    public static Dictionary<string, int> AllItems = new Dictionary<string, int>();

    public static bool StatSetEnabled = false;

    public bool CloseConsoleOnCommand = false;
    public bool CutInputFocusOnCommand = false;

    protected static char[] _SplitArgsCharacters = { ' ' };

    public static AutocompletionSettings GiveAutocompletionSettings = new(input =>
    {
        List<string> ret = new();
        foreach (string key in Game.Items.IDs)
        {
            if (key.AutocompletionMatch(input.ToLower()))
            {
                ret.Add(key.Replace("gungeon:", ""));
            }
        }
        return ret.ToArray();
    });

    public static AutocompletionSettings SpawnAutocompletionSettings = new(input =>
    {
        List<string> ret = new();
        foreach (string key in Game.Enemies.IDs)
        {
            if (key.AutocompletionMatch(input.ToLower()))
            {
                ret.Add(key.Replace("gungeon:", ""));
            }
        }
        return ret.ToArray();
    });

    public static AutocompletionSettings TrackedStatAutocompletionSettings = new(input =>
    {
        List<string> ret = new();
        foreach (string key in Enum.GetNames(typeof(TrackedStats)))
        {
            if (key.AutocompletionMatch(input.ToUpper()))
            {
                ret.Add(key.ToLower());
            }
        }
        return ret.ToArray();
    });

    public static AutocompletionSettings TrueFalseAutocompletionSettings = AutocompletionFromCollection(new string[] { "true", "false" });
    public static AutocompletionSettings ChestAutocompletionSettings = AutocompletionFromCollectionGetter(() => BaseGameChests.Concat(ModdedChests.Keys));
    public static AutocompletionSettings ShrineAutocompletionSettings = AutocompletionFromCollectionGetter(() => BaseGameShrines.Select(x => x.Replace("shrine_", "")).Concat(ModdedShrines.Keys));
    public static AutocompletionSettings NPCAutocompletionSettings = AutocompletionFromCollectionGetter(() => BaseGameNPCs.Keys.Concat(ModdedNPCs.Keys));
    public static AutocompletionSettings LevelAutocompletionSettings = AutocompletionFromCollectionGetter(() => DungeonDictionary.Keys);
    public static AutocompletionSettings PlayerStatAutocompletionSettings = AutocompletionFromCollectionGetter(() => PlayerStatDictionary.Keys);
    public static AutocompletionSettings CharacterAutocompletionSettings = AutocompletionFromCollectionGetter(() => Characters.Keys);

    public override void Start()
    {
        // GUI
        GUI = new SGroup
        {
            Visible = false,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            Children = {
                new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Fill();
                        elem.Size -= new Vector2(0f, elem.Backend.LineHeight - 4f); // Command line input space
                    },
                    Children = {
                        new SLabel("Use <color=#ffffffff>help</color> to find out how to use the console.") { Foreground = Color.green },
                        new SLabel("Use <color=#ffffffff>clear</color> to clear all messages.") { Foreground = Color.green },
                    }
                },
                new STextField {
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Size.x = elem.Parent.InnerSize.x;
                        elem.Position.x = 0f;
                        elem.Position.y = elem.Parent.InnerSize.y - elem.Size.y;
                    },
                    OnTextUpdate = delegate(STextField elem, string prevText) {
                        HideAutocomplete();
                    },
                    OverrideTab = true,
                    OnKey = delegate(STextField field, bool keyDown, KeyCode keyCode) {
                        if (!keyDown) {
                            return;
                        }
                        if (keyCode == KeyCode.Escape || keyCode == KeyCode.F2 || keyCode == KeyCode.Slash || keyCode == KeyCode.BackQuote) {
                            ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None;
                            return;
                        }
                        if (keyCode == KeyCode.Tab) {
                            ShowAutocomplete();
                            return;
                        }
                        if (keyCode == KeyCode.UpArrow || keyCode == KeyCode.DownArrow)
                        {
                            if (lastCommands.Count <= 0)
                            {
                                field.Text = string.Empty;
                                return;
                            }

                            if (keyCode == KeyCode.UpArrow)
                            {
                                currentCommandIndex--;
                                if (currentCommandIndex <= 0)
                                {
                                    currentCommandIndex = 0;
                                }
                            }
                            else if(keyCode == KeyCode.DownArrow)
                            {
                                currentCommandIndex++;
                                if (currentCommandIndex >= lastCommands.Count)
                                {
                                    currentCommandIndex = lastCommands.Count - 1;
                                }
                            }

                            field.Text = lastCommands[currentCommandIndex];
                            field.MoveCursor(field.Text.Length);
                        }
                    },
                    OnSubmit = delegate(STextField elem, string text) {
                        int lastIdx = lastCommands.Count - 1;
                        if (text != string.Empty && (lastCommands.Count <= 0 || text != lastCommands[lastIdx]))
                        {
                            lastCommands.Add(text);
                        }
                        currentCommandIndex = lastCommands.Count;
                        
                        if (text.Length == 0) return;
                        ParseCommand(text);
                        if (CloseConsoleOnCommand) {
                            ETGModGUI.CurrentMenu = ETGModGUI.MenuOpened.None;
                        }
                    }
                }
            }
        };
        // GLOBAL NAMESPACE
        Commands
                .AddUnit("help", delegate (string[] args)
                {
                    List<List<string>> paths = Commands.ConstructPaths();
                    for (int i = 0; i < paths.Count; i++)
                    {
                        var command = string.Join(" ", paths[i].ToArray());
                        if (CommandDescriptions.TryGetValue(command, out var description))
                        {
                            Log($"{command} - {description}");
                            continue;
                        }
                        Log(command);
                    }
                })
                .AddGroup("tp", Teleport)
                .AddGroup("rtp", RelativeTeleport)
                .AddUnit("character", SwitchCharacter, CharacterAutocompletionSettings)
                .AddUnit("clear", (string[] args) =>
                {
                    GUI[0].Children.Clear();
                    ((SGroup)GUI[0]).ContentSize.y = 0;
                    GUI[0].Children.Add(new SLabel("Use <color=#ffffffff>help</color> to find out how to use the console.") { Foreground = Color.green });
                    GUI[0].Children.Add(new SLabel("Use <color=#ffffffff>clear</color> to clear all messages.") { Foreground = Color.green });
                    ((SGroup)GUI[0]).ScrollPosition.y = 0f;
                })
                //.AddUnit("godmode", Godmode)
                .AddGroup("spawn", Spawn, SpawnAutocompletionSettings)
                .AddGroup("godmode", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.Godmode, "Godmode"), TrueFalseAutocompletionSettings)
                .AddGroup("quick_kill", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.Quickkill, "Quick Kill"), TrueFalseAutocompletionSettings)
                .AddGroup("instant_charge", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.InstaCharge, "Instant Charge"), TrueFalseAutocompletionSettings)
                .AddGroup("high_damage", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.HighDamage, "High Damage"), TrueFalseAutocompletionSettings)
                .AddGroup("no_curse", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.NoCurse, "No Curse"), TrueFalseAutocompletionSettings)
                .AddGroup("perfect_aim", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.PerfectAim, "Perfect Aim"), TrueFalseAutocompletionSettings)
                .AddGroup("infinite_ammo", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.InfiniteAmmo, "Infinite Ammo"), TrueFalseAutocompletionSettings)
                .AddGroup("flight", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.Flight, "Flight"), TrueFalseAutocompletionSettings)
                .AddGroup("no_clip", (string[] a) => SwitchValue(a.FirstOrDefault(), ref ETGModMainBehaviour.NoClip, "No Clip"), TrueFalseAutocompletionSettings)
                .AddGroup("reveal_floor", x => Minimap.Instance.RevealAllRooms(true))
                .AddGroup("visit_all_rooms", x => { Minimap.Instance.RevealAllRooms(true); GameManager.Instance?.Dungeon?.data?.rooms?.ForEach(x => x.visibility = Dungeonator.RoomHandler.VisibilityStatus.VISITED); })
                .AddGroup("add_teleporters", x => GameManager.Instance?.Dungeon?.data?.rooms?.ForEach(x => x?.AddProceduralTeleporterToRoom()))
                .AddGroup("load_level", x => { if (x.Length <= 0) { Log("Level not given!"); return; } LoadLevel(x[0], false, false); }, LevelAutocompletionSettings);
        Commands.GetGroup("godmode").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.Godmode, "Godmode"));
        Commands.GetGroup("quick_kill").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.Quickkill, "Quick Kill"));
        Commands.GetGroup("instant_charge").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.InstaCharge, "Instant Charge"));
        Commands.GetGroup("high_damage").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.HighDamage, "High Damage"));
        Commands.GetGroup("no_curse").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.NoCurse, "No Curse"));
        Commands.GetGroup("perfect_aim").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.PerfectAim, "Perfect Aim"));
        Commands.GetGroup("infinite_ammo").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.InfiniteAmmo, "Infinite Ammo"));
        Commands.GetGroup("flight").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.Flight, "Flight"));
        Commands.GetGroup("no_clip").AddUnit("value", (string[] a) => LogValue(ETGModMainBehaviour.NoClip, "No Clip"));
        Commands.GetGroup("load_level").AddUnit("show_dictionary", x => { var text = "<color=#00ff00>LEVEL DICTIONARY:</color>\n" +
                "     <color=#00ffff>* keep</color>, <color=#00ffff>leadlordkeep</color> or <color=#00ffff>lead_lord_keep</color> - Keep of the Lead Lord.\n" +
                "     <color=#00ffff>* proper</color>, <color=#00ffff>gungeon</color>, <color=#00ffff>gungeonproper</color> or <color=#00ffff>gungeon_proper</color> - Gungeon Proper\n" +
                "     <color=#00ffff>* mines</color>, <color=#00ffff>mine</color>, <color=#00ffff>powdermines</color>, <color=#00ffff>powdermine</color>, <color=#00ffff>powder_mines</color> or <color=#00ffff>powder_mine</color> - Black Powder Mines\n" +
                "     <color=#00ffff>* hollow</color> - Hollow\n" +
                "     <color=#00ffff>* forge</color> - Forge\n" +
                "     <color=#00ffff>* hell</color>, <color=#00ffff>bullethell</color> or <color=#00ffff>bullet_hell</color> - Bullet Hell\n" +
                "     <color=#00ffff>* oubliette</color>, <color=#00ffff>sewer</color> or <color=#00ffff>sewers</color> - Oubliette\n" +
                "     <color=#00ffff>* abbey</color>, <color=#00ffff>truegunabbbey</color> or <color=#00ffff>true_gun_abbey</color> - Abbey of the True Gun\n" +
                "     <color=#00ffff>* ratlair</color>, <color=#00ffff>ratden</color>, <color=#00ffff>rat_lair</color> or <color=#00ffff>rat_den</color> - Resourceful Rat's Lair\n" +
                "     <color=#00ffff>* rng</color>, <color=#00ffff>r&g</color>, <color=#00ffff>rngdept</color> or <color=#00ffff>r&gdept</color>, <color=#00ffff>rng_dept</color> or <color=#00ffff>r&g_dept</color> - R&G Dept.\n" +
                "     <color=#00ffff>* marinepast</color> or <color=#00ffff>marine_past</color> - Marine's Past\n" +
                "     <color=#00ffff>* convictpast</color> or <color=#00ffff>convict_past</color> - Convict's Past\n" +
                "     <color=#00ffff>* hunterpast</color> or <color=#00ffff>hunter_past</color> - Hunter's Past\n" +
                "     <color=#00ffff>* pilotpast</color> or <color=#00ffff>pilot_past</color> - Pilot's Past\n" +
                "     <color=#00ffff>* robotpast</color> or <color=#00ffff>robot_past</color> - Robot's Past\n" +
                "     <color=#00ffff>* bulletpast</color> or <color=#00ffff>bullet_past</color> - Bullet's Past\n" +
                "     <color=#00ffff>* gunslingerpast</color> or <color=#00ffff>gunslinger_past</color> - Gunslinger's Past\n" +
                "     <color=#00ffff>* cultistpast</color>, <color=#00ffff>cultist_past</color>, <color=#00ffff>cooppast</color> or <color=#00ffff>coop_past</color> - Cultist's Past\n" +
                "     <color=#00ffff>* tutorial</color>, <color=#00ffff>halls</color>, <color=#00ffff>knowledgehalls</color> or <color=#00ffff>knowledge_halls</color> - Halls of Knowledge\n" +
                "<color=#00ff00>AVAILABLE LEVELS:</color>";
            List<string> availableLevels = new();
            if (GameManager.HasInstance)
            {
                if (GameManager.Instance.dungeonFloors != null)
                {
                    foreach (GameLevelDefinition def in GameManager.Instance.dungeonFloors)
                    {
                        availableLevels.Add("\n     <color=#00ffff>* " + def.dungeonSceneName + " (prefab path: " + def.dungeonPrefabPath + ")</color>");
                    }
                }
                if (GameManager.Instance.customFloors != null)
                {
                    foreach (GameLevelDefinition def in GameManager.Instance.customFloors)
                    {
                        availableLevels.Add("\n     <color=#00ffff>* " + def.dungeonSceneName + " (prefab path: " + def.dungeonPrefabPath + ")</color>");
                    }
                }
            }
            Log(text + string.Join("", availableLevels.ToArray()));
        });
        Commands.GetGroup("load_level").AddGroup("ignore_dictionary", x => { if (x.Length <= 0) { Log("Level not given!"); return; } LoadLevel(x[0], true, false); }, LevelAutocompletionSettings);
        Commands.GetGroup("load_level").GetGroup("ignore_dictionary").AddUnit("glitched", x => { if (x.Length <= 0) { Log("Level not given!"); return; } LoadLevel(x[0], true, true); }, LevelAutocompletionSettings);
        Commands.GetGroup("load_level").AddUnit("glitched", x => { if (x.Length <= 0) { Log("Level not given!"); return; } LoadLevel(x[0], false, true); }, LevelAutocompletionSettings);

        // SPAWN NAMESAPCE
        Commands.GetGroup("spawn")
                .AddGroup("chest", x => SpawnChest(x), 
                    AutocompletionFromCollectionGetterMultilevel(x => x == 0 ? BaseGameChests.Concat(ModdedChests.Keys).ToArray() : x == 1 ? new string[] {"normal", "locked", "mimic"} : new string[0]))
                .AddUnit("item", SpawnItem, GiveAutocompletionSettings)
                .AddUnit("shrine", SpawnShrine, ShrineAutocompletionSettings)
                .AddUnit("npc", SpawnNPC, NPCAutocompletionSettings);

        Commands.GetGroup("spawn").GetGroup("chest").AddUnit("all_guns", x => SpawnChest(x, true),
            AutocompletionFromCollectionGetterMultilevel(x => x == 0 ? BaseGameChests.Concat(ModdedChests.Keys).ToArray() : x == 1 ? new string[] { "normal", "locked", "mimic" } : new string[0]));
        Commands.GetGroup("spawn").GetGroup("chest").AddUnit("all_items", x => SpawnChest(x, false, true),
            AutocompletionFromCollectionGetterMultilevel(x => x == 0 ? BaseGameChests.Concat(ModdedChests.Keys).ToArray() : x == 1 ? new string[] { "normal", "locked", "mimic" } : new string[0]));

        // GIVE NAMESPACE
        Commands.AddGroup("give", GiveItem, GiveAutocompletionSettings);

        // STAT NAMESPACE
        //Commands.AddGroup("stat");

        //Commands.GetGroup("stat")
        //        .AddUnit("get", StatGet, _StatAutocompletionSettings)
        //        .AddGroup("set", StatSetCurrentCharacter, _StatAutocompletionSettings)
        //        .AddUnit("mod", StatMod, _StatAutocompletionSettings)
        //        .AddUnit("list", StatList);

        //Commands.GetGroup("stat").GetGroup("set")
        //                          .AddUnit("session", StatSetSession, _StatAutocompletionSettings);

        Commands.AddGroup("tracked_stat");
        Commands.GetGroup("tracked_stat")
            .AddUnit("list", StatList)
            .AddUnit("get", StatGet, TrackedStatAutocompletionSettings)
            .AddUnit("add", StatMod, TrackedStatAutocompletionSettings)
            .AddUnit("set", StatSet, TrackedStatAutocompletionSettings);

        Commands.AddGroup("stat");
        Commands.GetGroup("stat")
            .AddUnit("get", x =>
            {
                if (x.Length <= 0)
                {
                    Log("Stat not given!");
                    return;
                }
                if (!PlayerStatDictionary.TryGetValue(x[0], out var stat))
                {
                    Log($"\"{x[0]}\" is not a valid stat!");
                    return;
                }
                if(GameManager.Instance?.PrimaryPlayer?.stats == null)
                {
                    Log("Player doesn't exist!");
                    return;
                }
                Log($"Value of {stat}: {GameManager.Instance.PrimaryPlayer.stats.GetStatValue(stat)}");
            }, PlayerStatAutocompletionSettings)
            .AddUnit("set", x =>
            {
                if (x.Length <= 0)
                {
                    Log("Stat not given!");
                    return;
                }
                if (!PlayerStatDictionary.TryGetValue(x[0], out var stat))
                {
                    Log($"\"{x[0]}\" is not a valid stat!");
                    return;
                }
                if (x.Length <= 1)
                {
                    Log("Value not given!");
                    return;
                }
                if (!float.TryParse(x[1], out var value))
                {
                    Log($"\"{x[1]}\" is not a valid value!");
                    return;
                }
                var owner = GameManager.Instance.PrimaryPlayer;
                if(owner == null || owner.stats == null)
                {
                    return;
                }
                float[] array = new float[owner.stats.BaseStatValues.Count];
                float[] array2 = new float[owner.stats.BaseStatValues.Count];
                for (int j = 0; j < array2.Length; j++)
                {
                    array2[j] = 1f;
                }
                float num = 0f;
                owner.stats.ActiveCustomSynergies.Clear();
                for (int k = 0; k < owner.ActiveExtraSynergies.Count; k++)
                {
                    AdvancedSynergyEntry advancedSynergyEntry = GameManager.Instance.SynergyManager.synergies[owner.ActiveExtraSynergies[k]];
                    if (advancedSynergyEntry.SynergyIsActive(GameManager.Instance.PrimaryPlayer, GameManager.Instance.SecondaryPlayer))
                    {
                        for (int l = 0; l < advancedSynergyEntry.statModifiers.Count; l++)
                        {
                            StatModifier statModifier = advancedSynergyEntry.statModifiers[l];
                            int statToBoost = (int)statModifier.statToBoost;
                            if (statModifier.modifyType == StatModifier.ModifyMethod.ADDITIVE)
                            {
                                array[statToBoost] += statModifier.amount;
                            }
                            else if (statModifier.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE)
                            {
                                array2[statToBoost] *= statModifier.amount;
                            }
                        }
                        for (int m = 0; m < advancedSynergyEntry.bonusSynergies.Count; m++)
                        {
                            owner.stats.ActiveCustomSynergies.Add(advancedSynergyEntry.bonusSynergies[m]);
                        }
                    }
                }
                for (int n = 0; n < owner.ownerlessStatModifiers.Count; n++)
                {
                    StatModifier statModifier2 = owner.ownerlessStatModifiers[n];
                    if (!statModifier2.hasBeenOwnerlessProcessed && statModifier2.statToBoost == PlayerStats.StatType.Health && statModifier2.amount > 0f)
                    {
                        num += statModifier2.amount;
                    }
                    int statToBoost2 = (int)statModifier2.statToBoost;
                    if (statModifier2.modifyType == StatModifier.ModifyMethod.ADDITIVE)
                    {
                        array[statToBoost2] += statModifier2.amount;
                    }
                    else if (statModifier2.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE)
                    {
                        array2[statToBoost2] *= statModifier2.amount;
                    }
                    statModifier2.hasBeenOwnerlessProcessed = true;
                }
                void ApplyStatModifier(StatModifier modifier, float[] statModsAdditive, float[] statModsMultiplic)
                {
                    int statToBoost = (int)modifier.statToBoost;
                    if (modifier.modifyType == StatModifier.ModifyMethod.ADDITIVE)
                    {
                        statModsAdditive[statToBoost] += modifier.amount;
                    }
                    else if (modifier.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE)
                    {
                        statModsMultiplic[statToBoost] *= modifier.amount;
                    }
                }
                for (int num2 = 0; num2 < owner.passiveItems.Count; num2++)
                {
                    PassiveItem passiveItem = owner.passiveItems[num2];
                    if (passiveItem.passiveStatModifiers != null && passiveItem.passiveStatModifiers.Length > 0)
                    {
                        for (int num3 = 0; num3 < passiveItem.passiveStatModifiers.Length; num3++)
                        {
                            StatModifier statModifier3 = passiveItem.passiveStatModifiers[num3];
                            if (!passiveItem.HasBeenStatProcessed && statModifier3.statToBoost == PlayerStats.StatType.Health && statModifier3.amount > 0f)
                            {
                                num += statModifier3.amount;
                            }
                            ApplyStatModifier(statModifier3, array, array2);
                        }
                    }
                    if (passiveItem is BasicStatPickup)
                    {
                        BasicStatPickup basicStatPickup = passiveItem as BasicStatPickup;
                        for (int num4 = 0; num4 < basicStatPickup.modifiers.Count; num4++)
                        {
                            StatModifier statModifier4 = basicStatPickup.modifiers[num4];
                            if (!passiveItem.HasBeenStatProcessed && statModifier4.statToBoost == PlayerStats.StatType.Health && statModifier4.amount > 0f)
                            {
                                num += statModifier4.amount;
                            }
                            ApplyStatModifier(statModifier4, array, array2);
                        }
                    }
                    if (passiveItem is CoopPassiveItem && (GameManager.Instance.CurrentGameType == GameManager.GameType.SINGLE_PLAYER || (GameManager.Instance.PrimaryPlayer.healthHaver && GameManager.Instance.PrimaryPlayer.healthHaver.IsDead) || owner.HasActiveBonusSynergy(CustomSynergyType.THE_TRUE_HERO, false)))
                    {
                        CoopPassiveItem coopPassiveItem = passiveItem as CoopPassiveItem;
                        for (int num5 = 0; num5 < coopPassiveItem.modifiers.Count; num5++)
                        {
                            StatModifier modifier = coopPassiveItem.modifiers[num5];
                            ApplyStatModifier(modifier, array, array2);
                        }
                    }
                    if (passiveItem is MetronomeItem)
                    {
                        float currentMultiplier = (passiveItem as MetronomeItem).GetCurrentMultiplier();
                        array2[5] *= currentMultiplier;
                    }
                    passiveItem.HasBeenStatProcessed = true;
                }
                if (owner.inventory != null && owner.inventory.AllGuns != null)
                {
                    if (owner.inventory.CurrentGun != null && owner.inventory.CurrentGun.currentGunStatModifiers != null && owner.inventory.CurrentGun.currentGunStatModifiers.Length > 0)
                    {
                        for (int num6 = 0; num6 < owner.inventory.CurrentGun.currentGunStatModifiers.Length; num6++)
                        {
                            StatModifier modifier2 = owner.inventory.CurrentGun.currentGunStatModifiers[num6];
                            ApplyStatModifier(modifier2, array, array2);
                        }
                    }
                    for (int num7 = 0; num7 < owner.inventory.AllGuns.Count; num7++)
                    {
                        if (owner.inventory.AllGuns[num7] && owner.inventory.AllGuns[num7].passiveStatModifiers != null && owner.inventory.AllGuns[num7].passiveStatModifiers.Length > 0)
                        {
                            for (int num8 = 0; num8 < owner.inventory.AllGuns[num7].passiveStatModifiers.Length; num8++)
                            {
                                StatModifier modifier3 = owner.inventory.AllGuns[num7].passiveStatModifiers[num8];
                                ApplyStatModifier(modifier3, array, array2);
                            }
                        }
                    }
                }
                for (int num9 = 0; num9 < owner.activeItems.Count; num9++)
                {
                    PlayerItem playerItem = owner.activeItems[num9];
                    if (playerItem.passiveStatModifiers != null && playerItem.passiveStatModifiers.Length > 0)
                    {
                        for (int num10 = 0; num10 < playerItem.passiveStatModifiers.Length; num10++)
                        {
                            StatModifier statModifier5 = playerItem.passiveStatModifiers[num10];
                            if (!playerItem.HasBeenStatProcessed && statModifier5.statToBoost == PlayerStats.StatType.Health && statModifier5.amount > 0f)
                            {
                                num += statModifier5.amount;
                            }
                            ApplyStatModifier(statModifier5, array, array2);
                        }
                    }
                    StatHolder component = playerItem.GetComponent<StatHolder>();
                    if (component && (!component.RequiresPlayerItemActive || playerItem.IsCurrentlyActive))
                    {
                        for (int num11 = 0; num11 < component.modifiers.Length; num11++)
                        {
                            StatModifier statModifier6 = component.modifiers[num11];
                            if (!playerItem.HasBeenStatProcessed && statModifier6.statToBoost == PlayerStats.StatType.Health && statModifier6.amount > 0f)
                            {
                                num += statModifier6.amount;
                            }
                            ApplyStatModifier(statModifier6, array, array2);
                        }
                    }
                }
                PlayerItem currentItem = owner.CurrentItem;
                if (currentItem && currentItem is ActiveBasicStatItem && currentItem.IsActive)
                {
                    ActiveBasicStatItem activeBasicStatItem = currentItem as ActiveBasicStatItem;
                    for (int num12 = 0; num12 < activeBasicStatItem.modifiers.Count; num12++)
                    {
                        StatModifier modifier4 = activeBasicStatItem.modifiers[num12];
                        ApplyStatModifier(modifier4, array, array2);
                    }
                }
                var divider = array2[(int)stat];
                if(divider == 0f)
                {
                    divider = 0.1f;
                    Log("Stat multiplier by 0 found! Can't set stat properly.");
                }
                GameManager.Instance?.PrimaryPlayer?.stats?.SetBaseStatValue(stat, (value - array[(int)stat]) / divider, GameManager.Instance?.PrimaryPlayer);
                GameManager.Instance?.PrimaryPlayer?.stats?.RecalculateStats(GameManager.Instance?.PrimaryPlayer, false, false);
            }, PlayerStatAutocompletionSettings)
            .AddUnit("set_exact", x =>
            {
                if (x.Length <= 0)
                {
                    Log("Stat not given!");
                    return;
                }
                if (!PlayerStatDictionary.TryGetValue(x[0], out var stat))
                {
                    Log($"\"{x[0]}\" is not a valid stat!");
                    return;
                }
                if (x.Length <= 1)
                {
                    Log("Value not given!");
                    return;
                }
                if (!float.TryParse(x[1], out var value))
                {
                    Log($"\"{x[1]}\" is not a valid value!");
                    return;
                }
                GameManager.Instance?.PrimaryPlayer?.stats?.SetBaseStatValue(stat, value, GameManager.Instance?.PrimaryPlayer);
                GameManager.Instance?.PrimaryPlayer?.stats?.RecalculateStats(GameManager.Instance?.PrimaryPlayer, false, false);
            }, PlayerStatAutocompletionSettings)
            .AddUnit("add", x =>
            {
                if (x.Length <= 0)
                {
                    Log("Stat not given!");
                    return;
                }
                if (!PlayerStatDictionary.TryGetValue(x[0], out var stat))
                {
                    Log($"\"{x[0]}\" is not a valid stat!");
                    return;
                }
                if (x.Length <= 1)
                {
                    Log("Value not given!");
                    return;
                }
                if (!float.TryParse(x[1], out var value))
                {
                    Log($"\"{x[1]}\" is not a valid value!");
                    return;
                }
                GameManager.Instance?.PrimaryPlayer?.ownerlessStatModifiers?.Add(StatModifier.Create(stat, StatModifier.ModifyMethod.ADDITIVE, value));
                GameManager.Instance?.PrimaryPlayer?.stats?.RecalculateStats(GameManager.Instance?.PrimaryPlayer, false, false);
            }, PlayerStatAutocompletionSettings)
            .AddUnit("multiply", x =>
            {
                if(x.Length <= 0)
                {
                    Log("Stat not given!");
                    return;
                }
                if(!PlayerStatDictionary.TryGetValue(x[0], out var stat))
                {
                    Log($"\"{x[0]}\" is not a valid stat!");
                    return;
                }
                if(x.Length <= 1)
                {
                    Log("Value not given!");
                    return;
                }
                if(!float.TryParse(x[1], out var value))
                {
                    Log($"\"{x[1]}\" is not a valid value!");
                    return;
                }
                GameManager.Instance?.PrimaryPlayer?.ownerlessStatModifiers?.Add(StatModifier.Create(stat, StatModifier.ModifyMethod.MULTIPLICATIVE, value));
                GameManager.Instance?.PrimaryPlayer?.stats?.RecalculateStats(GameManager.Instance?.PrimaryPlayer, false, false);
            }, PlayerStatAutocompletionSettings)
            .AddUnit("remove_0_multipliers", x =>
            {
                GameManager.Instance?.PrimaryPlayer?.ownerlessStatModifiers?.RemoveAll(x => x.amount == 0f && x.modifyType == StatModifier.ModifyMethod.MULTIPLICATIVE);
                GameManager.Instance?.PrimaryPlayer?.stats?.RecalculateStats(GameManager.Instance?.PrimaryPlayer, false, false);
                Log("Multipliers by 0 removed!");
            }, PlayerStatAutocompletionSettings);

        Commands
                .AddUnit("skiplevel", delegate (string[] args)
                {
                    Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
                    GameManager.Instance.DelayedLoadNextLevel(0.5f);
                }).AddUnit("charselect", delegate (string[] args)
                {
                    Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
                    GameManager.Instance.DelayedLoadCharacterSelect(0.5f);
                });

        // DUMP NAMESPACE
        //Commands.AddUnit("dump", new ConsoleCommandGroup());

        //Commands.GetGroup("dump")
        //        .AddGroup("sprites", (args) => SetBool(args, ref ETGMod.Assets.DumpSprites))
        //        .AddUnit("packer", (args) => ETGMod.Assets.Dump.DumpPacker())
        //        .AddUnit("synergies", DumpSynergies)
        //        .AddUnit("pickups", DumpPickups);

        //Commands.GetGroup("dump").GetGroup("sprites")
        //                          .AddUnit("metadata", (args) => SetBool(args, ref ETGMod.Assets.DumpSpritesMetadata));

        // CONF NAMESPACE
        Commands.AddGroup("conf");

        Commands.GetGroup("conf")
                .AddGroup("close_console_on_command", (args) => SwitchValue(args.FirstOrDefault(), ref CloseConsoleOnCommand, "Close console on command"), TrueFalseAutocompletionSettings);
        Commands.GetGroup("conf").GetGroup("close_console_on_command").AddUnit("value", x => LogValue(CloseConsoleOnCommand, "Close console on command"));
        // .AddUnit("player", (args) => ETGMod.Player.PlayerReplacement = args.Length == 1 ? args[0] : null)
        //.AddUnit("player_coop", (args) => ETGMod.Player.CoopReplacement = args.Length == 1 ? args[0] : null);
    }

    public override void Update()
    {

    }

    protected virtual SLabel _Log(object text)
    {
        var slab = new SLabel(text.ToString());
        GUI[0].Children.Add(slab);
        ((SGroup)GUI[0]).ScrollPosition.y = float.MaxValue;
        return slab;
    }

    protected virtual SButton _LogButton(object text, Action<SButton> onClick, Texture texture)
    {
        var butt = new SButton(text.ToString());
        butt.Icon = texture;
        butt.OnClick = onClick;
        GUI[0].Children.Add(butt);
        ((SGroup)GUI[0]).ScrollPosition.y = float.MaxValue;
        return butt;
    }

    protected virtual SImage _LogImage(Texture tex, Color? color = null)
    {
        var im = new SImage(tex, color ?? Color.white);
        GUI[0].Children.Add(im);
        ((SGroup)GUI[0]).ScrollPosition.y = float.MaxValue;
        return im;
    }

    public static SLabel Log(object text, bool debuglog = false)
    {
        if(Instance == null)
        {
            ETGModGUI.Create();
            ETGModGUI.Start();
        }
        var result = Instance?._Log(text);
        if (debuglog)
        {
            Debug.Log(text);
        }
        return result;
    }

    public static SButton LogButton(object text, Action<SButton> onClick = null, Texture texture = null)
    {
        if (Instance == null)
        {
            ETGModGUI.Create();
            ETGModGUI.Start();
        }
        var result = Instance?._LogButton(text, onClick, texture);
        return result;
    }

    public static SImage LogImage(Texture tex, Color? color = null)
    {
        if (Instance == null)
        {
            ETGModGUI.Create();
            ETGModGUI.Start();
        }
        var result = Instance?._LogImage(tex, color);
        return result;
    }

    public string[] SplitArgs(string args)
    {
        return args.Split(_SplitArgsCharacters, StringSplitOptions.RemoveEmptyEntries);
    }

    public void ParseCommand(string text)
    {
        string[] input = SplitArgs(text.Trim());
        int commandindex = Commands.GetFirstNonUnitIndexInPath(input);

        List<string> path = new List<string>();
        for (int i = 0; i < input.Length - (input.Length - commandindex); i++)
        {
            if (!string.IsNullOrEmpty(input[i])) path.Add(input[i]);
        }

        List<string> args = new List<string>();
        for (int i = commandindex; i < input.Length; i++)
        {
            if (!string.IsNullOrEmpty(input[i])) args.Add(input[i]);
        }
        RunCommand(path.ToArray(), args.ToArray());
    }

    public void ShowAutocomplete()
    {
        STextField field = (STextField)GUI[1];

        // AUTO COMPLETIONATOR 3000
        // (by Zatherz) (made good by specia lapi) (frogs) (what)
        //
        // TODO: Make Tab autocomplete to the shared part of completions
        // TODO: AutocompletionRule interface and class per rule?
        // Create an input array by splitting it on spaces
        string inputtext = field.Text.Substring(0, field.CursorIndex);
        string[] input = SplitArgs(inputtext);
        string otherinput = string.Empty;
        if (field.CursorIndex < field.Text.Length)
        {
            otherinput = field.Text.Substring(field.CursorIndex + 1);
        }
        // Get where the command appears in the path so that we know where the arguments start
        int commandindex = Commands.GetFirstNonUnitIndexInPath(input);
        List<string> pathlist = new List<string>();
        for (int i = 0; i < input.Length - (input.Length - commandindex); i++)
        {
            pathlist.Add(input[i]);
        }

        ConsoleCommandUnit unit = Commands.GetUnit(pathlist.ToArray());
        // Get an array of available completions
        int matchindex = input.Length - pathlist.Count;
        var originalLength = pathlist.Count;
        for(int i = 0; i < matchindex; i++)
        {
            if (i + 1 >= matchindex && !inputtext.EndsWith(" ", StringComparison.InvariantCulture))
            {
                break;
            }
            pathlist.Add(input[originalLength + i]);
        }

        string[] path = pathlist.ToArray();
        /*
		HACK! blame Zatherz
		matchindex will be off by +1 if the current keyword your cursor is on isn't empty
		this will check if there are no spaces on the left on the cursor
		and if so, decrease matchindex
		if there *are* spaces on the left of the cursor, that means the current
		"token" the cursor is on is an empty string, so that doesn't have any problems
		Hopefully this is a good enough explanation, if not, ping @Zatherz on Discord
		*/

        string matchkeyword = string.Empty;
        if (!inputtext.EndsWith(" ", StringComparison.InvariantCulture) && inputtext.Length > 0)
        {
            matchindex--;
            matchkeyword = input[input.Length - 1];
        }

        string[] completions;
        try { completions = unit.Autocompletion.Match(Math.Max(matchindex, 0), matchkeyword); } catch { completions = new string[0]; }

        if (completions == null || completions.Length <= 0)
        {
        }
        else if (completions.Length == 1 || GUI.Children.Count >= 3)
        {
            if (path.Length > 0)
            {
                field.Text = string.Join(" ", path) + " " + completions[0] + " " + otherinput;
            }
            else
            {
                field.Text = completions[0] + " " + otherinput;
            }

            field.MoveCursor(field.Text.Length);
            HideAutocomplete();
        }
        else if (completions.Length > 1)
        {
            SGroup hints = new SGroup
            {
                Parent = GUI,
                AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                ScrollDirection = SGroup.EDirection.Vertical,
                OnUpdateStyle = delegate (SElement elem)
                {
                    elem.Size = new Vector2(elem.Parent.InnerSize.x, Mathf.Min(((SGroup)elem).ContentSize.y, 160f));
                    elem.Position = GUI[1].Position - new Vector2(0f, elem.Size.y + 4f);
                }
            };

            for (int i = 0; i < completions.Length; i++)
            {
                hints.Children.Add(new SLabel(completions[i]));
            }
        }
    }

    public static AutocompletionSettings AutocompletionFromCollection(IEnumerable<string> collection)
    {
        return new AutocompletionSettings(x => collection?.Distinct()?.Where(x2 => (x2?.ToLowerInvariant()?.AutocompletionMatch(x.ToLowerInvariant())).GetValueOrDefault())?.ToArray() ?? new string[0]);
    }

    public static AutocompletionSettings AutocompletionFromCollection(IEnumerable<IEnumerable<string>> collection)
    {
        return new AutocompletionSettings((count, x) => collection?.ToList()[count]?.Distinct()?.Where(x2 => (x2?.ToLowerInvariant()?.AutocompletionMatch(x.ToLowerInvariant())).GetValueOrDefault())?.ToArray() ?? new string[0]);
    }

    public static AutocompletionSettings AutocompletionFromCollectionGetter(Func<IEnumerable<string>> collectionGet)
    {
        return new AutocompletionSettings(x => collectionGet?.Invoke()?.Distinct()?.Where(x2 => (x2?.ToLowerInvariant()?.AutocompletionMatch(x.ToLowerInvariant())).GetValueOrDefault())?.ToArray() ?? new string[0]);
    }

    public static AutocompletionSettings AutocompletionFromCollectionGetterMultilevel(Func<int, IEnumerable<string>> collectionGet)
    {
        return new AutocompletionSettings((x, x2) => collectionGet?.Invoke(x)?.Distinct()?.Where(x3 => (x3?.ToLowerInvariant()?.AutocompletionMatch(x2.ToLowerInvariant())).GetValueOrDefault())?.ToArray() ?? new string[0]);
    }

    public void HideAutocomplete()
    {
        if (GUI.Children.Count < 3)
        {
            return;
        }
        GUI.Children.RemoveAt(2);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        GUI[1].Focus();
    }

    public static bool ArgCount(string[] args, int min)
    {
        if (args.Length >= min) return true;
        Log("Error: need at least " + min + " argument(s)");
        return false;
    }

    public static bool ArgCount(string[] args, int min, int max)
    {
        if (args.Length >= min && args.Length <= max) return true;
        if (min == max)
        {
            Log("Error: need exactly " + min + " argument(s)");
        }
        else
        {
            Log("Error: need between " + min + " and " + max + " argument(s)");
        }
        return false;
    }

    /// <summary>
    /// Runs the provided command with the provided args.
    /// </summary>
    public static void RunCommand(string[] unit, string[] args)
    {
        ConsoleCommandUnit command = Commands.GetUnit(unit);
        if (command == null)
        {
            if (Commands.GetGroup(unit) == null)
            {
                Log("Command doesn't exist");
            }
        }
        else
        {
            try
            {
                command.RunCommand(args);
            }
            catch (Exception e)
            {
                Log(e.ToString());

                Debug.LogError("Exception occured while running command:" + e.ToString());
            }
        }
    }

    public static void Teleport(string[] args)
    {
        if(args.Length < 2)
        {
            Log("Coordinates not given!");
            return;
        }
        if(args.Length == 1)
        {
            Log("Y coordinate not given!");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null)
        {
            GameManager.Instance.PrimaryPlayer.TeleportToPoint(new Vector2(
                float.Parse(args[0]),
                float.Parse(args[1])
            ), true);
        }
    }

    public static void RelativeTeleport(string[] args)
    {
        if (args.Length < 2)
        {
            Log("Coordinates not given!");
            return;
        }
        if (args.Length == 1)
        {
            Log("Y coordinate not given!");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.PrimaryPlayer != null)
        {
            GameManager.Instance.PrimaryPlayer.TeleportToPoint(GameManager.Instance.PrimaryPlayer.transform.position.XY() + new Vector2(
                float.Parse(args[0]),
                float.Parse(args[1])
            ), true);
        }
    }

    public static void SwitchValue(string arg, ref bool debugValue, string name)
    {
        if(arg != null && bool.TryParse(arg, out var b))
        {
            if(debugValue == b)
            {
                Log(name + " is previously " + (debugValue ? "on" : "off") + ".");
            }
            else
            {
                Log(name + " was previously " + (debugValue ? "on" : "off") + ".");
                debugValue = b;
                Log(name + " is now " + (debugValue ? "on" : "off") + ".");
            }
            return;
        }
        Log(name + " was previously " + (debugValue ? "on" : "off") + ".");
        debugValue = !debugValue;
        Log(name + " is now " + (debugValue ? "on" : "off") + ".");
    }

    public static void LogValue(bool debugValue, string name)
    {
        Log(name + " is now " + (debugValue ? "on" : "off") + ".");
    }

    public static void GiveItem(string[] args)
    {
        if (args.Length < 1)
        {
            Log("Item not given!");
            return;
        }

        if (!GameManager.Instance.PrimaryPlayer)
        {
            Log("Couldn't access Player Controller");
            return;
        }

        string id = args[0];
        if (!Game.Items.ContainsID(id))
        {
            Log($"Invalid item ID {id}!");
            return;
        }

        Log("Attempting to spawn item ID " + args[0] + " (numeric " + id + ")" + ", class " + Game.Items.Get(id).GetType());

        if (Game.Items[id].gameObject.GetComponent<NPCCellKeyItem>())
        {
            GameManager.Instance.PrimaryPlayer.BloopItemAboveHead(Game.Items[id].gameObject.GetComponent<NPCCellKeyItem>().sprite, string.Empty);
            GameManager.Instance.PrimaryPlayer.AcquirePuzzleItem(Game.Items[id].gameObject.GetComponent<NPCCellKeyItem>());
        }
        else
        {
            if (args.Length >= 2 && int.TryParse(args[1], out var count))
            {

                for (int i = 0; i < count; i++)
                    GameManager.Instance.PrimaryPlayer.GiveItem(id);
            }
            else
            {
                GameManager.Instance.PrimaryPlayer.GiveItem(id);
            }
        }
    }

    public static void SpawnItem(string[] args)
    {
        if (args.Length < 1)
        {
            Log("Item not given!");
            return;
        }

        if (!GameManager.Instance.PrimaryPlayer)
        {
            Log("Couldn't access Player Controller");
            return;
        }

        string id = args[0];
        if (!Game.Items.ContainsID(id))
        {
            Log($"Invalid item ID {id}!");
            return;
        }

        Log("Attempting to spawn item ID " + args[0] + " (numeric " + id + ")" + ", class " + Game.Items.Get(id).GetType());

        if (args.Length == 2 && int.TryParse(args[1], out var count))
        {
            for (int i = 0; i < count; i++)
                LootEngine.SpawnItem(Game.Items[args[0]].gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.down, 0f, true, false, false);
        }
        else
        {
            LootEngine.SpawnItem(Game.Items[args[0]].gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.down, 0f, true, false, false);
        }
    }

    public static void SpawnShrine(string[] args)
    {
        if(args.Length <= 0)
        {
            Log("Shrine not given!");
            return;
        }
        if (GameManager.Instance?.PrimaryPlayer == null)
        {
            Log("Player doesn't exist!");
            return;
        }
        var count = 1;
        PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
        RoomHandler currentRoom = primaryPlayer.CurrentRoom;
        AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
        bool isChallengeShrine = args[0] == "challenge";
        string name = "shrine_" + args[0] + (isChallengeShrine ? "_gungeon_001" : "");
        GameObject shrine = assetBundle.LoadAsset(name) as GameObject;
        if(shrine == null)
        {
            ModdedShrines.TryGetValue(args[0], out shrine);
        }
        if(shrine == null)
        {
            Log($"Invalid shrine type {args[0]}!");
            return;
        }
        DungeonPlaceableBehaviour placeable = shrine.GetComponentInChildren<DungeonPlaceableBehaviour>();
        for(int i = 0; i < count; i++)
        {
            IntVector2 rewardLocation = currentRoom.GetBestRewardLocation(new(2, 2), RoomHandler.RewardLocationStyle.PlayerCenter, false);
            IntVector2 location = rewardLocation - currentRoom.area.basePosition;
            GameObject shrineInstance;
            if (placeable != null)
            {
                shrineInstance = placeable.InstantiateObject(primaryPlayer.CurrentRoom, location, false);
            }
            else
            {
                shrineInstance = UnityEngine.Object.Instantiate(shrine, rewardLocation.ToVector3(), Quaternion.identity);
            }
            IPlayerInteractable[] interactables = shrineInstance.GetInterfacesInChildren<IPlayerInteractable>();
            for (int j = 0; j < interactables.Length; j++)
            {
                currentRoom.RegisterInteractable(interactables[j]);
            }
            shrineInstance.GetComponentsInChildren<SpeculativeRigidbody>().ToList().ForEach(x => PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(x, null, false));
        }
    }

    public static void SpawnNPC(string[] args)
    {
        if (args.Length <= 0)
        {
            Log("NPC not given!");
            return;
        }
        if (GameManager.Instance?.PrimaryPlayer == null)
        {
            Log("Player doesn't exist!");
            return;
        }
        var count = 1;
        PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
        RoomHandler currentRoom = primaryPlayer.CurrentRoom;
        GameObject go;
        if(BaseGameNPCs.TryGetValue(args[0], out var path))
        {
            go = LoadHelper.LoadAssetFromAnywhere<GameObject>(path);
        }
        else
        {
            ModdedNPCs.TryGetValue(args[0], out go);
        }
        if(go == null)
        {
            Log($"Invalid NPC type {args[0]}!");
            return;
        }
        var placeable = go.GetComponentInChildren<DungeonPlaceableBehaviour>();
        for(int i = 0; i < count; i++)
        {
            IntVector2 rewardLocation = currentRoom.GetBestRewardLocation(new(2, 2), RoomHandler.RewardLocationStyle.PlayerCenter, false);
            IntVector2 location = rewardLocation - currentRoom.area.basePosition;
            GameObject instance;
            if (placeable != null)
            {
                instance = placeable.InstantiateObject(primaryPlayer.CurrentRoom, location, false);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(go, rewardLocation.ToVector3(), Quaternion.identity);
            }
            IPlayerInteractable[] interfacesInChildren = instance.GetInterfacesInChildren<IPlayerInteractable>();
            for (int j = 0; j < interfacesInChildren.Length; j++)
            {
                currentRoom.RegisterInteractable(interfacesInChildren[j]);
            }
            instance.GetComponentsInChildren<SpeculativeRigidbody>().ToList().ForEach(x => PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(x, null, false));
        }
    }

    public static void Spawn(string[] args)
    {
        if(args.Length < 1)
        {
            Log("Object to spawn not given!");
            return;
        }
        string id = args[0];
        if (!Game.Enemies.ContainsID(id))
        {
            Log($"Enemy with ID {id} doesn't exist");
            return;
        }
        AIActor enemyPrefab = Game.Enemies[id];
        Log("Spawning ID " + id);
        int count = 1;
        if (args.Length > 1 && !int.TryParse(args[1], out count))
        {
            count = 1;
        }
        for (int i = 0; i < count; i++)
        {
            IntVector2? targetCenter = new IntVector2?(GameManager.Instance.PrimaryPlayer.CenterPosition.ToIntVector2(VectorConversions.Floor));
            Pathfinding.CellValidator cellValidator = delegate (IntVector2 c)
            {
                for (int j = 0; j < enemyPrefab.Clearance.x; j++)
                {
                    for (int k = 0; k < enemyPrefab.Clearance.y; k++)
                    {
                        if (GameManager.Instance.Dungeon.data.isTopWall(c.x + j, c.y + k))
                        {
                            return false;
                        }
                        if (targetCenter.HasValue)
                        {
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) < 4)
                            {
                                return false;
                            }
                            if (IntVector2.Distance(targetCenter.Value, c.x + j, c.y + k) > 20)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            };
            IntVector2? randomAvailableCell = GameManager.Instance.PrimaryPlayer.CurrentRoom.GetRandomAvailableCell(new IntVector2?(enemyPrefab.Clearance), new Dungeonator.CellTypes?(enemyPrefab.PathableTiles), false, cellValidator);
            if (randomAvailableCell.HasValue)
            {
                AIActor aIActor = AIActor.Spawn(enemyPrefab, randomAvailableCell.Value, GameManager.Instance.PrimaryPlayer.CurrentRoom, true, AIActor.AwakenAnimationType.Default, true);
                aIActor.HandleReinforcementFallIntoRoom(0);
            }
        }
    }

    public static void LoadLevel(string level, bool ignoreDictionary, bool glitched)
    {
        Log("Attempting to load level \"" + level + "\"...");
        string sceneName = level;
        if (DungeonDictionary.ContainsKey(sceneName.ToLower()) && !ignoreDictionary)
        {
            sceneName = DungeonDictionary[sceneName.ToLower()];
        }
        if (GameManager.HasInstance)
        {
            GameLevelDefinition gameLevelDefinition = null;
            for (int i = 0; i < GameManager.Instance.dungeonFloors.Count; i++)
            {
                if (GameManager.Instance.dungeonFloors[i].dungeonSceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    gameLevelDefinition = GameManager.Instance.dungeonFloors[i];
                    break;
                }
            }
            if (gameLevelDefinition == null)
            {
                for (int j = 0; j < GameManager.Instance.customFloors.Count; j++)
                {
                    if (GameManager.Instance.customFloors[j].dungeonSceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        gameLevelDefinition = GameManager.Instance.customFloors[j];
                        break;
                    }
                }
            }
            if (gameLevelDefinition == null)
            {
                Log("Failed to load level: Level \"" + sceneName + "\" doesn't exist!");
            }
            else
            {
                if (GameManager.Instance.IsFoyer && Foyer.Instance != null)
                {
                    Foyer.Instance.OnDepartedFoyer();
                }
                if (glitched)
                {
                    GameManager.Instance.InjectedFlowPath = "Core Game Flows/Secret_DoubleBeholster_Flow";
                }
                if ((level == "gunslingerpast" || level == "gunslinger_past") && !ignoreDictionary)
                {
                    GameManager.IsGunslingerPast = true;
                }
                if (sceneName == "fs_coop")
                {
                    GameManager.IsCoopPast = true;
                }
                GameManager.Instance.LoadCustomLevel(sceneName);
                ETGModConsole.Log("Successfully loaded level \"" + level + "\" with scene name \"" + sceneName + "\".");
            }
        }
        else
        {
            ETGModConsole.Log("Failed to load level: Game Manager doesn't exist!");
        }
    }

    public static void SpawnChest(string[] args, bool isAlwaysGuns = false, bool isAlwaysItems = false)
    {
        if(args.Length <= 0)
        {
            Log("Chest type not given!");
            return;
        }
        if(GameManager.Instance?.PrimaryPlayer == null)
        {
            Log("Player doesn't exist!");
            return;
        }
        var currentRoom = GameManager.Instance.PrimaryPlayer.CurrentRoom;
        if(currentRoom == null)
        {
            return;
        }
        var chesttype = args[0].Trim().ToLowerInvariant();
        var rewards = GameManager.Instance?.RewardManager;
        Chest chest = chesttype switch
        {
            "brown" or "hidden_rainbow" => rewards?.D_Chest,
            "blue" => rewards?.C_Chest,
            "green" or "glitched" => rewards?.B_Chest,
            "red" => rewards?.A_Chest,
            "black" => rewards?.S_Chest,
            "synergy" or "rainbow_synergy" => rewards?.Synergy_Chest,
            "rainbow" => rewards?.Rainbow_Chest,
            "rat" => LoadHelper.LoadAssetFromAnywhere<GameObject>("chest_rat").GetComponent<Chest>(),
            "truth" => LoadHelper.LoadAssetFromAnywhere<GameObject>("TruthChest").GetComponent<Chest>(),
            "high_dragunfire" => 
                                    LoadHelper.LoadAssetFromAnywhere<GameObject>("highdragunfire_chest_red")?.GetComponent<Chest>() ?? 
                                    LoadHelper.LoadAssetFromAnywhere<GameObject>("highdragunfire_chest_red")?.GetComponent<FloorChestPlacer>()?.OverrideChestPrefab?.GetComponent<Chest>(),
            "random" => BraveUtility.RandomElement(new List<Chest> { rewards?.D_Chest, rewards?.C_Chest, rewards?.B_Chest, rewards?.A_Chest, rewards?.S_Chest }),
            _ => null,
        };
        var isModded = false;
        if(chest == null)
        {
            var val = ModdedChests.Keys.Where(x => x.Trim().ToLowerInvariant() == chesttype).FirstOrDefault();
            if(val != null)
            {
                ModdedChests.TryGetValue(val, out chest);
                if(chest != null)
                {
                    isModded = true;
                }
            }
        }
        var count = 1;
        if(args.Length > 2 && !int.TryParse(args[2], out count))
        {
            count = 1;
        }
        var specialAction = "";
        if((args.Length == 2 || (args.Length > 2 && !int.TryParse(args[2], out count))) && !int.TryParse(args[1], out count))
        {
            count = 1;
        }
        if(args.Length > 1)
        {
            specialAction = args[1];
        }
        for(int i = 0; i < count; i++)
        {
            if (chesttype == "mirror")
            {
                GameObject g = LoadHelper.LoadAssetFromAnywhere<GameObject>("Shrine_Mirror").GetComponent<DungeonPlaceableBehaviour>().InstantiateObject(currentRoom,
                    currentRoom.GetBestRewardLocation(new IntVector2(2, 3), Dungeonator.RoomHandler.RewardLocationStyle.PlayerCenter, true) - currentRoom.area.basePosition + IntVector2.Up * 2, true);
                currentRoom.RegisterInteractable(g.GetInterfaceInChildren<IPlayerInteractable>());
                if (isAlwaysGuns)
                {
                    Log("Warning: all_guns currently doesn't work with mirrors!");
                }
                if (isAlwaysItems)
                {
                    Log("Warning: all_items currently doesn't work with mirrors!");
                }
            }
            else if (chest != null)
            {
                var location = currentRoom.GetBestRewardLocation(new IntVector2(chesttype is "truth" ? 4 : 2, 1), Dungeonator.RoomHandler.RewardLocationStyle.PlayerCenter, true);
                var c = Chest.Spawn(chest, location + (chesttype is "truth" ? IntVector2.Right * 2 : IntVector2.Zero), currentRoom, specialAction.Trim().ToLowerInvariant() is not "mimic");
                if(c == null)
                {
                    continue;
                }
                if (specialAction.Trim().ToLowerInvariant() is not "locked")
                {
                    c.ForceUnlock();
                }
                if(specialAction.Trim().ToLowerInvariant() is "mimic")
                {
                    c.overrideMimicChance = 100;
                    if (!c.IsTruthChest && !c.IsRainbowChest && !c.lootTable.CompletesSynergy)
                    {
                        c.m_isMimic = false;
                        if (!string.IsNullOrEmpty(c.MimicGuid))
                        {
                            c.m_isMimic = true;
                            if (c.gameObject.activeInHierarchy)
                            {
                                c.StartCoroutine(c.MimicIdleAnimCR());
                            }
                        }
                    }
                }
                var isRainbow = chesttype is "hidden_rainbow" or "rainbow" or "rainbow_synergy";
                if(chesttype is "hidden_rainbow")
                {
                    c.ChestIdentifier = Chest.SpecialChestIdentifier.SECRET_RAINBOW;
                }
                if (isRainbow && chesttype is not "rainbow")
                {
                    c.BecomeRainbowChest();
                }
                if(chesttype is "glitched")
                {
                    c.BecomeGlitchChest();
                }
                if((!isRainbow && !isModded) || isAlwaysGuns || isAlwaysItems)
                {
                    if(c.lootTable != null)
                    {
                        if(isModded)
                        {
                            if (isAlwaysGuns)
                            {
                                Log("Warning: all_guns may break with modded chests!");
                            }
                            if (isAlwaysItems)
                            {
                                Log("Warning: all_items may break with modded chests!");
                            }
                        }
                        c.lootTable.lootTable = (isAlwaysGuns || UnityEngine.Random.value > 0.5f) && !isAlwaysItems ? rewards.GunsLootTable : rewards.ItemsLootTable;
                        if (c.lootTable.overrideItemLootTables != null && (isAlwaysItems || isAlwaysGuns))
                        {
                            for (int j = 0; j < c.lootTable.overrideItemLootTables.Count; j++)
                            {
                                c.lootTable.overrideItemLootTables[j] = c.lootTable.lootTable;
                            }
                        }
                    }
                }
                if(chesttype is "truth")
                {
                    var g = LoadHelper.LoadAssetFromAnywhere<GameObject>("NPC_Truth_Knower").GetComponent<TalkDoerLite>().InstantiateObject(currentRoom, location - currentRoom.area.basePosition);
                    currentRoom.RegisterInteractable(g.GetInterfaceInChildren<IPlayerInteractable>());
                }
                if(chesttype is "random")
                {
                    chest = BraveUtility.RandomElement(new List<Chest> { rewards?.D_Chest, rewards?.C_Chest, rewards?.B_Chest, rewards?.A_Chest, rewards?.S_Chest });
                }
            }
            else
            {
                Log($"Invalid chest type {chesttype}!");
                return;
            }
        }
    }

    public static TrackedStats? GetStatFromString(string statname)
    {
        TrackedStats stat;
        try
        {
            stat = (TrackedStats)Enum.Parse(typeof(TrackedStats), statname);
        }
        catch
        {
            return null;
        }
        return stat;
    }

    public static void StatGet(string[] args)
    {
        if (args.Length <= 0) { Log("TrackedStat not given."); return; }
        TrackedStats? stat = GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue)
        {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        if (GameManager.Instance.PrimaryPlayer != null)
        {
            float characterstat = GameStatsManager.Instance.GetCharacterStatValue(stat.Value);
            Log("Character: " + characterstat);
            float sessionstat = GameStatsManager.Instance.GetSessionStatValue(stat.Value);
            Log("Session: " + sessionstat);
        }
        float playerstat = GameStatsManager.Instance.GetPlayerStatValue(stat.Value);
        Log("This save file: " + playerstat);
    }

    public static void StatSetSession(string[] args)
    {
        if (!ArgCount(args, 2)) return;
        TrackedStats? stat = GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue)
        {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse(args[1], out value))
        {
            Log("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.SetStat(stat.Value, value);
    }

    public static void StatSetCurrentCharacter(string[] args)
    {
        if (!ArgCount(args, 2)) return;
        TrackedStats? stat = GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue)
        {
            Log("The stat isn't a real TrackedStat");
            return;
        }
        float value;
        if (!float.TryParse(args[1], out value))
        {
            Log("The value isn't a proper number");
            return;
        }
        PlayableCharacters currentCharacter = GameManager.Instance.PrimaryPlayer.characterIdentity;
        GameStatsManager.Instance.m_characterStats[currentCharacter].SetStat(stat.Value, value);
    }

    public static void StatSet(string[] args)
    {
        if (args.Length <= 0) { Log("TrackedStat not given."); return; }
        TrackedStats? stat = GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue)
        {
            Log("The value isn't a proper number");
            return;
        }
        if (!float.TryParse(args[1], out var value))
        {
            Log("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.GetPlayerStatValue(stat.Value);
        if(GameStatsManager.Instance.m_sessionStats != null)
        {
            GameStatsManager.Instance.m_sessionStats.SetStat(stat.Value, 0f);
        }
        foreach (var stats in GameStatsManager.Instance.m_characterStats)
        {
            stats.Value.SetStat(stat.Value, 0f);
        }
        GameStatsManager.Instance.m_characterStats[PlayableCharacters.Pilot].SetStat(stat.Value, value);
    }

    public static void StatMod(string[] args)
    {
        if (args.Length <= 0) { Log("TrackedStat not given."); return; }
        TrackedStats? stat = GetStatFromString(args[0].ToUpper());
        if (!stat.HasValue)
        {
            Log("The value isn't a proper number");
            return;
        }
        if (!float.TryParse(args[1], out var value))
        {
            Log("The value isn't a proper number");
            return;
        }
        GameStatsManager.Instance.RegisterStatChange(stat.Value, value);
    }

    public static void StatList(string[] args)
    {
        foreach (var value in Enum.GetValues(typeof(TrackedStats)))
        {
            Log(value.ToString().ToLower());
        }
    }

    public static void SwitchCharacter(string[] args)
    {
        if (args.Length < 1)
        {
            Log("Character not given!");
        }
        if (!Characters.TryGetValue(args[0], out string path))
        {
            path = args[0];
        }
        GameObject gameObject = (GameObject)BraveResources.Load("Player" + path, ".prefab");
        if (gameObject == null)
        {
            gameObject = (GameObject)Resources.Load("Player" + path);
        }
        if (gameObject == null)
        {
            Log($"Invalid character {args[0]}!");
            return;
        }
        Pixelator.Instance.FadeToBlack(0.5f, false, 0f);
        bool flag = false;
        if (GameManager.Instance.PrimaryPlayer)
        {
            flag = GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns;
        }
        GameManager.Instance.PrimaryPlayer.SetInputOverride("getting deleted");
        PlayerController primaryPlayer = GameManager.Instance.PrimaryPlayer;
        Vector3 position = primaryPlayer.transform.position;
        UnityEngine.Object.Destroy(primaryPlayer.gameObject);
        GameManager.Instance.ClearPrimaryPlayer();
        GameManager.PlayerPrefabForNewGame = gameObject;
        PlayerController component = GameManager.PlayerPrefabForNewGame.GetComponent<PlayerController>();
        GameStatsManager.Instance.BeginNewSession(component);
        GameObject gameObject2 = UnityEngine.Object.Instantiate(GameManager.PlayerPrefabForNewGame, position, Quaternion.identity);
        GameManager.PlayerPrefabForNewGame = null;
        gameObject2.SetActive(true);
        PlayerController component2 = gameObject2.GetComponent<PlayerController>();
        GameManager.Instance.PrimaryPlayer = component2;
        component2.PlayerIDX = 0;
        GameManager.Instance.MainCameraController.ClearPlayerCache();
        GameManager.Instance.MainCameraController.SetManualControl(false, true);
        if (GameManager.Instance.IsFoyer) Foyer.Instance.PlayerCharacterChanged(component2);
        if (Minimap.Instance?.UIMinimap?.dockItems != null) new List<Tuple<tk2dSprite, PassiveItem>>(Minimap.Instance.UIMinimap.dockItems).ForEach(x => Minimap.Instance.UIMinimap.RemovePassiveItemFromDock(x.Second));

        Pixelator.Instance.FadeToBlack(0.5f, true, 0f);
        if (flag)
        {
            GameManager.Instance.PrimaryPlayer.CharacterUsesRandomGuns = true;
            while (GameManager.Instance.PrimaryPlayer.inventory.AllGuns.Count > 1)
            {
                Gun gun = GameManager.Instance.PrimaryPlayer.inventory.AllGuns[1];
                GameManager.Instance.PrimaryPlayer.inventory.RemoveGunFromInventory(gun);
                UnityEngine.Object.Destroy(gun.gameObject);
            }
        }

        if (args.Length == 2)
        {
            component2.SwapToAlternateCostume(null);
        }
    }
}

