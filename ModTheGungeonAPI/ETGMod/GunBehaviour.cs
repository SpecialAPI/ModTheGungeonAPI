using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Reflection;
using HarmonyLib;
using Dungeonator;

/// <summary>
/// Base class for gun modifier behaviours.
/// </summary>
public class GunBehaviour : BraveBehaviour
{
	private void Awake()
	{
		gun = GetComponent<Gun>();
		if (gun != null)
		{
			OnCreation(gun);
			gun.OnInitializedWithOwner += OnInitializedWithOwner;
			gun.OnInitializedWithOwner += InternalOnInitializedWithOwner;
			gun.PostProcessProjectile += PostProcessProjectile;
			gun.PostProcessVolley += PostProcessVolley;
			gun.OnDropped += OnDropped;
			gun.OnDropped += InternalOnDropped;
			gun.OnAutoReload += OnAutoReload;
			gun.OnReloadPressed += OnReloadPressed;
			gun.OnFinishAttack += OnFinishAttack;
			gun.OnPostFired += OnPostFired;
			gun.OnAmmoChanged += OnAmmoChanged;
			gun.OnBurstContinued += OnBurstContinued;
			gun.OnPreFireProjectileModifier += OnPreFireProjectileModifier;
			gun.OnReflectedBulletDamageModifier += OnReflectedBulletDamageModifier;
			gun.OnReflectedBulletScaleModifier += OnReflectedBulletScaleModifier;
			gun.ModifyActiveCooldownDamage += ModifyActiveCooldownDamage;
			if (gun.CurrentOwner != null)
			{
				OnInitializedWithOwner(gun.CurrentOwner);
				InternalOnInitializedWithOwner(gun.CurrentOwner);
			}
		}
	}

	private void InternalOnInitializedWithOwner(GameActor owner)
	{
		if (owner == null)
		{
			return;
		}
		if (owner is PlayerController player)
		{
			OnPlayerPickup(player);
		}
		else if (owner is AIActor enemy)
		{
			OnEnemyPickup(enemy);
		}
	}

	private void InternalOnDropped()
	{
		if (gun?.CurrentOwner != null)
		{
			if (gun.CurrentOwner is PlayerController player)
			{
				OnDroppedByPlayer(player);
			}
		}
	}

	/// <summary>
	/// Runs when a player drops the gun this behaviour is applied to.
	/// </summary>
	/// <param name="player">The player that dropped the gun.</param>
	public virtual void OnDroppedByPlayer(PlayerController player)
	{

	}

	/// <summary>
	/// Runs when an AIActor picks up the gun this behaviour is applied to.
	/// </summary>
	/// <param name="enemyOwner">The enemy that picked up the gun.</param>
	public virtual void OnEnemyPickup(AIActor enemyOwner)
	{
	}

