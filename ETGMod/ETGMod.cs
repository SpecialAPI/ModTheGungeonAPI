using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static partial class ETGMod
{
    public static string ResourcesDirectory
	{
		get
		{
			var dir = Path.Combine(Paths.GameRootPath, "Resources");
            if (!Directory.Exists(dir))
            {
				Directory.CreateDirectory(dir);
            }
			return dir;
		}
	}

	public static string SpriteReplacementDirectory
	{
		get
		{
			var dir = Path.Combine(ResourcesDirectory, "SpriteReplacement");
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			return dir;
		}
	}

	public static Action<Coroutine> StopGlobalCoroutine;
    public static Func<IEnumerator, Coroutine> StartGlobalCoroutine;

	public static T RunHook<T>(this MulticastDelegate md, T val, params object[] args)
	{
		if (md == null)
		{
			return val;
		}
		object[] array = new object[args.Length + 1];
		array[0] = val;
		Array.Copy(args, 0, array, 1, args.Length);
		Delegate[] invocationList = md.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			array[0] = invocationList[i].DynamicInvoke(array);
		}
		return (T)((object)array[0]);
	}

	public static class Databases
    {
        public static readonly ItemDB Items = new();
        public static readonly StringDB Strings = new();
    }

	public static class AIActor
	{
		public static Action<global::AIActor> OnPreStart;
		public static Action<global::AIActor> OnPostStart;
		public static Action<global::AIActor> OnBlackPhantomnessCheck;
	}

	public static class Chest
	{
		public static Action<global::Chest> OnPostSpawn;
		public static DOnPreOpen OnPreOpen;
		public static Action<global::Chest, PlayerController> OnPostOpen;
		public delegate bool DOnPreOpen(bool shouldOpen, global::Chest chest, PlayerController player);
	}
}
