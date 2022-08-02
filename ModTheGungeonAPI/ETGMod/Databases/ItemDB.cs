using System;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using Dungeonator;

[HarmonyPatch]
public class ItemDB
{
    /// <summary>
    /// Finds an item that has the given id.
    /// </summary>
    /// <param name="id">The item id to search for.</param>
    /// <returns>The found item or null if nothing is found.</returns>
    public PickupObject this[int id]
    {
        get
        {
            return PickupObjectDatabase.Instance.InternalGetById(id);
        }
    }

    /// <summary>
    /// Finds an item that has the given internal name.
    /// </summary>
    /// <param name="name">The internal name to search for.</param>
    /// <returns>The found item or null if nothing is found.</returns>
    public PickupObject this[string name]
    {
        get
        {
            return PickupObjectDatabase.Instance.InternalGetByName(name);
        }
    }

    /// <summary>
    /// Returns the number of items in the game.
    /// </summary>
    public int Count
    {
        get
        {
            return PickupObjectDatabase.Instance.Objects.Count;
        }
    }

    /// <summary>
    /// The list of all modded items created.
    /// </summary>
    public List<PickupObject> ModItems = new();
    /// <summary>
    /// The dictionary where the keys are the internal names for the modded items and the values are the modded items with those names.
    /// </summary>
    public Dictionary<string, PickupObject> ModItemMap = new();
    /// <summary>
    /// The dictionary where the keys are the names of a floor and the values are the items that will be added to the fallback item table for those floors.
    /// </summary>
    public Dictionary<string, List<WeightedGameObject>> ModLootPerFloor = new();

    /// <summary>
    /// Sprite collection used by guns.
    /// </summary>
    // wow thanks for the explanation zatherz very helpful
    public tk2dSpriteCollectionData WeaponCollection = ETGMod.Assets.Collections.Find(x => x.spriteCollectionName == "WeaponCollection");
    /// <summary>
    /// Sprite collection used by some other guns.
    /// </summary>
    public tk2dSpriteCollectionData WeaponCollection02 = ETGMod.Assets.Collections.Find(x => x.spriteCollectionName == "WeaponCollection02");
    /// <summary>
    /// Sprite collection used by projectiles.
    /// </summary>
    public tk2dSpriteCollectionData ProjectileCollection = ETGMod.Assets.Collections.Find(x => x.spriteCollectionName == "ProjectileCollection");
    /// <summary>
    /// Sprite collection used by items.
    /// </summary>
    public tk2dSpriteCollectionData ItemCollection = ETGMod.Assets.Collections.Find(x => x.spriteCollectionName == "ItemCollection");