	/// <summary>
	/// Runs when a player picks up the gun this behaviour is applied to.
	/// </summary>
	/// <param name="playerOwner">The player that picked up the gun.</param>
	public virtual void OnPlayerPickup(PlayerController playerOwner)
	{
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to is created as an object.
	/// </summary>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnCreation(Gun gun)
	{
	}

	/// <summary>
	/// Runs every frame when the gun is active, even if the gun is not picked up.
	/// </summary>
	public virtual void Update()
	{
	}

	/// <summary>
	/// Modifies the damage of bullets reflected through reload.
	/// </summary>
	/// <param name="originalDamage">The original damage of the bullet.</param>
	/// <returns>The modified damage of the bullet</returns>
	public virtual float OnReflectedBulletDamageModifier(float originalDamage)
	{
		return originalDamage;
	}

	/// <summary>
	/// Modifies the scale of bullets reflected through reload.
	/// </summary>
	/// <param name="originalScale">The original scale of the bullet.</param>
	/// <returns>The modified scale of the bullet</returns>
	public virtual float OnReflectedBulletScaleModifier(float originalScale)
	{
		return originalScale;
	}

	/// <summary>
	/// Modifies the amount of damage dealt for cooldown purposes. The higher the number, the faster the recharge.
	/// </summary>
	/// <param name="originalDamage">The original amount of damage dealt.</param>
	/// <returns>The modified amount of damage dealt.</returns>
	public virtual float ModifyActiveCooldownDamage(float originalDamage)
	{
		return originalDamage;
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to gets picked up, be it by enemy or by player.
	/// </summary>
	/// <param name="actor">The enemy or player that picked up this gun.</param>
	public virtual void OnInitializedWithOwner(GameActor actor)
	{
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to ends a burst shot.
	/// </summary>
	/// <param name="player">The player owner of the gun.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnBurstContinued(PlayerController player, Gun gun)
	{
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to shoots a projectile. Can be used to modify the projectile that was created.
	/// </summary>
	/// <param name="projectile">The projectile shot.</param>
	public virtual void PostProcessProjectile(Projectile projectile)
	{
	}

	/// <summary>
	/// Runs when the volley of the gun this behaviour is applied to gets updated. Can be used to modify the volley based on synergies and etc.
	/// </summary>
	/// <param name="volley">The volley that is being updated. Modify this argument to modify the updated volley.</param>
	public virtual void PostProcessVolley(ProjectileVolleyData volley)
	{
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to is dropped, be it by enemy or by player.
	/// </summary>
	public virtual void OnDropped()
	{
	}

	/// <summary>
	/// Runs when the gun this behaviour is applied to is reloaded with an empty clip, be it manually or automatically.
	/// </summary>
	/// <param name="player">The player that reloaded the gun.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnAutoReload(PlayerController player, Gun gun)
	{
	}

	/// <summary>
	/// Runs when a player tries to reload the gun this behaviour is applied to, even if it's full. To check for actual reloads, use Gun.IsReloading.
	/// </summary>
	/// <param name="player">The player that reloaded the gun.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	/// <param name="manual">True if the player reloaded the gun by pressing the reload key, false otherwise.</param>
	public virtual void OnReloadPressed(PlayerController player, Gun gun, bool manual)
	{
	}

	/// <summary>
	/// Runs when the player stops shooting with the gun this behaviour is applied to.
	/// </summary>
	/// <param name="player">The player that stopped shooting.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnFinishAttack(PlayerController player, Gun gun)
	{
	}

	/// <summary>
	/// Runs after the gun this behaviour is applied to is shot, after all of the projectiles were created. On charge guns, runs once per charge when the player starts charging the gun.
	/// </summary>
	/// <param name="player">The player that shot the gun.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnPostFired(PlayerController player, Gun gun)
	{
	}

	/// <summary>
	/// Runs after the gun this behaviour is applied to either loses or gains any amount of ammo.
	/// </summary>
	/// <param name="player">The player that owns the gun.</param>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	public virtual void OnAmmoChanged(PlayerController player, Gun gun)
	{
	}

	/// <summary>
	/// Runs before the gun this behaviour is applied to is fired. Can be used to change what projectile gets fired.
	/// </summary>
	/// <param name="gun">The gun this behaviour is applied to.</param>
	/// <param name="projectile">The original projectile that was about to get fired.</param>
	/// <param name="module">The projectile module that is being fired.</param>
	/// <returns>The modified projectile that will be fired. Return the original projectile for no change.</returns>
	public virtual Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile, ProjectileModule module)
	{
		return projectile;
	}

	public void BraveOnLevelWasLoaded()
	{
		OnLevelLoadPreGeneration();
		if (gun?.CurrentOwner != null && gun.CurrentOwner is PlayerController player)
		{
			OnPlayerLevelLoadPreGeneration(player);
		}
		StartCoroutine(DelayedLoad());
	}

	private IEnumerator DelayedLoad()
	{
		while (Dungeon.IsGenerating)
		{
			yield return null;
		}
		OnLevelLoadPostGeneration();
		if (gun?.CurrentOwner != null && gun.CurrentOwner is PlayerController player)
		{
			OnPlayerLevelLoadPostGeneration(player);
		}
		yield break;
	}

	/// <summary>
	/// Runs when a new floor is loaded, but before it's actually generated.
	/// </summary>
	public virtual void OnLevelLoadPreGeneration()
	{
	}

	/// <summary>
	/// Runs when a new floor is loaded with a player owner, but before the floor is actually generated.
	/// </summary>
	/// <param name="player">The player owner of the gun this behaviour is applied to.</param>
	public virtual void OnPlayerLevelLoadPreGeneration(PlayerController player)
	{
	}

	/// <summary>
	/// Runs when a floor is loaded and fully generated.
	/// </summary>
	public virtual void OnLevelLoadPostGeneration()
	{
	}

	/// <summary>
	/// Runs when a floor is loaded and fully generated with a player owner.
	/// </summary>
	/// <param name="player">The player owner of the gun this behaviour is applied to.</param>
	public virtual void OnPlayerLevelLoadPostGeneration(PlayerController player)
	{
	}

	/// <summary>
	/// Transfer the data that wouldn't normally be saved from a gun pre-pickup to a gun post-pickup.
	/// </summary>
	/// <param name="sourceGun">The gun pre-pickup.</param>
	public virtual void InheritData(Gun sourceGun)
	{
	}

	/// <summary>
	/// Save data from this gun to a mid-game save.
	/// </summary>
	/// <param name="data">The list of data to save. Add data to this list to save it.</param>
	/// <param name="dataIndex">Not used.</param>
	public virtual void MidGameSerialize(List<object> data, int dataIndex)
	{
	}

	/// <summary>
	/// Get data from the given list of data, cast it to the type T and increment the data index.
	/// </summary>
	/// <typeparam name="T">The type to cast the data to.</typeparam>
	/// <param name="data">The list of data to get the data from.</param>
	/// <param name="dataIndex">The index to increment.</param>
	/// <returns>The data found from the list.</returns>
	public T DeserializeObject<T>(List<object> data, ref int dataIndex)
	{
		T result = default;
		if (data[dataIndex] is T t)
		{
			result = t;
		}
		dataIndex++;
		return result;
	}

	/// <summary>
	/// Get data from the saved list of a mid-game save. Data must be gotten in the exact same order as it was saved in MidGameSerialize.
	/// </summary>
	/// <param name="data">The list of data from the mid-game save.</param>
	/// <param name="dataIndex">The current data index. Needs to be increased by the amount of data gotten from the list,</param>
	public virtual void MidGameDeserialize(List<object> data, ref int dataIndex)
	{
	}

	/// <summary>
	/// The gun this behaviour is applied to.
	/// </summary>
	[NonSerialized]
	public Gun gun;
	/// <summary>
	/// Generic GameActor owner of this gun.
	/// </summary>
	public GameActor GenericOwner => gun?.CurrentOwner;
	/// <summary>
	/// PlayerController owner of the gun this behaviour is applied to if the owner is a player, null otherwise.
	/// </summary>
	public PlayerController PlayerOwner => GenericOwner as PlayerController;
	/// <summary>
	/// AIActor owner of the gun this behaviour is applied to if the owner is an enemy, null otherwise.
	/// </summary>
	public AIActor EnemyOwner => GenericOwner as AIActor;
	/// <summary>
	/// True if the gun was ever picked up, false otherwise.
	/// </summary>
	public bool EverPickedUp => (gun?.HasBeenPickedUp).GetValueOrDefault();
}
