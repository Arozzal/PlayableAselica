using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayableAselica.Networking;

public class AselicaScript : MonoBehaviour
{
	// Start is called before the first frame update
	static bool fly = false;

	public float ClickDuration = 0.6f;
	public bool isLongKlicked = false;
	bool clicking = false;
	float totalDownTime = 0;

	void Start()
	{

	}

	void UpdateLongKeyPress()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			totalDownTime = 0;
			clicking = true;
		}


		if (clicking && Input.GetKey(KeyCode.Space))
		{
			totalDownTime += Time.deltaTime;

			if (totalDownTime >= ClickDuration)
			{
				clicking = false;
				isLongKlicked = true;
			}
		}

		if (clicking && Input.GetKeyUp(KeyCode.Space))
		{
			clicking = false;
		}
	}

	void Update()
	{
		
		CharacterMotor motor = gameObject.GetComponent<CharacterMotor>();
		CharacterBody body = gameObject.GetComponent<CharacterBody>();
		Animator anim = gameObject.GetComponent<CharacterDirection>().modelAnimator;

		UpdateLongKeyPress();

		if (isLongKlicked)
		{
			fly = true;
		}


		if (fly)
		{
			motor.velocity.y = 0.8f;
			motor.velocity.x = 0;
			motor.velocity.z = 0;

			if (Input.GetKeyDown(KeyCode.Space) && !isLongKlicked)
			{
				fly = false;
			}
			isLongKlicked = false;

			if (Input.GetKey(KeyCode.W))
			{
				Vector3 velocity = Camera.main.transform.forward * body.moveSpeed;
				if (Input.GetKey(KeyCode.D))
				{
					velocity = Quaternion.Euler(0, 45, 0) * velocity;
				}
				if (Input.GetKey(KeyCode.A))
				{
					velocity = Quaternion.Euler(0, -45, 0) * velocity;
				}
				motor.velocity = velocity;

				anim.SetBool("isGrounded", true);
				body.characterDirection.modelAnimator.SetBool("isGrounded", true);
				PlayableAselica.Aselica.network.NetServerRequestAnimBool.Invoke(PlayableAselica.Aselica.boolAnim, thisUser);
			}
			else
			{

				anim.SetBool("isGrounded", false);
				body.characterDirection.modelAnimator.SetBool("isGrounded", false);
				PlayableAselica.Aselica.network.NetServerRequestAnimBool.Invoke(PlayableAselica.Aselica.boolAnim, thisUser);
			}
		}
	}
}