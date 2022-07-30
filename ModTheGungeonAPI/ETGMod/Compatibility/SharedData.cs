using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Class used for cross-mod interactions and enum extensions.
/// </summary>
public static partial class ETGModCompatibility
{
    private static readonly Dictionary<string, Dictionary<string, object>> sharedData = new();

    /// <summary>
    /// Sets a mod's shared data with the given name to the given value.
    /// </summary>
    /// <param name="guid">The guid of the mod that owns the shared data.</param>
    /// <param name="name">The name of the shared data to set.</param>
    /// <param name="value">The new value for the shared data.</param>
    public static void SetSharedData(string guid, string name, object value)
    {
        if(!sharedData.TryGetValue(guid, out var pluginData))
        {
            pluginData = new();
            sharedData.Add(guid, pluginData);
        }
        if (pluginData.ContainsKey(name))
        {
            pluginData[name] = value;
        }
        else
        {
            pluginData.Add(name, value);
        }
    }

    /// <summary>
    /// Tries to get a mod's shared data and reports if the result was successful.
    /// </summary>
    /// <typeparam name="T">The type of generic data to get.</typeparam>
    /// <param name="guid">The guid of the mod that owns the shared data.</param>
    /// <param name="name">The name of the shared data to get.</param>
    /// <param name="value">The value of the shared data to output.</param>
    /// <returns>True if the shared data exists and is of the type T, false otherwise.</returns>
    public static bool TryGetSharedData<T>(string guid, string name, out T value)
    {
        var success = TryGetSharedData(guid, name, out var oValue);
        if (!success)
        {
            value = default;
            return false;
        }
        if(oValue is T val)
        {
            value = val;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Tries to get a mod's shared data and reports if the result was successful.
    /// </summary>
    /// <param name="guid">The guid of the mod that owns the shared data.</param>
    /// <param name="name">The name of the shared data to get.</param>
    /// <param name="value">The value of the shared data to output.</param>
    /// <returns>True if the shared data exists, false otherwise.</returns>
    public static bool TryGetSharedData(string guid, string name, out object value)
    {
        if(sharedData.TryGetValue(guid, out var pluginData))
        {
            if(pluginData.TryGetValue(name, out value))
            {
                return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Gets a mod's shared data with the given name.
    /// </summary>
    /// <typeparam name="T">The type of generic data to get.</typeparam>
    /// <param name="guid">The guid of the mod that owns the shared data.</param>
    /// <param name="name">The name of the shared data to get.</param>
    /// <returns>The shared data's value if it exists and is of type T, default value otherwise.</returns>
    public static T GetSharedData<T>(string guid, string name)
    {
        TryGetSharedData<T>(guid, name, out var val);
        return val;
    }

    /// <summary>
    /// Gets a mod's shared data with the given name.
    /// </summary>
    /// <param name="guid">The guid of the mod that owns the shared data.</param>
    /// <param name="name">The name of the shared data to get.</param>
    /// <returns>The shared data's value if it exists, null otherwise.</returns>
    public static object GetSharedData(string guid, string name)
    {
        TryGetSharedData(guid, name, out var val);
        return val;
    }
}
