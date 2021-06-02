using System.Collections.Generic;
using RoR2;
using UnityEngine;
using System.Linq;
using static PlayableAselica.Networking;
using RoR2.Projectile;

// the entitystates namespace is used to make the skills, i'm not gonna go into detail here but it's easy to learn through trial and error
namespace EntityStates.AselicaStates
{
	public class Secondary : BaseSkillState
	{
		public float damageCoefficient = 2f;
		public float baseDuration = 1.5f;
		public float recoil = 1f;
		public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");

		private float duration;
		private float fireDuration;
		private Animator animator;
		private string muzzleString;
		private Vector3 areaIndicator;
		private bool hasFired;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.25f * this.duration;
			base.characterBody.SetAimTimer(4.00f);
			this.animator = base.GetModelAnimator();
			this.muzzleString = "Muzzle";
			this.animator.Play("SkillTwo", 0);

			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(PlayableAselica.AselicaCastingTrail.projectilePrefab, this.characterBody.transform.position + new Vector3(0, -1.2f, 0.6f), this.gameObject.transform.rotation,
				base.gameObject, this.damageStat * PlayableAselica.AselicaCastingTrail.damageCoefficient, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
			}
			

			float maxDistance = 100000f;
			RaycastHit raycastHit;
			if (Physics.Raycast(base.GetAimRay(), out raycastHit, maxDistance, LayerIndex.world.mask))
			{
				this.areaIndicator = raycastHit.point;
			}
		}

		public override void OnExit()
		{
			base.OnExit();

			if (!hasFired)
			{
				if (base.isAuthority)
				{
					hasFired = true;
					ProjectileManager.instance.FireProjectile(PlayableAselica.AselicaCastingTrail.landingProjectilPrefab, areaIndicator + new Vector3(0, 0.2f, 0.6f), this.gameObject.transform.rotation,
					base.gameObject, this.damageStat * PlayableAselica.AselicaCastingTrail.damageCoefficient, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
					doDamage();
				}
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();

			if (base.fixedAge >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
			}
		}

		public void doDamage()
		{
			if (base.fixedAge >= this.fireDuration)
			{
				Vector3 trans = areaIndicator;
				Collider[] colls = CollectEnemies(4.5f, trans, 20.0f);
				var hurts = colls.Where(x => x.GetComponent<HurtBox>() != null && x.GetComponent<HurtBox>().teamIndex != teamComponent.teamIndex);

				foreach (Collider coll in hurts)
				{
					HurtBox hurt = coll.GetComponentInChildren<HurtBox>();

					ServerDamageContainer serverDamageContainer = new ServerDamageContainer(new DamageInfo
					{
						damage = this.damageStat * 2,
						attacker = base.gameObject,
						procCoefficient = 2,
						position = hurt.transform.position,
						crit = base.RollCrit()
					}, hurt.healthComponent.gameObject);

					PlayableAselica.Aselica.network.netRequestDoDamage.Invoke(serverDamageContainer);
				}
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}

		private Collider[] CollectEnemies(float radius, Vector3 position, float maxYDiff)
		{
			Collider[] array = Physics.OverlapSphere(position, radius, LayerIndex.entityPrecise.mask);
			array = array.Where(x => Mathf.Abs(x.ClosestPoint(base.transform.position).y - base.transform.position.y) <= maxYDiff).ToArray();
			return array;
		}

		private bool ColliderDamage(Collider[] array, float damageMulti, float coeff)
		{
			// now that we have our enemies, only get the ones within the Y dimension
			var hurtboxes = array.Where(x => x.GetComponent<HurtBox>() != null);
			List<HurtBoxGroup> allReadyDamaged = new List<HurtBoxGroup>();
			HurtBox hitSoundObj = null;
			foreach (var hurtBox in hurtboxes)
			{
				var hurtBox2 = hurtBox.GetComponentInChildren<HurtBox>();
				if (hurtBox2 == null) continue;
				if (hurtBox2.healthComponent == base.healthComponent) continue;
				if (allReadyDamaged.Where(x => x == hurtBox2.hurtBoxGroup).Count() > 0) continue; // already hit them
				if (hurtBox2.teamIndex == base.teamComponent.teamIndex) continue; // dont hit teammates LUL
				hitSoundObj = hurtBox2;
				allReadyDamaged.Add(hurtBox2.hurtBoxGroup);
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = damageStat * damageMulti;
				damageInfo.attacker = base.gameObject;
				damageInfo.procCoefficient = coeff;
				damageInfo.position = hurtBox2.transform.position;
				damageInfo.crit = base.RollCrit();

				// WEB HANDLE
				for(int i = 0; i < 5; i++)
				{
					if(i == 4)
					{
						damageInfo.damage *= 2;
					}

					ServerDamageContainer container = new ServerDamageContainer(damageInfo, hurtBox2.healthComponent.gameObject);
					PlayableAselica.Aselica.network.netRequestDoDamage.Invoke(container);
					Vector3 hitPos = hurtBox2.collider.ClosestPoint(base.transform.position);
				}
			}
			if (allReadyDamaged == null) return false;
			return allReadyDamaged.Count() > 0;
		}
	}
}