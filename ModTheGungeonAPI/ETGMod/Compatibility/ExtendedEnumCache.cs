using FullSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[fsObject]
internal class ExtendedEnumCache
{
    private static ExtendedEnumCache _instance;

    public static ExtendedEnumCache Instance => _instance ??= Load();

    public static ExtendedEnumCache Load()
    {
        SaveManager.Init();
        if (!SaveManager.Load(EnumCacheSave, out _instance, true, 0u, null, null))
            _instance = new ExtendedEnumCache();

        return _instance;
    }

    public static void Save()
    {
        SaveManager.Init();
        try
        {
            SaveManager.Save(Instance, EnumCacheSave, GameStatsManager.Instance != null ? GameStatsManager.Instance.PlaytimeMin : 0, 0u, null);
        }
        catch(Exception ex)
        {
            Debug.LogError("Failed saving enum cache: " + ex);
        }
    }

    [fsProperty]
    public List<GungeonFlags> extendedFlags = new();
    [fsProperty]
    public List<KeyValuePair<GlobalDungeonData.ValidTilesets, string>> extendedValidTilesets = new();
    [fsProperty]
    public Dictionary<PlayableCharacters, List<KeyValuePair<TrackedStats, float>>> extendedPlayersTrackedStats = new();
    [fsProperty]
    public Dictionary<PlayableCharacters, List<KeyValuePair<TrackedMaximums, float>>> extendedPlayersTrackedMaximums = new();
    [fsProperty]
    public Dictionary<PlayableCharacters, List<CharacterSpecificGungeonFlags>> extendedPlayersCharacterFlags = new();

    public static SaveManager.SaveType EnumCacheSave = new()
    {
        filePattern = "Slot{0}.enumCache",
        backupCount = 3,
        backupPattern = "Slot{0}.enumCacheBackup.{1}",
        backupMinTimeMin = 45,
    };
}
