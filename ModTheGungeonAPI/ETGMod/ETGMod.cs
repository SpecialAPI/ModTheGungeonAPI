using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Core class of the MTG API.
/// </summary>
public static partial class ETGMod
{
	/// <summary>
	/// Returns the path to the Resources directory (Path/To/Game/Resources)
	/// </summary>
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

	/// <summary>
	/// Returns the path to the old Sprite Replacement directory (Path/To/Resources/SpriteReplacement)
	/// </summary>
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

	public static readonly Action<Coroutine> StopGlobalCoroutine = (x) => Chainloader.ManagerObject.GetOrAddComponent<GlobalCRRunner>().StopCoroutine(x);
    public static readonly Func<IEnumerator, Coroutine> StartGlobalCoroutine = (x) => Chainloader.ManagerObject.GetOrAddComponent<GlobalCRRunner>().StartCoroutine(x);

	internal class GlobalCRRunner : MonoBehaviour //because transforms can't run coroutines for some reason
    {
    }

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

	/// <summary>
	/// Contains the instance Item Database and the instance String Database;
	/// </summary>
	public static class Databases
    {
		/// <summary>
		/// The instance Item Database used for adding, getting and creating new items.
		/// </summary>
        public static readonly ItemDB Items = new();
		/// <summary>
		/// The instance String Database for editing, adding and getting strings.
		/// </summary>
        public static readonly StringDB Strings = new();
    }

	/// <summary>
	/// Contains all of the AIActor hooks.
	/// </summary>
	public static class AIActor
	{
		/// <summary>
		/// Runs before an AIActor fully starts.
		/// </summary>
		public static Action<global::AIActor> OnPreStart;
		/// <summary>
		/// Runs after an AIActor fully starts.
		/// </summary>
		public static Action<global::AIActor> OnPostStart;
		/// <summary>
		/// Runs before an AIActor is checked to become jammed.
		/// </summary>
		public static Action<global::AIActor> OnBlackPhantomnessCheck;
	}

	/// <summary>
	/// Contains all of the Chest hooks.
	/// </summary>
	public static class Chest
	{
		/// <summary>
		/// Runs after a chest spawns.
		/// </summary>
		public static Action<global::Chest> OnPostSpawn;
		/// <summary>
		/// Runs before a chest is opened. If the final result of the hook is false, the chest will not open.
		/// </summary>
		public static DOnPreOpen OnPreOpen;
		/// <summary>
		/// Runs after a chest is opened.
		/// </summary>
		public static Action<global::Chest, PlayerController> OnPostOpen;
		/// <summary>
		/// Hook delegate for the OnPreOpen hook. If the final result is false, the chest will not open.
		/// </summary>
		/// <param name="shouldOpen">Current result. Starts as true, gets set to the returned result of this delegate after running.</param>
		/// <param name="chest">The target chest.</param>
		/// <param name="player">The player trying to open the chest.</param>
		/// <returns></returns>
		public delegate bool DOnPreOpen(bool shouldOpen, global::Chest chest, PlayerController player);
	}
}
