using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Used for easy loading of assets.
/// </summary>
public static class LoadHelper
{
	/// <summary>
	/// Loads an asset with the given name from all asset bundles and returns the first one found.
	/// </summary>
	/// <param name="path">The path or the name of the asset.</param>
	/// <returns>The found asset or null if nothing was found</returns>
	public static UnityEngine.Object LoadAssetFromAnywhere(string path)
	{
		UnityEngine.Object obj = null;
		foreach (string name in BundlePrereqs)
		{
			try
			{
				obj = ResourceManager.LoadAssetBundle(name).LoadAsset(path);
			}
			catch
			{
			}
			if (obj != null)
			{
				break;
			}
		}
		return obj;
	}

	/// <summary>
	/// Loads an asset with the given name and the given type from all asset bundles and returns the first one found.
	/// </summary>
	/// <typeparam name="T">The required type of the asset.</typeparam>
	/// <param name="path">The path or the name of the asset.</param>
	/// <returns>The found asset or null if nothing was found</returns>
	public static T LoadAssetFromAnywhere<T>(string path) where T : UnityEngine.Object
	{
		T obj = null;
		foreach (string name in BundlePrereqs)
		{
			try
			{
				obj = ResourceManager.LoadAssetBundle(name).LoadAsset<T>(path);
			}
			catch
			{
			}
			if (obj != null)
			{
				break;
			}
		}
		return obj;
	}

	/// <summary>
	/// Finds all assets that match the path and type in all asset bundles.
	/// </summary>
	/// <typeparam name="T">The type of assets to load.</typeparam>
	/// <param name="toFind">The path or the name for the assets.</param>
	/// <returns>A list of all found assets.</returns>
	public static List<T> Find<T>(string toFind) where T : UnityEngine.Object
	{
		List<T> objects = new();
		foreach (string name in BundlePrereqs)
		{
			try
			{
				foreach (string str in ResourceManager.LoadAssetBundle(name).GetAllAssetNames())
				{
					if (str.ToLower().Contains(toFind))
					{
						if (ResourceManager.LoadAssetBundle(name).LoadAsset(str).GetType() == typeof(T) && !objects.Contains(ResourceManager.LoadAssetBundle(name).LoadAsset<T>(str)))
						{
							objects.Add(ResourceManager.LoadAssetBundle(name).LoadAsset<T>(str));
						}
					}
				}
			}
			catch
			{
			}
		}
		return objects;
	}

	/// <summary>
	/// Finds all assets that match the path in all asset bundles.
	/// </summary>
	/// <param name="toFind">The path or the name for the assets.</param>
	/// <returns>A list of all found assets.</returns>
	public static List<UnityEngine.Object> Find(string toFind)
	{
		List<UnityEngine.Object> objects = new();
		foreach (string name in BundlePrereqs)
		{
			try
			{
				foreach (string str in ResourceManager.LoadAssetBundle(name).GetAllAssetNames())
				{
					if (str.ToLower().Contains(toFind))
					{
						if (!objects.Contains(ResourceManager.LoadAssetBundle(name).LoadAsset(str)))
						{
							objects.Add(ResourceManager.LoadAssetBundle(name).LoadAsset(str));
						}
					}
				}
			}
			catch
			{
			}
		}
		return objects;
	}

	private static readonly string[] BundlePrereqs = new string[]
	{
		"brave_resources_001",
		"dungeon_scene_001",
		"encounters_base_001",
		"enemies_base_001",
		"flows_base_001",
		"foyer_001",
		"foyer_002",
		"foyer_003",
		"shared_auto_001",
		"shared_auto_002",
		"shared_base_001",
		"dungeons/base_bullethell",
		"dungeons/base_castle",
		"dungeons/base_catacombs",
		"dungeons/base_cathedral",
		"dungeons/base_forge",
		"dungeons/base_foyer",
		"dungeons/base_gungeon",
		"dungeons/base_mines",
		"dungeons/base_nakatomi",
		"dungeons/base_resourcefulrat",
		"dungeons/base_sewer",
		"dungeons/base_tutorial",
		"dungeons/finalscenario_bullet",
		"dungeons/finalscenario_convict",
		"dungeons/finalscenario_coop",
		"dungeons/finalscenario_guide",
		"dungeons/finalscenario_pilot",
		"dungeons/finalscenario_robot",
		"dungeons/finalscenario_soldier"
	};
}
