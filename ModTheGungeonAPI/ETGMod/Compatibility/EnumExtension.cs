using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Text;
using System.Globalization;
using FullSerializer.Internal;
using FullSerializer;
using UnityEngine;

[HarmonyPatch]
public static partial class ETGModCompatibility
{
    private readonly static Dictionary<Type, Dictionary<GuidInfo, int>> extendedEnums = new();
	private static bool enumsChanged = true;

	/// <summary>
	/// Extends a given enum and returns the result.
	/// </summary>
	/// <typeparam name="T">The enum to extend.</typeparam>
	/// <param name="guid">The guid of the mod that extends the enum.</param>
	/// <param name="name">The name of the new enum value.</param>
	/// <returns>The extended enum value.</returns>
    public static T ExtendEnum<T>(string guid, string name) where T : Enum
    {
		return (T)ExtendEnum(guid, name, typeof(T));
    }

	/// <summary>
	/// Extends a given enum and returns the result.
	/// </summary>
	/// <param name="guid">The guid of the mod that extends the enum.</param>
	/// <param name="name">The name of the new enum value.</param>
	/// <param name="t">The type of the enum to extend.</param>
	/// <returns>The extended enum value.</returns>
	public static object ExtendEnum(string guid, string name, Type t)
	{
        if (!t.IsEnum)
        {
			return 0;
        }
		ETGModMainBehaviour.EnsureHarmonyInitialized();
		Dictionary<GuidInfo, int> valuesForEnum;
		if (!extendedEnums.ContainsKey(t))
		{
			valuesForEnum = new();
			extendedEnums.Add(t, valuesForEnum);
		}
		else
		{
			valuesForEnum = extendedEnums[t];
		}
		GuidInfo ginfo = new(guid, name);
		var values = valuesForEnum.Where(x => x.Key.guid == ginfo.guid && x.Key.info == ginfo.info);
		KeyValuePair<GuidInfo, int>? value = null;
		if (values.Count() > 0)
		{
			value = values.FirstOrDefault();
		}
		if (value.HasValue)
		{
			return value.GetValueOrDefault().Value;
		}
		else
		{
			var max = 0;
			try
			{
				max = Enum.GetValues(t).Cast<int>().Max();
			}
			catch
			{
			}
			var val = 0;
			if (t.IsDefined(typeof(FlagsAttribute), false))
			{
				val = max == 0 ? 1 : max * 2;
			}
			else
			{
				val = max + 1;
			}
			valuesForEnum.Add(ginfo, val);
			enumsChanged = true;
			return val;
		}
	}

