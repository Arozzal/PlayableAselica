using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2.Projectile;
using BepInEx;
using R2API.Utils;
using System.Reflection;
using System.Linq;
using RoR2.UI;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using RoR2.Skills;
using R2API.AssetPlus;
using KinematicCharacterController;
using MiniRpcLib;
using MiniRpcLib.Action;
using UnityEngine.Networking;


namespace PlayableAselica
{
    public class Networking
    {
        #region NETWORK
        // RPC
        public IRpcAction<ServerDamageContainer> netRequestDoDamage = null;
        public IRpcAction<ServerBuffContainer> NetRequestGiveBuff = null;
        public IRpcAction<ClientFxContainer> NetRequestSpawnFx = null;
        public IRpcAction<ClientFxContainer> NetServerRequestSpawnFx = null;
        public IRpcAction<ClientAnimatorBoolContainer> NetServerRequestAnimBool = null;
        public IRpcAction<ClientAnimatorBoolContainer> NetClientRequestAnimBool = null;
        public IRpcAction<ClientAnimatorFloatContainer> NetServerRequestAnimFloat = null;
        public IRpcAction<ClientAnimatorFloatContainer> NetClientRequestAnimFloat = null;
        public IRpcAction<ClientAnimatorTriggerContainer> NetServerRequestAnimTrigger = null;
        public IRpcAction<ClientAnimatorTriggerContainer> NetClientRequestAnimTrigger = null;
        // RPC  RPC     RPC     RPC     RPC     RPC     RPC     RPC //  NetworkUser.readOnlyInstancesList

        public static MiniRpcInstance rpc = null;
		public static NetworkUser thisUser;
		public static NetworkIdentity thisInstance;

		public void RegisterNetworking(string MODUID)
        {
            rpc = MiniRpc.CreateInstance(MODUID);

			

			if (netRequestDoDamage == null)
            {
                netRequestDoDamage = rpc.RegisterAction<ServerDamageContainer>(Target.Server, new Action<NetworkUser, ServerDamageContainer>(RPCHandleServerDamage), null);
            }

			if(NetClientRequestAnimBool == null)
			{
				NetClientRequestAnimBool = rpc.RegisterAction<ClientAnimatorBoolContainer>(Target.Client, new Action<NetworkUser, ClientAnimatorBoolContainer>(RPCHandleClientAnimBool), null);
			}

			if (NetServerRequestAnimBool == null)
			{
				NetServerRequestAnimBool = rpc.RegisterAction<ClientAnimatorBoolContainer>(Target.Server, new Action<NetworkUser, ClientAnimatorBoolContainer>(RPCHandleServerAnimBool), null);
			}

		}

        [Server]
        private void RPCHandleServerDamage(NetworkUser arg1, ServerDamageContainer arg2)
        {
			// deal the DAMAGES
			try
            {
				
                arg2.DealDamage();
            }
            catch (Exception ex) { } // not very important if this fails
        }
        [Server]
        private void RPCHandleServerBuff(NetworkUser arg1, ServerBuffContainer arg2)
        {
            try
            {
                arg2.Execute();
            }
            catch (Exception ex) { } // not very important if this fails
        }
        [Server]
        private void RPCHandleServerToClientFx(NetworkUser arg1, ClientFxContainer arg2)
        {
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                try
                {
                    NetRequestSpawnFx.Invoke(arg2, user);
                }
                catch (Exception ex) { } // not important if this fails
            }
        }

        /*
        [Client]
        private void RPCHandleClientFx(NetworkUser arg1, ClientFxContainer arg2)
        {
            try
            {
                StartCoroutine(RPCPlayFx(Assets.MainAssetBundle.LoadAsset<GameObject>(arg2.prefabName), arg2.position, arg2.rotation, arg2.emissionLife, arg2.destroyLife));
            }
            catch (Exception ex) { } // not very important if this fails
        }
        private IEnumerator RPCPlayFx(GameObject particle, Vector3 pos, Quaternion rot, float emissionLife, float destroyLife)
        {
            var go = GameObject.Instantiate(particle, pos, rot);
            var particles = go.GetComponentsInChildren<ParticleSystem>();
            Destroy(go, destroyLife);
            yield return new WaitForSeconds(emissionLife);
            foreach (var obj in particles)
            {
                if (obj != null)
                    obj.Stop();
            }
        }*/

