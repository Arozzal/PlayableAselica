using System.Collections.Generic;
using RoR2;
using UnityEngine;
using System.Linq;
using static PlayableAselica.Networking;

// the entitystates namespace is used to make the skills, i'm not gonna go into detail here but it's easy to learn through trial and error
namespace EntityStates.AselicaStates
{
	public class Primary : BaseSkillState
	{
		public float damageCoefficient = 2f;
		public float baseDuration = 0.75f;
		public float recoil = 1f;
		public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");

		private float duration;
		private float fireDuration;
		private static bool hit1 = false;
		private static bool hit2 = false;
		private static bool hit3 = false;
		private Animator animator;
		private string muzzleString;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			this.fireDuration = 0.25f * this.duration;
			base.characterBody.SetAimTimer(2f);
			this.animator = base.GetModelAnimator();
			this.muzzleString = "Muzzle";
			//this.animator.SetBool("isAttacking", true);
			this.animator.Play("Attacking", 0);
			
		}

		public override void OnExit()
		{
			hit1 = false;
			hit2 = false;
			hit3 = false;
			base.OnExit();
		}

		public void updateDamage()
		{
			Vector3 trans = base.transform.position + base.characterDirection.forward * 3.0f;
			Collider[] colls = CollectEnemies(2.0f, trans, 2.0f);
			var hurts = colls.Where(x => x.GetComponent<HurtBox>() != null);
			List<HurtBoxGroup> list = new List<HurtBoxGroup>();
			foreach (Collider coll in hurts)
			{
				HurtBox hurt = coll.GetComponentInChildren<HurtBox>();

				bool flag = !(hurt == null) && !(hurt.healthComponent == base.healthComponent) && (from x in list
					where x == hurt.hurtBoxGroup
					select x).Count<HurtBoxGroup>() <= 0 && hurt.teamIndex != base.teamComponent.teamIndex;

				if (flag)
				{
					int id = coll.GetComponentInParent<Transform>().GetInstanceID();
					list.Add(hurt.hurtBoxGroup);

					ServerDamageContainer serverDamageContainer = new ServerDamageContainer(new DamageInfo
					{
						damage = this.damageStat,
						attacker = base.gameObject,
						procCoefficient = 2,
						position = hurt.transform.position,
						crit = base.RollCrit()
					}, hurt.healthComponent.gameObject);

					PlayableAselica.Aselica.network.netRequestDoDamage.Invoke(serverDamageContainer);
				}
			}
		}

		

	

	public override void FixedUpdate()
		{

			base.FixedUpdate();

			if (hit1 == false)
			{
				hit1 = true;
				updateDamage();
			}
		
			if (base.fixedAge > duration * 0.33)
			{
				if(hit2 == false)
				{
					hit2 = true;
					updateDamage();
				}

			}

			if (base.fixedAge > duration * 0.66)
			{
				if (hit3 == false)
				{
					hit3 = true;
					updateDamage();
				}
			}


			if (base.fixedAge >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
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
				ServerDamageContainer container = new ServerDamageContainer(damageInfo, hurtBox2.healthComponent.gameObject);

				PlayableAselica.Aselica.network.netRequestDoDamage.Invoke(container);

				// play hit fx!
				Vector3 hitPos = hurtBox2.collider.ClosestPoint(base.transform.position);
				//PlayFx(Assets.SephHitFx, hitPos, Quaternion.identity, 1f);
			}
			if (allReadyDamaged == null) return false;
			return allReadyDamaged.Count() > 0;
		}
	}
}