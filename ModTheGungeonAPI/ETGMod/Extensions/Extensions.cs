using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Globalization;

public static partial class ETGMod
{
    // ETGMod helper extension methods.
    /// <summary>
    /// Just like object.ToString(), but returns string.Empty if the object is null.
    /// </summary>
    /// <param name="o">The object to convert into string.</param>
    /// <returns>The result string.</returns>
    public static string ToStringSafe(this object o)
    {
        return o == null ? string.Empty : o is string str ? str : o.ToString();
    }

    /// <summary>
    /// Removes the characters ", \, -, and \n from the string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The string with then unacceptable characters removed.</returns>
    public static string RemoveUnacceptableCharactersForEnum(this string str)
    {
        if(str == null)
        {
            return null;
        }
        return str.Replace("\"", "").Replace("\\", "").Replace(" ", "").Replace("-", "_").Replace("\n", "");
    }

    /// <summary>
    /// Removes the characters ", \, and \n from the string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The string with then unacceptable characters removed.</returns>
    public static string RemoveUnacceptableCharactersForGUID(this string str)
    {
        if (str == null)
        {
            return null;
        }
        return str.Replace("\"", "").Replace("\\", "").Replace("\n", "");
    }

    /// <summary>
    /// Makes the string all lowercase, removes the characters \n, \, \", . and - and replaces spaces with underscores.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToID(this string str)
    {
        return str.ToLowerInvariant().Replace("\n", "").Replace("\\", "").Replace("\"", "").Replace(" ", "_").Replace(".", "").Replace("-", "");
    }

    private static readonly long _DoubleNegativeZero = BitConverter.DoubleToInt64Bits(-0D);
    public static bool IsNegativeZero(this double d)
    {
        return BitConverter.DoubleToInt64Bits(d) == _DoubleNegativeZero;
    }
    public static bool IsNegativeZero(this float f)
    {
        return BitConverter.DoubleToInt64Bits(f) == _DoubleNegativeZero;
    }

    public static int Count(this string @in, char c)
    {
        int count = 0;
        for (int i = 0; i < @in.Length; i++)
        {
            if (@in[i] == c) count++;
        }
        return count;
    }

