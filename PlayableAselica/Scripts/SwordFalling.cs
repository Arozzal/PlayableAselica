using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlayableAselica.Networking;

class SwordFalling : MonoBehaviour
{
	// Start is called before the first frame update
	public static float damageStat = 0;
	public static float damageCoefizienz = 0;
	public static TeamIndex teamindex = TeamIndex.None;
	bool isDeath = false;
	ParticleSystem[] parts;

	void Start()
	{
		parts = gameObject.GetComponentsInChildren<ParticleSystem>();
		foreach(ParticleSystem part in parts)
		{
			part.Stop();
		}

		Transform[] trans = gameObject.GetComponents<Transform>();
		for (int i = 0; i < trans.Length; i++)
		{
			trans[i].Rotate(90, -0, -0);
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (isDeath == false)
		{
			Vector3 pos = gameObject.transform.position;
			pos.y -= 0.6f;
			gameObject.transform.position = pos;
			Collider[] colls = CollectEnemies(4.0f, gameObject.transform.position, 4.0f);
			ColliderDamage(colls);
		}
	}

	private Collider[] CollectEnemies(float radius, Vector3 position, float maxYDiff)
	{
		Collider[] array = Physics.OverlapSphere(position, radius, LayerIndex.entityPrecise.mask);
		array = array.Where(x => Mathf.Abs(x.ClosestPoint(base.transform.position).y - base.transform.position.y) <= maxYDiff).ToArray();
		return array;
	}

	private void ColliderDamage(Collider[] array)
	{
		// now that we have our enemies, only get the ones within the Y dimension
		var hurtboxes = array.Where(x => x.GetComponent<HurtBox>() != null);
		HurtBox hitSoundObj = null;
		foreach (var hurtBox in hurtboxes)
		{
			var hurtBox2 = hurtBox.GetComponentInChildren<HurtBox>();
			if (hurtBox2 == null) continue;
			if (hurtBox2.teamIndex == teamindex) continue; // dont hit teammates LUL
			hitSoundObj = hurtBox2;
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = damageStat * damageCoefizienz;
			damageInfo.attacker = base.gameObject;
			damageInfo.procCoefficient = 1;
			damageInfo.position = hurtBox2.transform.position;
			damageInfo.crit = false;

			// WEB HANDLE
			ServerDamageContainer container = new ServerDamageContainer(damageInfo, hurtBox2.healthComponent.gameObject);
			PlayableAselica.Aselica.network.netRequestDoDamage.Invoke(container);
			Vector3 hitPos = hurtBox2.collider.ClosestPoint(base.transform.position);
			gameObject.GetComponent<MeshRenderer>().enabled = false;
			foreach (ParticleSystem part in parts)
			{
				part.Play();
			}
			isDeath = true;
			//Destroy(gameObject);
		}
	}
}
