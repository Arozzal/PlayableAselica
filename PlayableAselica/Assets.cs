using R2API;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PlayableAselica
{
	// get the assets from your assetbundle here
	// if it's returning null, check and make sure you have the build action set to "Embedded Resource" and the file names are right because it's not gonna work otherwise

	public enum TextStyle
	{
		DEATH,
		UTILITY,
		DAMAGE,
		STACK
	}

	public static class Assets
	{
		public static AssetBundle MainAssetBundle = null;
		public static AssetBundleResourcesProvider Provider;

		public static Texture charPortrait;

		public static Sprite iconP;
		public static Sprite icon1;
		public static Sprite icon2;
		public static Sprite icon3;
		public static Sprite icon4;
		public static GameObject fallingSwordProjectile;

		

		public static string applyStyle(string text, TextStyle style)
		{
			string styleToken = "";

			switch (style)
			{
				case TextStyle.DAMAGE:
					styleToken = "cIsDamage";
					break;
				case TextStyle.UTILITY:
					styleToken = "cIsUtility";
					break;
				case TextStyle.STACK:
					styleToken = "cStack";
					break;
				case TextStyle.DEATH:
					styleToken = "cDeath";
					break;
			}

			return "<style=" + styleToken + ">" + text + "</style>";
				
		}


		public static void PopulateAssets()
		{
			if (MainAssetBundle == null)
			{
				using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayableAselica.aselica"))
				{
					MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
					Provider = new AssetBundleResourcesProvider("@aselica", MainAssetBundle);
				}
			}

			// include this if you're using a custom soundbank
			/*using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleSurvivor.ExampleSurvivor.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }*/

			// and now we gather the assets
			charPortrait = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/aselicaIcon 1.png").texture;

			iconP = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/aselicaIcon 1.png");
			icon1 = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/Icon/AselicaIconSkill1.png");
			icon2 = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/Icon/AselicaIconSkill2.png");
			icon3 = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/Icon/AselicaIconSkill3.png");
			icon4 = MainAssetBundle.LoadAsset<Sprite>("Assets/aselica/Icon/AselicaIconSkill4.png");

			AselicaCastingTrail.projectilePrefab = MainAssetBundle.LoadAsset<GameObject>("Assets/aselica/Skill 1/PrefabInstance/Effect_Skill_Hero_Aselica_Skill1_Casting.prefab");

			AselicaCastingTrail.projectilePrefab.AddComponent<ProjectileController>();
			AselicaCastingTrail.projectilePrefab.GetComponent<ProjectileController>().procCoefficient = 1f;
			AselicaCastingTrail.projectilePrefab.AddComponent<ProjectileDamage>();
			AselicaCastingTrail.projectilePrefab.GetComponent<ProjectileDamage>().damage = 1f;
			AselicaCastingTrail.projectilePrefab.GetComponent<ProjectileDamage>().damageType = RoR2.DamageType.Generic;
			AselicaCastingTrail.projectilePrefab.AddComponent<EffectSelfDestruction>();
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			
			// register it for networking
			if (AselicaCastingTrail.projectilePrefab) PrefabAPI.RegisterNetworkPrefab(AselicaCastingTrail.projectilePrefab);

			// add it to the projectile catalog or it won't work in multiplayer
			RoR2.ProjectileCatalog.getAdditionalEntries += list =>
			{
				list.Add(AselicaCastingTrail.projectilePrefab);
			};


			AselicaCastingTrail.landingProjectilPrefab = MainAssetBundle.LoadAsset<GameObject>("Assets/aselica/Skill 1/PrefabInstance/Effect_Skill_Hero_Aselica_Skill1_Projectile.prefab");

			AselicaCastingTrail.landingProjectilPrefab.AddComponent<ProjectileController>();
			AselicaCastingTrail.landingProjectilPrefab.GetComponent<ProjectileController>().procCoefficient = 1f;
			AselicaCastingTrail.landingProjectilPrefab.AddComponent<ProjectileDamage>();
			AselicaCastingTrail.landingProjectilPrefab.GetComponent<ProjectileDamage>().damage = 1f;
			AselicaCastingTrail.landingProjectilPrefab.GetComponent<ProjectileDamage>().damageType = RoR2.DamageType.Generic;
			AselicaCastingTrail.landingProjectilPrefab.AddComponent<CircleFade>();
			// register it for networking
			if (AselicaCastingTrail.landingProjectilPrefab) PrefabAPI.RegisterNetworkPrefab(AselicaCastingTrail.landingProjectilPrefab);

			// add it to the projectile catalog or it won't work in multiplayer
			RoR2.ProjectileCatalog.getAdditionalEntries += list =>
			{
				list.Add(AselicaCastingTrail.landingProjectilPrefab);
			};

			fallingSwordProjectile = MainAssetBundle.LoadAsset<GameObject>("Effect_Skill_Hero_Aselica_Skill3_Projectile_Sword(Mesh)");
			fallingSwordProjectile.AddComponent<ProjectileController>();
			fallingSwordProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
			fallingSwordProjectile.AddComponent<ProjectileDamage>();
			fallingSwordProjectile.GetComponent<ProjectileDamage>().damage = 1f;
			fallingSwordProjectile.GetComponent<ProjectileDamage>().damageType = RoR2.DamageType.Generic;
			fallingSwordProjectile.AddComponent<CircleFade>();
			fallingSwordProjectile.AddComponent<SwordFalling>();



			// register it for networking
			if (fallingSwordProjectile) PrefabAPI.RegisterNetworkPrefab(fallingSwordProjectile);

			// add it to the projectile catalog or it won't work in multiplayer
			RoR2.ProjectileCatalog.getAdditionalEntries += list =>
			{
				list.Add(fallingSwordProjectile);
			};


			//
		}
	}
}
