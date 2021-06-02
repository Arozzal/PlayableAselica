using EntityStates;
using EntityStates.Huntress;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlayableAselica
{
	class AselicaCastingTrail : BaseArrowBarrage
	{
		public static float damageCoefficient = 0.5f;
		public static GameObject projectilePrefab;
		public static GameObject landingProjectilPrefab;
		private GameObject areaIndicatorInstance;
		private bool shouldFireArrowRain;

		public override void OnEnter()
		{
			base.OnEnter();
			base.PlayAnimation("FullBody, Override", "LoopArrowRain");
		}

		// Token: 0x06003AD4 RID: 15060 RVA: 0x000F2B30 File Offset: 0x000F0D30
		private void UpdateAreaIndicator()
		{
			if (this.areaIndicatorInstance)
			{
				float maxDistance = 1000f;
				RaycastHit raycastHit;
				if (Physics.Raycast(base.GetAimRay(), out raycastHit, maxDistance, LayerIndex.world.mask))
				{
					this.areaIndicatorInstance.transform.position = raycastHit.point;
					this.areaIndicatorInstance.transform.up = raycastHit.normal;
				}
			}
		}

		// Token: 0x06003AD5 RID: 15061 RVA: 0x000F2BA0 File Offset: 0x000F0DA0
		public override void Update()
		{
			base.Update();
			//this.UpdateAreaIndicator();
		}

		// Token: 0x06003AD6 RID: 15062 RVA: 0x000F2BAE File Offset: 0x000F0DAE
		protected override void HandlePrimaryAttack()
		{
			base.HandlePrimaryAttack();
			this.shouldFireArrowRain = true;
			this.outer.SetNextStateToMain();
		}

		// Token: 0x06003AD8 RID: 15064 RVA: 0x000F2C63 File Offset: 0x000F0E63
		public override void OnExit()
		{
			if (this.areaIndicatorInstance)
			{
				EntityState.Destroy(this.areaIndicatorInstance.gameObject);
			}
			base.OnExit();
		}

		
	}
}
