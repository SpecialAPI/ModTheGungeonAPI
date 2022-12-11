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
    /// The string table that has all of the synergy names.
    /// </summary>
    public readonly StringDBTable Synergy = new(() => SynergyTable);
    /// <summary>
    /// The string table that has all of the UI text.
    /// </summary>
    public readonly UIStringDBTable UI = new();

    /// <summary>
    /// Runs when the game's language is changed.
    /// </summary>
    public Action<StringTableManager.GungeonSupportedLanguages> OnLanguageChanged;

    /// <summary>
    /// Runs for each UI language manager when the game's language is changed or when a UI language manager is created.
    /// </summary>
    public Action<dfLanguageManager, dfLanguageCode> OnUILanguageChanged;

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

    [HarmonyPatch(typeof(dfLanguageManager), nameof(dfLanguageManager.LoadLanguage))]
    [HarmonyPostfix]
    private static void UILanguageChanged(dfLanguageManager __instance, dfLanguageCode language, bool forceReload)
    {
        ETGMod.Databases.Strings.UI.LanguageChanged(__instance);
        ETGMod.Databases.Strings.OnUILanguageChanged?.Invoke(__instance, language);
        if (forceReload)
        {
            dfControl[] controls = __instance.GetComponentsInChildren<dfControl>();
            for (int i = 0; i < controls.Length; i++)
            {
                controls[i].Localize();
            }
            for (int j = 0; j < controls.Length; j++)
            {
                controls[j].PerformLayout();
                controls[j].LanguageChanged?.Invoke(controls[j]);
            }
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
                StringTableManager.m_backupSynergyTable = StringTableManager.LoadSynergyTable("english_items");
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
        _changes = new();
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
    /// Sets a string with the given key to the given values that all have the weight 1.
    /// </summary>
    /// <param name="key">The key to the string.</param>
    /// <param name="values">The new values for the string that all have the weight 1.</param>
    public void SetComplex(string key, params string[] values)
    {
        var collection = new StringTableManager.ComplexStringCollection();
        values.Do(x => collection.AddString(x, 1f));
        this[key] = collection;
    }

    /// <summary>
    /// Sets a string with the given key to the given values.
    /// </summary>
    /// <param name="key">The key to the string.</param>
    /// <param name="values">The new values and weights for the string where the strings are values and floats are weights.</param>
    public void SetComplex(string key, params Tuple<string, float>[] values)
    {
        var collection = new StringTableManager.ComplexStringCollection();
        values.Do(x => collection.AddString(x.First, x.Second));
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

public sealed class UIStringDBTable
{
    private readonly Dictionary<string, string> _changes = new();

    /// <summary>
    /// Reapplies all of the changes made to the given manager.
    /// </summary>
    /// <param name="man">The manager to reapply the changes to.</param>
    public void LanguageChanged(dfLanguageManager man)
    {
        foreach(var kvp in _changes)
        {
            man.strings[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Sets a string with the given key to the given value.
    /// </summary>
    /// <param name="key">The key to the string.</param>
    /// <param name="value">The new value for the string.</param>
    public void Set(string key, string value)
    {
        if(dfGUIManager.activeInstances != null)
        {
            foreach (var man in dfGUIManager.ActiveManagers)
            {
                if (man != null)
                {
                    var lang = man.GetComponent<dfLanguageManager>();
                    if (lang != null)
                    {
                        lang.strings[key] = value;
                    }
                }
            }
        }
        _changes[key] = value;
    }
}