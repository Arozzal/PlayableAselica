using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleFade : MonoBehaviour
{
	// Start is called before the first frame update

	ParticleSystem[] partSyses;

	void Start()
	{
		partSyses = gameObject.GetComponentsInChildren<ParticleSystem>();
		foreach(var part in partSyses)
		{
			part.Stop();
			part.Clear();

			var main = part.main;
			main.startDelay = 0.2f;

			part.Play();
		}

		SkinnedMeshRenderer[] renders = this.GetComponentsInChildren<SkinnedMeshRenderer>();
		for(int i = 0; i < renders.Length; i++)
		{
			if(renders[i].name == "Effect_Skill_Hero_Scarlet_Skill2_Casting_Aura")
			{
				Destroy(renders[i], 1.0f);
			}
		}

		Destroy(gameObject, 3.0f);
	}

	// Update is called once per frame
	void Update()
	{

	}
}
