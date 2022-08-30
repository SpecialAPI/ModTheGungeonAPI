using System;
using System.Collections.Generic;
using HarmonyLib;

[HarmonyPatch]
public class StringDB
{
    /// <summary>
    /// Current game language.
    /// </summary>
    public StringTableManager.GungeonSupportedLanguages CurrentLanguage
    {
        get
        {
            return GameManager.Options.CurrentLanguage;
        }
        set
        {
            StringTableManager.SetNewLanguage(value, true);
        }
    }

    /// <summary>
    /// The string table that has miscellaneous strings that don't fit into other tables.
    /// </summary>
    public readonly StringDBTable Core = new(() => StringTableManager.CoreTable);
    /// <summary>
    /// The string table that has all of the item-related strings: item names, short descriptions, long descrpitions, etc.
    /// </summary>
    public readonly StringDBTable Items = new(() => StringTableManager.ItemTable);
    /// <summary>
    /// The string table that has all of the enemy-related strings: enemy names, short descriptions, long descriptions, etc.
    /// </summary>
    public readonly StringDBTable Enemies = new(() => StringTableManager.EnemyTable);
    /// <summary>
    /// The string table that has all of the text in the intro.
    /// </summary>
    public readonly StringDBTable Intro = new(() => StringTableManager.IntroTable);
    /// <summary>
    /// The string table that has ui text. Unused in the base game.
    /// </summary>
    public readonly StringDBTable UI = new(() => UITable);
    /// <summary>
    /// The string table that has all of the synergy names.
    /// </summary>
    public readonly StringDBTable Synergy = new(() => SynergyTable);

    /// <summary>
    /// 
    /// </summary>
    public Action<StringTableManager.GungeonSupportedLanguages> OnLanguageChanged;

    [HarmonyPatch(typeof(StringTableManager), nameof(StringTableManager.SetNewLanguage))]
    [HarmonyPostfix]
    private static void LanguageChanged()
    {
        ETGMod.Databases.Strings.Core.LanguageChanged();
        ETGMod.Databases.Strings.Items.LanguageChanged();
        ETGMod.Databases.Strings.Enemies.LanguageChanged();
        ETGMod.Databases.Strings.Intro.LanguageChanged();
        ETGMod.Databases.Strings.OnLanguageChanged?.Invoke(ETGMod.Databases.Strings.CurrentLanguage);
    }

    public static Dictionary<string, StringTableManager.StringCollection> UITable
    {
        get
        {
            if (StringTableManager.m_uiTable == null)
            {
                StringTableManager.m_uiTable = StringTableManager.LoadUITable(StringTableManager.m_currentSubDirectory);
            }
            if (StringTableManager.m_backupUiTable == null)
            {
                StringTableManager.m_backupUiTable = StringTableManager.LoadUITable("english_items");
            }
            return StringTableManager.m_uiTable;
        }
    }

    public static Dictionary<string, StringTableManager.StringCollection> SynergyTable
    {
        get
        {
            if (StringTableManager.m_synergyTable == null)
            {
                StringTableManager.m_synergyTable = StringTableManager.LoadSynergyTable(StringTableManager.m_currentSubDirectory);
            }
            if (StringTableManager.m_backupSynergyTable == null)
            {
                StringTableManager.m_backupSynergyTable = StringTableManager.LoadItemsTable("english_items");
            }
            return StringTableManager.m_synergyTable;
        }
    }
}

/// <summary>
/// Represents a string table in StringTableManager.
/// </summary>
public sealed class StringDBTable
{
    private readonly Func<Dictionary<string, StringTableManager.StringCollection>> _getTable;
    private readonly Dictionary<string, StringTableManager.StringCollection> _changes;
    private Dictionary<string, StringTableManager.StringCollection> _cachedTable;

    /// <summary>
    /// Creates a new StringTableManager from a table getter Func.
    /// </summary>
    /// <param name="getTable">The Func that gets the current table.</param>
    public StringDBTable(Func<Dictionary<string, StringTableManager.StringCollection>> getTable)
    {
        _getTable = getTable;
        _changes = new Dictionary<string, StringTableManager.StringCollection>();
    }

    /// <summary>
    /// The current table dictionary.
    /// </summary>
    public Dictionary<string, StringTableManager.StringCollection> Table => _cachedTable ??= _getTable();

    /// <summary>
    /// Sets or gets a string collection from this table.
    /// </summary>
    /// <param name="key">The key to the string collection.</param>
    /// <returns>The string table found.</returns>
    public StringTableManager.StringCollection this[string key]
    {
        get => Table[key];
        set
        {
            Table[key] = value;
            _changes[key] = value;
            JournalEntry.ReloadDataSemaphore++;
        }
    }

    /// <summary>
    /// Returns true if the table contains a collection with the given key, false otherwise.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>True if the table contains a collection with the given key, false otherwise.</returns>
    public bool ContainsKey(string key) => Table.ContainsKey(key);

    /// <summary>
    /// Sets a string with the given key to the given value.
    /// </summary>
    /// <param name="key">The key to the string.</param>
    /// <param name="value">The new value for the string.</param>
    public void Set(string key, string value)
    {
        var collection = new StringTableManager.SimpleStringCollection();
        collection.AddString(value, 1f);
        this[key] = collection;
    }

    /// <summary>
    /// Reloads the table and reapplies all of the changes made.
    /// </summary>
    public void LanguageChanged()
    {
        _cachedTable = null;
        Dictionary<string, StringTableManager.StringCollection> table = Table;

        foreach (var kvp in _changes)
        {
            table[kvp.Key] = kvp.Value;
        }
    }
}