        // ANIM RPC
        [Server]
        private void RPCHandleServerAnimBool(NetworkUser arg1, ClientAnimatorBoolContainer arg2)
        {
            try
            {
                foreach (var user in NetworkUser.readOnlyInstancesList)
                {
                    if (arg1 != null)
                        if (arg1.netId == user.netId) continue; // skip issuer as it happens client side
                    NetClientRequestAnimBool.Invoke(arg2, user);
                }
            }
            catch (Exception ex) { }
        }
        [Client]
        private void RPCHandleClientAnimBool(NetworkUser arg1, ClientAnimatorBoolContainer arg2)
        {
            try
            {
                arg2.Execute();
            }
            catch (Exception ex) { }
        }
        [Server]
        private void RPCHandleServerAnimFloat(NetworkUser arg1, ClientAnimatorFloatContainer arg2)
        {
            try
            {
                foreach (var user in NetworkUser.readOnlyInstancesList)
                {
                    if (arg1 != null)
                        if (arg1.netId == user.netId) continue; // skip issuer as it happens client side
                    NetClientRequestAnimFloat.Invoke(arg2, user);
                }
            }
            catch (Exception ex) { }
        }
        [Client]
        private void RPCHandleClientAnimFloat(NetworkUser arg1, ClientAnimatorFloatContainer arg2)
        {
            try
            {
                arg2.Execute();
            }
            catch (Exception ex) { }
        }
        [Server]
        private void RPCHandleServerAnimTrigger(NetworkUser arg1, ClientAnimatorTriggerContainer arg2)
        {
            try
            {
                foreach (var user in NetworkUser.readOnlyInstancesList)
                {
                    if (arg1 != null)
                    {
                        if (arg1.netId == user.netId) continue; // skip issuer as it happens client side
                    }
                    NetClientRequestAnimTrigger.Invoke(arg2, user);
                }
            }
            catch (Exception ex) { }
        }
        [Client]
        private void RPCHandleClientAnimTrigger(NetworkUser arg1, ClientAnimatorTriggerContainer arg2)
        {
            try
            {
                arg2.Execute();
            }
            catch (Exception ex) { }
        }

        public class ServerDamageContainer : MessageBase
        {
            public DamageInfo damage;
            public GameObject enemyGO;
            private HealthComponent _healthComponent;
            public ServerDamageContainer(DamageInfo damage, GameObject enemyHurboxGO)
            {
                this.damage = damage;
                this.enemyGO = enemyHurboxGO;
            }
            public void DealDamage()
            {
                _healthComponent = enemyGO.GetComponentInChildren<HealthComponent>();
                _healthComponent.TakeDamage(damage);
                GlobalEventManager.instance.OnHitEnemy(damage, _healthComponent.gameObject);
                GlobalEventManager.instance.OnHitAll(damage, _healthComponent.gameObject);
            }

            public override void Deserialize(NetworkReader reader)
            {
                damage = new DamageInfo();
                damage.attacker = reader.ReadGameObject();
                damage.crit = reader.ReadBoolean();
                damage.damage = reader.ReadSingle();
                damage.damageColorIndex = reader.ReadDamageColorIndex();
                damage.damageType = reader.ReadDamageType();
                damage.force = reader.ReadVector3();
                damage.procCoefficient = reader.ReadSingle();
                damage.position = reader.ReadVector3();

                // gameob
                enemyGO = reader.ReadGameObject();
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(damage.attacker);
                writer.Write(damage.crit);
                writer.Write(damage.damage);
                writer.Write(damage.damageColorIndex);
                writer.Write(damage.damageType);
                writer.Write(damage.force);
                writer.Write(damage.procCoefficient);
                writer.Write(damage.position);

                // gameobject time
                writer.Write(enemyGO);
            }
        }
        public class ServerBuffContainer : MessageBase
        {
            public BuffIndex buff;
            public float buffDuration;
            public CharacterBody body;
            public ServerBuffContainer(BuffIndex buff, float buffDuration, CharacterBody body)
            {
                this.buff = buff;
                this.buffDuration = buffDuration;
                this.body = body;
            }
            public void Execute()
            {
                body.AddTimedBuff(buff, buffDuration);
            }