	[HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.GetPlayerStatValue))]
	[HarmonyPrefix]
	private static void FixCharacterCountTracked(GameStatsManager __instance)
    {
		if (__instance.m_numCharacters > 0 && enumsChanged)
		{
			__instance.m_numCharacters = Mathf.Max(__instance.m_numCharacters, Enum.GetValues(typeof(PlayableCharacters)).Length);
			enumsChanged = false;
		}
	}

	[HarmonyPatch(typeof(GameStatsManager), nameof(GameStatsManager.GetPlayerMaximum))]
	[HarmonyPrefix]
	private static void FixCharacterCountMaximum(GameStatsManager __instance)
	{
		if (__instance.m_numCharacters > 0 && enumsChanged)
		{
			__instance.m_numCharacters = Mathf.Max(__instance.m_numCharacters, Enum.GetValues(typeof(PlayableCharacters)).Length);
			enumsChanged = false;
		}
	}

	[HarmonyPatch(typeof(fsEnumConverter), nameof(fsEnumConverter.TryDeserialize))]
	[HarmonyPostfix]
	private static void FixEnumConverter(ref fsResult __result, fsData data, ref object instance, Type storageType)
    {
        if (!__result._success && data.IsString)
        {
			string[] array = data.AsString.Split(new char[]
				   {
					','
				   }, StringSplitOptions.RemoveEmptyEntries);
			long num = 0L;
			foreach (string text in array)
			{
				if (!fsEnumConverter.ArrayContains(Enum.GetNames(storageType), text))
				{
					var split = text.Split('.');
					if (split.Count() > 3)
                    {
						var guid = string.Join(".", split.Take(3).ToArray());
						var name = string.Join(".", split.Skip(3).ToArray());
						ExtendEnum(guid, name, storageType);
                    }
                    else
                    {
						return;
                    }
				}
				long num2 = (long)Convert.ChangeType(Enum.Parse(storageType, text), typeof(long));
				num |= num2;
			}
			instance = Enum.ToObject(storageType, num);
			__result = fsResult.Success;
		}
    }

	[HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
    [HarmonyPostfix]
    private static void AddNewValues(ref Array __result, Type enumType)
    {
        if (__result != null && extendedEnums != null && extendedEnums.ContainsKey(enumType))
        {
            List<object> list = new();
            foreach (var obj in __result)
            {
                list.Add(obj);
            }
            list.AddRange(extendedEnums[enumType].Values.Select(x => Enum.ToObject(enumType, x)));
            __result = Array.CreateInstance(enumType, list.Count);
            for(int i = 0; i < __result.Length; i++)
            {
                __result.SetValue(list[i], i);
            }
        }
    }

    [HarmonyPatch(typeof(Enum), nameof(Enum.GetNames))]
    [HarmonyPostfix]
	private static void AddNewNames(ref string[] __result, Type enumType)
    {
        if (__result != null && extendedEnums != null && extendedEnums.ContainsKey(enumType))
        {
            __result = __result.Concat(extendedEnums[enumType].Keys.Select(x => x.ToString())).ToArray();
        }
    }

    [HarmonyPatch(typeof(Enum), nameof(Enum.GetName))]
    [HarmonyPostfix]
	private static void AddName(ref string __result, Type enumType, object value)
    {
        if(__result == null && extendedEnums != null && extendedEnums.ContainsKey(enumType))
        {
            try
            {
                var val = (int)value;
                var kvps = extendedEnums[enumType].Where(x => x.Value == val);
                if(kvps.Count() > 0)
                {
                    __result = kvps.FirstOrDefault().Key.ToString();
                }
            }
            catch { }
        }
    }

	[HarmonyPatch(typeof(Enum), nameof(Enum.IsDefined))]
	[HarmonyPostfix]
	private static void AddDefined(ref bool __result, Type enumType, object value)
    {
		if(extendedEnums != null && extendedEnums.ContainsKey(enumType) && !__result)
		{
			Type type = value.GetType();
			if (type == typeof(string))
			{
				__result = Enum.GetNames(enumType).Contains(value);
			}
			else
			{
				__result = (int)typeof(Enum).GetMethod("FindPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { value, Enum.GetValues(enumType) }) >= 0;
			}
		}
	}

	[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string), typeof(bool))]
	[HarmonyFinalizer]
	private static Exception AddParse(Exception __exception, ref object __result, Type enumType, string value, bool ignoreCase)
    {
		if(__exception != null)
        {
			if(__result != null || extendedEnums == null || !extendedEnums.ContainsKey(enumType))
            {
				return __exception;
            }
			if(__exception is ArgumentException)
            {
				if (enumType == null)
				{
					return __exception;
				}
				if (value == null)
				{
					return __exception;
				}
				if (!enumType.IsEnum)
				{
					return __exception;
				}
				value = value.Trim();
				if (value.Length == 0)
				{
					return __exception;
				}
				var values = Enum.GetValues(enumType);
				var names = Enum.GetNames(enumType);
				int num = FindName(names, value, ignoreCase);
				if (num >= 0)
				{
					__result = values.GetValue(num);
					return null;
				}
				TypeCode typeCode = ((Enum)values.GetValue(0)).GetTypeCode();
				if (value.IndexOf(',') != -1)
				{
					string[] array = value.Split(',');
					ulong num2 = 0UL;
					for (int i = 0; i < array.Length; i++)
					{
						num = FindName(names, array[i].Trim(), ignoreCase);
						if (num < 0)
						{
							throw new ArgumentException("The requested value was not found.");
						}
						num2 |= GetValue(values.GetValue(num), typeCode);
					}
					__result = Enum.ToObject(enumType, num2);
					return null;
				}
			}
			return __exception;
        }
		return null;
    }

	private static int FindName(string[] names, string name, bool ignoreCase)
	{
		if (!ignoreCase)
		{
			for (int i = 0; i < names.Length; i++)
			{
				if (name == names[i])
				{
					return i;
				}
			}
		}
		else
		{
			for (int j = 0; j < names.Length; j++)
			{
				if (string.Compare(name, names[j], ignoreCase, CultureInfo.InvariantCulture) == 0)
				{
					return j;
				}
			}
		}
		return -1;
	}

	private static ulong GetValue(object value, TypeCode typeCode)
	{
        return typeCode switch
        {
            TypeCode.SByte => (ulong)((byte)((sbyte)value)),
            TypeCode.Byte => (ulong)((byte)value),
            TypeCode.Int16 => (ulong)((ushort)((short)value)),
            TypeCode.UInt16 => (ulong)((ushort)value),
            TypeCode.Int32 => (ulong)((int)value),
            TypeCode.UInt32 => (ulong)((uint)value),
            TypeCode.Int64 => (ulong)((long)value),
            TypeCode.UInt64 => (ulong)value,
            _ => throw new ArgumentException("typeCode is not a valid type code for an Enum"),
        };
    }

	[HarmonyPatch(typeof(Enum), "FormatFlags")]
    [HarmonyPostfix]
	private static void FixFlagFormatting(ref string __result, Type enumType, object value)
    {
        if(extendedEnums != null && extendedEnums.ContainsKey(enumType))
        {
			string text = string.Empty;
			var values = Enum.GetValues(enumType);
			var names = Enum.GetNames(enumType);
			string text2 = value.ToString();
			if (text2 == "0")
			{
				text = Enum.GetName(enumType, value);
				if (text == null)
				{
					text = text2;
				}
				__result = text;
				return;
			}
			switch (((Enum)values.GetValue(0)).GetTypeCode())
			{
				case TypeCode.SByte:
					{
						sbyte b = (sbyte)value;
						for (int i = values.Length - 1; i >= 0; i--)
						{
							sbyte b2 = (sbyte)values.GetValue(i);
							if ((int)b2 != 0)
							{
								if (((int)b & (int)b2) == (int)b2)
								{
									text = names[i] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									b = (sbyte)((int)b - (int)b2);
								}
							}
						}
						if ((int)b != 0)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.Byte:
					{
						byte b3 = (byte)value;
						for (int j = values.Length - 1; j >= 0; j--)
						{
							byte b4 = (byte)values.GetValue(j);
							if (b4 != 0)
							{
								if ((b3 & b4) == b4)
								{
									text = names[j] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									b3 -= b4;
								}
							}
						}
						if (b3 != 0)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.Int16:
					{
						short num = (short)value;
						for (int k = values.Length - 1; k >= 0; k--)
						{
							short num2 = (short)values.GetValue(k);
							if (num2 != 0)
							{
								if ((num & num2) == num2)
								{
									text = names[k] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num -= num2;
								}
							}
						}
						if (num != 0)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.UInt16:
					{
						ushort num3 = (ushort)value;
						for (int l = values.Length - 1; l >= 0; l--)
						{
							ushort num4 = (ushort)values.GetValue(l);
							if (num4 != 0)
							{
								if ((num3 & num4) == num4)
								{
									text = names[l] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num3 -= num4;
								}
							}
						}
						if (num3 != 0)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.Int32:
					{
						int num5 = (int)value;
						for (int m = values.Length - 1; m >= 0; m--)
						{
							int num6 = (int)values.GetValue(m);
							if (num6 != 0)
							{
								if ((num5 & num6) == num6)
								{
									text = names[m] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num5 -= num6;
								}
							}
						}
						if (num5 != 0)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.UInt32:
					{
						uint num7 = (uint)value;
						for (int n = values.Length - 1; n >= 0; n--)
						{
							uint num8 = (uint)values.GetValue(n);
							if (num8 != 0u)
							{
								if ((num7 & num8) == num8)
								{
									text = names[n] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num7 -= num8;
								}
							}
						}
						if (num7 != 0u)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.Int64:
					{
						long num9 = (long)value;
						for (int num10 = values.Length - 1; num10 >= 0; num10--)
						{
							long num11 = (long)values.GetValue(num10);
							if (num11 != 0L)
							{
								if ((num9 & num11) == num11)
								{
									text = names[num10] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num9 -= num11;
								}
							}
						}
						if (num9 != 0L)
						{
							__result = text2;
							return;
						}
						break;
					}
				case TypeCode.UInt64:
					{
						ulong num12 = (ulong)value;
						for (int num13 = values.Length - 1; num13 >= 0; num13--)
						{
							ulong num14 = (ulong)values.GetValue(num13);
							if (num14 != 0UL)
							{
								if ((num12 & num14) == num14)
								{
									text = names[num13] + ((!(text == string.Empty)) ? ", " : string.Empty) + text;
									num12 -= num14;
								}
							}
						}
						if (num12 != 0UL)
						{
							__result = text2;
							return;
						}
						break;
					}
			}
			if (text == string.Empty)
			{
				__result = text2;
				return;
			}
			__result = text;
		}
    }
}
