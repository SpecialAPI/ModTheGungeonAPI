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

public class GunBehaviour : BraveBehaviour
{
	public void Awake()
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

	public virtual void OnDroppedByPlayer(PlayerController player)
	{

	}

	public virtual void OnEnemyPickup(AIActor enemyOwner)
	{
	}

	public virtual void OnPlayerPickup(PlayerController playerOwner)
	{
	}

	public virtual void Start()
	{
	}

	public virtual void OnCreation(Gun gun)
	{
	}

	public virtual void Update()
	{
	}

	public virtual float OnReflectedBulletDamageModifier(float originalDamage)
	{
		return originalDamage;
	}

	public virtual float OnReflectedBulletScaleModifier(float originalScale)
	{
		return originalScale;
	}

	public virtual float ModifyActiveCooldownDamage(float originalDamage)
	{
		return originalDamage;
	}

	public virtual void OnInitializedWithOwner(GameActor actor)
	{
	}

	public virtual void OnBurstContinued(PlayerController player, Gun gun)
	{
	}

	public virtual void PostProcessProjectile(Projectile projectile)
	{
	}

	public virtual void PostProcessVolley(ProjectileVolleyData volley)
	{
	}

	public virtual void OnDropped()
	{
	}

	public virtual void OnAutoReload(PlayerController player, Gun gun)
	{
	}

	public virtual void OnReloadPressed(PlayerController player, Gun gun, bool manual)
	{
	}

	public virtual void OnFinishAttack(PlayerController player, Gun gun)
	{
	}

	public virtual void OnPostFired(PlayerController player, Gun gun)
	{
	}

	public virtual void OnAmmoChanged(PlayerController player, Gun gun)
	{
	}

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

	public virtual void OnLevelLoadPreGeneration()
	{
	}

	public virtual void OnPlayerLevelLoadPreGeneration(PlayerController player)
	{
	}

	public virtual void OnLevelLoadPostGeneration()
	{
	}

	public virtual void OnPlayerLevelLoadPostGeneration(PlayerController player)
	{
	}

	public virtual void InheritData(Gun sourceGun)
	{
	}

	public virtual void MidGameSerialize(List<object> data, int dataIndex)
	{
	}

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

	public virtual void MidGameDeserialize(List<object> data, ref int dataIndex)
	{
	}

	[NonSerialized]
	public Gun gun;
	public GameActor GenericOwner => gun?.CurrentOwner;
	public PlayerController PlayerOwner => GenericOwner as PlayerController;
	public AIActor EnemyOwner => GenericOwner as AIActor;
	public bool EverPickedUp => (gun?.HasBeenPickedUp).GetValueOrDefault();
}