            public override void Deserialize(NetworkReader reader)
            {
                // buff
                buff = (BuffIndex)Enum.Parse(typeof(BuffIndex), reader.ReadString());

                buffDuration = reader.ReadSingle();
                body = reader.ReadGameObject().GetComponent<CharacterBody>();
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(buff.ToString());
                writer.Write(buffDuration);
                writer.Write(body.gameObject);
            }
        }
        public class ClientFxContainer : MessageBase
        {
            public string prefabName;
            public float emissionLife;
            public float destroyLife;
            public Vector3 position;
            public Quaternion rotation;

            public ClientFxContainer(string prefabName, float emissionLife, float destroyLife, Vector3 pos, Quaternion rot)
            {
                this.prefabName = prefabName;
                this.emissionLife = emissionLife;
                this.destroyLife = destroyLife;
                this.position = pos;
                this.rotation = rot;
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(prefabName);
                writer.Write(emissionLife);
                writer.Write(destroyLife);
                writer.Write(position);
                writer.Write(rotation);
            }
            public override void Deserialize(NetworkReader reader)
            {
                prefabName = reader.ReadString();
                emissionLife = reader.ReadSingle();
                destroyLife = reader.ReadSingle();
                position = reader.ReadVector3();
                rotation = reader.ReadQuaternion();
            }
        }
        public class ServerMagnetContainer : MessageBase
        {
            public float killTime;
            public float orbDuration;
            public Vector3 spawnPoint;
            public float maxDistanceFromOrb;
            public ServerMagnetContainer(float killTime, float orbDuration, Vector3 spawnPos, float maxDistFromOrb)
            {
                this.killTime = killTime;
                this.orbDuration = orbDuration;
                this.spawnPoint = spawnPos;
                this.maxDistanceFromOrb = maxDistFromOrb;
            }
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(killTime);
                writer.Write(orbDuration);
                writer.Write(spawnPoint);
                writer.Write(maxDistanceFromOrb);
            }
            public override void Deserialize(NetworkReader reader)
            {
                killTime = reader.ReadSingle();
                orbDuration = reader.ReadSingle();
                spawnPoint = reader.ReadVector3();
                maxDistanceFromOrb = reader.ReadSingle();
            }
        }
        public class ClientAnimatorBoolContainer : MessageBase
        {
            public GameObject characterDirection;
            public bool animBool;
            public string animName;

            public void Execute()
            {
                var animator = characterDirection.GetComponent<CharacterDirection>();
                animator.modelAnimator.SetBool(animName, animBool);
            }
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(characterDirection);
                writer.Write(animBool);
                writer.Write(animName);
            }
            public override void Deserialize(NetworkReader reader)
            {
                characterDirection = reader.ReadGameObject();
                animBool = reader.ReadBoolean();
                animName = reader.ReadString();
            }
        }
        public class ClientAnimatorFloatContainer : MessageBase
        {
            public GameObject characterDirection;
            public float animFloat;
            public string animName;

            public void Execute()
            {
                var animator = characterDirection.GetComponent<CharacterDirection>();
                animator.modelAnimator.SetFloat(animName, animFloat);
            }
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(characterDirection);
                writer.Write(animFloat);
                writer.Write(animName);
            }
            public override void Deserialize(NetworkReader reader)
            {
                characterDirection = reader.ReadGameObject();
                animFloat = reader.ReadSingle();
                animName = reader.ReadString();
            }
        }
        public class ClientAnimatorTriggerContainer : MessageBase
        {
            public GameObject characterDirection;
            public string animName;

            public void Execute()
            {
                var animator = characterDirection.GetComponent<CharacterDirection>();
                animator.modelAnimator.SetTrigger(animName);
            }
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(characterDirection);
                writer.Write(animName);
            }
            public override void Deserialize(NetworkReader reader)
            {
                characterDirection = reader.ReadGameObject();
                animName = reader.ReadString();
            }
        }
        #endregion
    }
}