    internal static bool ContainsWhitespace(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c))
                return true;
        }

        return false;
    }

    public static T GetFirst<T>(this IEnumerable<T> e)
    {
        foreach (T t in e)
        {
            return t;
        }
        return default(T);
    }

    public static string ToTitleCaseInvariant(this string s)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }

    public static string ToStringInvariant(this float o)
    {
        return o.ToString(CultureInfo.InvariantCulture);
    }

    public static string RemovePrefix(this string str, string prefix)
    {
        return str.StartsWithInvariant(prefix) ? str.Substring(prefix.Length) : str;
    }

    public static string RemoveSuffix(this string str, string suffix)
    {
        return str.StartsWithInvariant(suffix) ? str.Substring(0, suffix.Length - suffix.Length) : str;
    }
    public static int IndexOfInvariant(this string s, string a)
    {
        return s.IndexOf(a, StringComparison.InvariantCulture);
    }
    public static int IndexOfInvariant(this string s, string a, int i)
    {
        return s.IndexOf(a, i, StringComparison.InvariantCulture);
    }
    public static int LastIndexOfInvariant(this string s, string a)
    {
        return s.LastIndexOf(a, StringComparison.InvariantCulture);
    }
    public static int LastIndexOfInvariant(this string s, string a, int i)
    {
        return s.LastIndexOf(a, i, StringComparison.InvariantCulture);
    }
    public static bool StartsWithInvariant(this string s, string a)
    {
        return s.StartsWith(a, StringComparison.InvariantCulture);
    }
    public static bool EndsWithInvariant(this string s, string a)
    {
        return s.EndsWith(a, StringComparison.InvariantCulture);
    }

    public static string Combine(this IList<string> sa, string c)
    {
        StringBuilder s = new StringBuilder();
        for (int i = 0; i < sa.Count; i++)
        {
            s.Append(sa[i]);
            if (i < sa.Count - 1)
            {
                s.Append(c);
            }
        }
        return s.ToString();
    }

    public static string CombineReversed(this IList<string> sa, string c)
    {
        StringBuilder s = new StringBuilder();
        for (int i = sa.Count - 1; 0 <= i; i--)
        {
            s.Append(sa[i]);
            if (0 < i)
            {
                s.Append(c);
            }
        }
        return s.ToString();
    }

    public static Type GetValueType(this MemberInfo info)
    {
        if (info is FieldInfo)
        {
            return ((FieldInfo)info).FieldType;
        }
        if (info is PropertyInfo)
        {
            return ((PropertyInfo)info).PropertyType;
        }
        if (info is MethodInfo)
        {
            return ((MethodInfo)info).ReturnType;
        }
        return null;
    }

    public static T AtOr<T>(this T[] a, int i, T or)
    {
        if (i < 0 || a.Length <= i) return or;
        return a[i];
    }

    public static void AddRange(this IDictionary to, IDictionary from)
    {
        foreach (DictionaryEntry entry in from)
            to.Add(entry.Key, entry.Value);
    }

    public static void SetRange(this IDictionary to, IDictionary from)
    {
        foreach (DictionaryEntry entry in from)
            to[entry.Key] = entry.Value;
    }

    public static void ForEach<T>(this BindingList<T> list, Action<T> a)
    {
        for (int i = 0; i < list.Count; i++)
        {
            a(list[i]);
        }
    }
    public static void AddRange<T>(this BindingList<T> list, BindingList<T> other)
    {
        for (int i = 0; i < other.Count; i++)
        {
            list.Add(other[i]);
        }
    }

    public static int IndexOf(this object[] array, object elem)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == elem)
            {
                return i;
            }
        }
        return -1;
    }

    public static Texture2D Copy(this Texture2D texture, TextureFormat? format = TextureFormat.ARGB32) // an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s an3s
    {
        if (texture == null)
        {
            return null;
        }
        RenderTexture copyRT = RenderTexture.GetTemporary(
            texture.width, texture.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Default
        );

        Graphics.Blit(texture, copyRT);

        RenderTexture previousRT = RenderTexture.active;
        RenderTexture.active = copyRT;

        Texture2D copy = new Texture2D(texture.width, texture.height, format != null ? format.Value : texture.format, 1 < texture.mipmapCount);
        copy.name = texture.name;
        copy.ReadPixels(new Rect(0, 0, copyRT.width, copyRT.height), 0, 0);
        copy.Apply(true, false);

        RenderTexture.active = previousRT;
        RenderTexture.ReleaseTemporary(copyRT);

        return copy;
    }

    public static Texture2D GetRW(this Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }
        if (texture.IsReadable())
        {
            return texture;
        }
        return texture.Copy();
    }

    public static bool IsReadable(this Texture2D texture)
    {
        // return texture.GetRawTextureData().Length != 0; // spams log
        try
        {
            texture.GetPixels();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Type GetListType(this Type list)
    {
        if (list.IsArray)
        {
            return list.GetElementType();
        }

        Type[] ifaces = list.GetInterfaces();
        for (int i = 0; i < ifaces.Length; i++)
        {
            Type iface = ifaces[i];
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IList<>))
            {
                return list.GetGenericArguments()[0];
            }
        }

        return null;
    }

    public static string GetPath(this Transform t)
    {
        List<string> path = new List<string>();
        do
        {
            path.Add(t.name);
        } while ((t = t.parent) != null);
        return path.CombineReversed("/");
    }

    public static GameObject AddChild(this GameObject go, string name, params Type[] components)
    {
        GameObject child = new GameObject(name, components);
        child.transform.SetParent(go.transform);
        child.transform.SetAsLastSibling();
        return child;
    }

    public static tk2dBaseSprite GetAnySprite(this BraveBehaviour b)
    {
        return
            b.GetComponent<tk2dBaseSprite>() ??
            b.transform.GetComponentInChildren<tk2dBaseSprite>() ??
            b.transform.GetComponentInParent<tk2dBaseSprite>();
    }

    public static Coroutine StartGlobal(this IEnumerator c)
    {
        if (c == null) return null;
        return StartGlobalCoroutine(c);
    }
    public static void StopGlobal(this Coroutine c)
    {
        if (c == null) return;
        StopGlobalCoroutine(c);
    }

}