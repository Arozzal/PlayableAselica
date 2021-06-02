using System.Collections.Generic;
using RoR2;
using UnityEngine;
using System.Linq;
using static PlayableAselica.Networking;

// the entitystates namespace is used to make the skills, i'm not gonna go into detail here but it's easy to learn through trial and error
namespace EntityStates.AselicaStates
{
	public class Buff : BaseSkillState
	{
		public float damageCoefficient = 2f;
		public float baseDuration = 0.75f;
		public float recoil = 1f;
		public static GameObject tracerEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/Tracers/TracerToolbotRebar");

		private float duration;
		private float fireDuration;
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
			this.animator.Play("FourthSkill", 0);
		}

		public override void OnExit()
		{
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
			for (int i = 0; i < players.Length; i++)
			{
				if(players[i].GetComponent<CharacterBody>().teamComponent.teamIndex != base.teamComponent.teamIndex)
				{
					continue;
				}


				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.ArmorBoost, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.MeatRegenBoost, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.FullCrit, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.AttackSpeedOnCrit, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.CloakSpeed, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.PowerBuff, 10.0f);
				players[i].GetComponent<CharacterBody>().AddTimedBuff(BuffIndex.WhipBoost, 10.0f);
			}

			base.OnExit();
		}

		public override void FixedUpdate()
		{


			base.FixedUpdate();

			

			if (base.fixedAge >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}
	}
}