    /// <summary>
    /// Adds an item to the list of items in the game.
    /// </summary>
    /// <param name="value">The item to add.</param>
    /// <param name="dontDestroyOnLoad">If true, the item won't be destroyed when a scene is loaded. Only use for fake prefab items.</param>
    /// <param name="floor">The floor to the fallback loot table of which the gun will be added.</param>
    /// <returns>The id of the added item.</returns>
    public int AddSpecific(PickupObject value, bool dontDestroyOnLoad = false, string floor = "ANY")
    {
        int id = PickupObjectDatabase.Instance.Objects.Count;
        PickupObjectDatabase.Instance.Objects.Add(value);
        ModItems.Add(value);
        if (value != null)
        {
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(value.gameObject);
            }
            ModItemMap[value.name] = value;
            value.PickupObjectId = id;
            value.encounterTrackable.EncounterGuid = value.encounterTrackable.EncounterGuid.RemoveUnacceptableCharactersForGUID();
            EncounterDatabaseEntry edbEntry = new(value.encounterTrackable);
            edbEntry.ProxyEncounterGuid =
            edbEntry.myGuid = value.encounterTrackable.EncounterGuid;
            edbEntry.path = "Assets/Resources/ITEMDB:" + value.name + ".prefab";
            EncounterDatabase.Instance.Entries.Add(edbEntry);


            WeightedGameObject lootGameObject = new()
            {
                weight = 1f,
                additionalPrerequisites = new DungeonPrerequisite[0]
            };
            lootGameObject.SetGameObject(value.gameObject);
            if (value is Gun)
            {
                GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.Add(lootGameObject);
            }
            else
            {
                GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.Add(lootGameObject);
            }
            if (!ModLootPerFloor.TryGetValue(floor, out var loot))
            {
                loot = new List<WeightedGameObject>();
            }
            loot.Add(lootGameObject);
            ModLootPerFloor[floor] = loot;
        }
        return id;
    }

    /// <summary>
    /// Adds an item to the list of items in the game.
    /// </summary>
    /// <param name="value">The item to add.</param>
    /// <param name="updateSpriteCollections">Does nothing, only exists for backwards compatibility.</param>
    /// <param name="floor">The floor to the fallback loot table of which the gun will be added.</param>
    /// <returns>The id of the added item.</returns>
    public int Add(PickupObject value, bool updateSpriteCollections = false, string floor = "ANY")
    {
        return AddSpecific(value, true, floor);
    }

    /// <summary>
    /// Adds a gun to the list of items in the game.
    /// </summary>
    /// <param name="value">The gun to add.</param>
    /// <param name="collection">Does nothing, only exists for backwards compatibility.</param>
    /// <param name="floor">The floor to the fallback loot table of which the gun will be added.</param>
    /// <returns>The id of the added gun.</returns>
    [Obsolete("Add(Gun, tk2dSpriteCollection, string) is deprecated, use Add(PickupObject, bool, string)")]
    public int Add(Gun value, tk2dSpriteCollectionData collection = null, string floor = "ANY")
    {
        return Add(value, false, floor);
    }

    [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.Start))]
    [HarmonyPrefix]
    private static void DungeonStart()
    {
        if(ETGMod.Databases.Items?.ModLootPerFloor != null && GameManager.HasInstance && GameManager.Instance.Dungeon != null && GameManager.Instance.Dungeon.baseChestContents != null &&
            GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops != null && GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops.elements != null)
        {
            if (ETGMod.Databases.Items.ModLootPerFloor.TryGetValue("ANY", out var loot))
            {
                GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops.elements.AddRange(loot);
            }
            string floorNameKey = GameManager.Instance.Dungeon.DungeonFloorName;
            if (!string.IsNullOrEmpty(floorNameKey))
            {
                string floorName = null;
                var startIndex = 0;
                if (floorNameKey.StartsWith("#"))
                {
                    startIndex = 1;
                }
                if (floorNameKey.Contains("_") && floorNameKey.IndexOf('_') - startIndex > 0)
                {
                    floorName = floorNameKey.Substring(startIndex, floorNameKey.IndexOf('_') - startIndex);
                }
                else if (floorNameKey.Length - startIndex > 0)
                {
                    floorName = floorNameKey.Substring(startIndex);
                }
                if (!string.IsNullOrEmpty(floorName) && ETGMod.Databases.Items.ModLootPerFloor.TryGetValue(floorName, out loot))
                {
                    GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops.elements.AddRange(loot);
                }
            }
        }
    }

    private Gun _GunGivenPrototype;

    /// <summary>
    /// Creates a new gun with a peashooter base.
    /// </summary>
    /// <param name="gunName">The ingame name for the new gun.</param>
    /// <param name="gunNameShort">The internal name for the new gun.</param>
    /// <returns>The created gun.</returns>
    public Gun NewGun(string gunName, string gunNameShort = null)
    {
        if (_GunGivenPrototype == null)
        {
            _GunGivenPrototype = (Gun)ETGMod.Databases.Items["Pea_Shooter"];
            ProjectileCollection = _GunGivenPrototype.DefaultModule.projectiles[0].GetComponentInChildren<tk2dBaseSprite>().Collection;
        }

        return NewGun(gunName, _GunGivenPrototype, gunNameShort);
    }

    /// <summary>
    /// Creates a new gun with the given base.
    /// </summary>
    /// <param name="gunName">The ingame name for the new gun.</param>
    /// <param name="baseGun">The base gun to build the new gun from.</param>
    /// <param name="gunNameShort">The internal name for the new gun.</param>
    /// <returns>The created gun.</returns>
    public Gun NewGun(string gunName, Gun baseGun, string gunNameShort = null)
    {
        if (gunNameShort == null)
        {
            gunNameShort = gunName.Replace(' ', '_');
        }

        GameObject go = UnityEngine.Object.Instantiate(baseGun.gameObject);
        go.name = gunNameShort;

        Gun gun = go.GetComponent<Gun>();
        SetupItem(gun, gunName);
        gun.gunName = gunName;
        gun.gunSwitchGroup = gunNameShort;

        gun.modifiedVolley = null;
        gun.singleModule = null;

        gun.RawSourceVolley = ScriptableObject.CreateInstance<ProjectileVolleyData>();
        gun.Volley.projectiles = new List<ProjectileModule>();

        gun.SetBaseMaxAmmo(300);
        gun.reloadTime = 0.625f;

        Gungeon.Game.Items.Add($"outdated_gun_mods:{gunName.ToID()}", gun);

        return gun;
    }


    /// <summary>
    /// Sets up an item, adding an encounter trackable to it and setting its name to the given one.
    /// </summary>
    /// <param name="item">The item to setup.</param>
    /// <param name="name">The name for the item.</param>
    public void SetupItem(PickupObject item, string name)
    {
        if (item.encounterTrackable == null) item.encounterTrackable = item.gameObject.AddComponent<EncounterTrackable>();
        if (item.encounterTrackable.journalData == null) item.encounterTrackable.journalData = new JournalEntry();

        item.encounterTrackable.EncounterGuid = item.name;

        item.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
        item.encounterTrackable.journalData.SuppressKnownState = false;

        string keyName = "#" + item.name.Replace(" ", "").ToUpperInvariant();
        item.encounterTrackable.journalData.PrimaryDisplayName = keyName + "_ENCNAME";
        item.encounterTrackable.journalData.NotificationPanelDescription = keyName + "_SHORTDESC";
        item.encounterTrackable.journalData.AmmonomiconFullEntry = keyName + "_LONGDESC";
        item.encounterTrackable.journalData.AmmonomiconSprite = item.name.Replace(' ', '_') + "_idle_001";
        item.SetName(name);
    }

    /// <summary>
    /// Finds a modded item that has the given internal name.
    /// </summary>
    /// <param name="name">The internal name to search for.</param>
    /// <returns>The found item or null if nothing is found.</returns>
    public PickupObject GetModItemByName(string name)
    {
        if (ModItemMap.TryGetValue(name, out var item))
        {
            return item;
        }
        return null;
    }

}