using System;
using System.Collections.Generic;

public sealed class StringDB
{

    internal StringDB() { }

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

    public readonly StringDBTable Core = new StringDBTable(() => StringTableManager.CoreTable);
    public readonly StringDBTable Items = new StringDBTable(() => StringTableManager.ItemTable);
    public readonly StringDBTable Enemies = new StringDBTable(() => StringTableManager.EnemyTable);
    public readonly StringDBTable Intro = new StringDBTable(() => StringTableManager.IntroTable);

    public Action<StringTableManager.GungeonSupportedLanguages> OnLanguageChanged;

    public void LanguageChanged()
    {
        Core.LanguageChanged();
        Items.LanguageChanged();
        Enemies.LanguageChanged();
        Intro.LanguageChanged();
        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

}

public sealed class StringDBTable
{

    private readonly Func<Dictionary<string, StringTableManager.StringCollection>> _getTable;
    private readonly Dictionary<string, StringTableManager.StringCollection> _changes;
    private Dictionary<string, StringTableManager.StringCollection> _cachedTable;

    public StringDBTable(Func<Dictionary<string, StringTableManager.StringCollection>> getTable)
    {
        _getTable = getTable;
        _changes = new Dictionary<string, StringTableManager.StringCollection>();
    }

    public Dictionary<string, StringTableManager.StringCollection> Table
    {
        get
        {
            return _cachedTable ?? (_cachedTable = _getTable());
        }
    }

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

    public bool ContainsKey(string key) => Table.ContainsKey(key);

    public void Set(string key, string value)
    {
        var collection = new StringTableManager.SimpleStringCollection();
        collection.AddString(value, 1f);
        this[key] = collection;
    }

    public string Get(string key) => StringTableManager.GetString(key);

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