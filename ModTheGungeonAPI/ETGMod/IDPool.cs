using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A list of values that can be found by their given ids.
/// </summary>
/// <typeparam name="T">The type of values in this IDPool.</typeparam>
public class IDPool<T> {
    private readonly Dictionary<string, T> _Storage = new();
    private readonly HashSet<string> _Namespaces = new();

    /// <summary>
    /// Gets or sets the object at the given id.
    /// </summary>
    /// <param name="id">The string id to get or set the object at.</param>
    /// <returns></returns>
    public T this[string id] {
        set {
            Set(Resolve(id), value);
        }
        get {
            return Get(id);
        }
    }

    /// <summary>
    /// Number of objects in this IDPool.
    /// </summary>
    public int Count {
        get {
            return _Storage.Count;
        }
    }

    public class NonExistantIDException : Exception {
        public NonExistantIDException(string id) : base($"Object with ID {id} doesn't exist") { }
    }

    public class BadIDElementException : Exception {
        public BadIDElementException(string name) : base($"The ID's {name} can not contain any colons or whitespace") { }
    }

    public class ItemIDExistsException : Exception {
        public ItemIDExistsException(string id) : base($"Item {id} already exists") { }
    }

    public class BadlyFormattedIDException : Exception {
        public BadlyFormattedIDException(string id) : base($"ID was improperly formatted: {id}") { }
    }

    private void Set(string id, T obj) {
        id = Resolve(id);
        VerifyID(id);
        var entry = Split(id);
        if (id.Any(char.IsWhiteSpace)) throw new BadIDElementException("name");
        _Storage[id] = obj;
        if (!_Namespaces.Contains(entry.Namespace)) {
            _Namespaces.Add(entry.Namespace);
        }
    }

    /// <summary>
    /// Adds a new item to this IDPool.
    /// </summary>
    /// <param name="id">The id of the item that will get added.</param>
    /// <param name="obj">The new item that will be added.</param>
    /// <exception cref="ItemIDExistsException">Thrown if an item with the given id already exists in this IDPool.</exception>
    public void Add(string id, T obj) {
        id = Resolve(id);
        VerifyID(id);
        if (_Storage.ContainsKey(id)) throw new ItemIDExistsException(id);
        Set(id, obj);
    }

    /// <summary>
    /// Gets the item with the given id.
    /// </summary>
    /// <param name="id">The id to search for.</param>
    /// <returns>The found item.</returns>
    /// <exception cref="NonExistantIDException"></exception>
    public T Get(string id) {
        id = Resolve(id);
        if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
        return _Storage[id];
    }

    /// <summary>
    /// Gets the item with the given id. Unlike Get(string), this will not throw an exception when the item is not found and will instead return the default value.
    /// </summary>
    /// <param name="id">The id to search for.</param>
    /// <returns>The found item or default value if nothing was found.</returns>
    public T GetSafe(string id)
    {
        try
        {
            return Get(id);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Removes an item with the given id from this IDPool.
    /// </summary>
    /// <param name="id">The id to search for.</param>
    /// <param name="destroy">If true, the removed item will also be destroyed if it's a unity object.</param>
    /// <exception cref="NonExistantIDException"></exception>
    public void Remove(string id, bool destroy = false) {
        id = Resolve(id);
        var split = Split(id);
        if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
        if (_Storage[id] is UnityEngine.Object && destroy) UnityEngine.Object.Destroy(_Storage[id] as UnityEngine.Object);
        _Storage.Remove(id);
    }

    /// <summary>
    /// Changes the id of an item to the given id.
    /// </summary>
    /// <param name="source">The original id for the item.</param>
    /// <param name="target">The new id for the item.</param>
    /// <exception cref="NonExistantIDException"></exception>
    public void Rename(string source, string target) {
        source = Resolve(source);
        target = Resolve(target);
        var target_entry = Split(target);
        if (!_Storage.ContainsKey(source)) throw new NonExistantIDException(source);

        var obj = _Storage[source];
        _Storage.Remove(source);
        _Storage[target] = obj;
    }

    /// <summary>
    /// Throws an exception if the given id contains more than 1 ":" character.
    /// </summary>
    /// <param name="id">The id to verify.</param>
    /// <exception cref="BadlyFormattedIDException"></exception>
    public static void VerifyID(string id) {
        if (id.Count(':') > 1) throw new BadlyFormattedIDException(id);
    }

    /// <summary>
    /// If the given id contains a ":" character, verifies the id. Otherwise, returns gungeon:id
    /// </summary>
    /// <param name="id">The id to resolve.</param>
    /// <returns>The modified id.</returns>
    public static string Resolve(string id) {
        id = id.Trim();
        if (id.Contains(":")) {
            VerifyID(id);
            return id;
        } else {
            return $"gungeon:{id}";
        }
    }

    /// <summary>
    /// Represents a pair of name and namespace.
    /// </summary>
    public struct Entry {
        public string Namespace;
        public string Name;

        /// <summary>
        /// Creates a new Entry from the given name and namespace.
        /// </summary>
        /// <param name="namesp">The namespace for this Entry.</param>
        /// <param name="name">The name for this entry.</param>
        public Entry(string namesp, string name) {
            Namespace = namesp;
            Name = name;
        }
    }

    /// <summary>
    /// Splits an id into a name and a namespace and turns it into an Entry.
    /// </summary>
    /// <param name="id">The id to split.</param>
    /// <returns>The created Entry.</returns>
    /// <exception cref="BadlyFormattedIDException"></exception>
    public static Entry Split(string id) {
        VerifyID(id);
        string[] split = id.Split(':');
        if (split.Length != 2) throw new BadlyFormattedIDException(id);
        return new Entry(split[0], split[1]);
    }

    /// <summary>
    /// Returns true if this IDPool contains the given id, false otherwise.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>True if this IDPool contains the given id, false otherwise.</returns>
    public bool ContainsID(string id) {
        return _Storage.ContainsKey(Resolve(id));
    }

    /// <summary>
    /// Returns an array of all ids in this IDPool.
    /// </summary>
    public string[] AllIDs {
        get {
            return _Storage.Keys.ToArray();
        }
    }

    /// <summary>
    /// Returns an IEnumerable containing all of the items in this IDPool.
    /// </summary>
    public IEnumerable<T> Entries {
        get {
            foreach (var v in _Storage.Values) {
                yield return v;
            }
        }
    }

    /// <summary>
    /// Returns an IEnumerable containing all of the ids in this IDPool.
    /// </summary>
    public IEnumerable<string> IDs {
        get {
            foreach (var k in _Storage.Keys) {
                yield return k;
            }
        }
    }

    /// <summary>
    /// Returns an IEnumerable containing all of the pairs of ids and items in this IDPool.
    /// </summary>
    public IEnumerable<KeyValuePair<string, T>> Pairs {
        get {
            foreach (var kv in _Storage) {
                yield return new KeyValuePair<string, T>(kv.Key, kv.Value);
            }
        }
    }
}
