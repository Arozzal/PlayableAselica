using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using R2API.AssetPlus;
using EntityStates;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Animations;
using KinematicCharacterController;
using System.Linq;
using MiniRpcLib;
using MiniRpcLib.Action;
using static PlayableAselica.Networking;

namespace PlayableAselica
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(MODUID, "Playable Aselica", "1.0.0")] // put your own name and version here
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(SurvivorAPI), nameof(LoadoutAPI), nameof(ItemAPI), nameof(DifficultyAPI), nameof(BuffAPI))] // need these dependencies for the mod to work properly



	public class Aselica : BaseUnityPlugin
    {
        public const string MODUID = "com.Arozal.PlayableAselica"; // put your own names here

        public static GameObject characterPrefab; // the survivor body prefab
        public GameObject characterDisplay; // the prefab used for character select
        public GameObject doppelganger; // umbra shit

        public static GameObject arrowProjectile; // prefab for our survivor's primary attack projectilefile

        private static readonly Color characterColor = new Color(1f, 0.85f, 0f); // color used for the survivor
        public static Networking network = null;

		public static ClientAnimatorBoolContainer boolAnim = new ClientAnimatorBoolContainer();

		private void Awake()
        {
            Assets.PopulateAssets(); // first we load the assets from our assetbundle
            network = new Networking();
            network.RegisterNetworking(MODUID);
            CreatePrefab(); // then we create our character's body prefab
            RegisterStates(); // register our skill entitystates for networking
            RegisterCharacter(); // and finally put our new survivor in the game
            CreateDoppelganger(); // not really mandatory, but it's simple and not having an umbra is just kinda lame
        }

        

        private static GameObject CreateModel(GameObject main)
        {
            Destroy(main.transform.Find("ModelBase").gameObject);
            Destroy(main.transform.Find("CameraPivot").gameObject);
            Destroy(main.transform.Find("AimOrigin").gameObject);

            // make sure it's set up right in the unity project
            GameObject model = Assets.MainAssetBundle.LoadAsset<GameObject>("Assets/aselica/school/AselicaSchool.prefab");
			
            return model;
        }

        internal static void CreatePrefab()
        {
            // first clone the commando prefab so we can turn that into our own survivor
            characterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), "Aselica", true, "C:\\Users\\Sean\\source\\repos\\PlayableAselica\\PlayableAselica\\Aselica.cs", "CreatePrefab", 151);

            characterPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            // create the model here, we're gonna replace commando's model with our own
            GameObject model = CreateModel(characterPrefab);

            GameObject gameObject = new GameObject("ModelBase");
            gameObject.transform.parent = characterPrefab.transform;
            gameObject.transform.localPosition = new Vector3(0f, -0.81f, 0f);
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
            
            GameObject gameObject2 = new GameObject("CameraPivot");
            gameObject2.transform.parent = gameObject.transform;
            gameObject2.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            gameObject2.transform.localRotation = Quaternion.identity;
            gameObject2.transform.localScale = Vector3.one;

            GameObject gameObject3 = new GameObject("AimOrigin");
            gameObject3.transform.parent = gameObject.transform;
            gameObject3.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            gameObject3.transform.localRotation = Quaternion.identity;
            gameObject3.transform.localScale = Vector3.one;


            Transform transform = model.transform;
            transform.parent = gameObject.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = new Vector3(1f, 1f, 1f);
            transform.localRotation = Quaternion.identity;

            CharacterDirection characterDirection = characterPrefab.GetComponent<CharacterDirection>();
            characterDirection.moveVector = Vector3.zero;
            characterDirection.targetTransform = gameObject.transform;
            characterDirection.overrideAnimatorForwardTransform = null;
            characterDirection.rootMotionAccumulator = null;
            characterDirection.modelAnimator = model.GetComponentInChildren<Animator>();
            characterDirection.driveFromRootRotation = false;
            characterDirection.turnSpeed = 720f;

            SkinnedMeshRenderer[] rn = characterPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            for(int i = 0; i < rn.Length; i++){
                if(rn[i].name == "Hero_Aselica_Cos19Casual_Body")
                {
                    RoR2.Chat.AddMessage(rn[i].name);
                    Mesh msRen = rn[i].sharedMesh;
                    int[] tri0 = msRen.GetTriangles(0);
                    int[] tri1 = msRen.GetTriangles(1);

                    int[] trians = new int[tri0.Length + tri1.Length];
                    
                    tri0.CopyTo(trians, 0);
                    tri1.CopyTo(trians, tri0.Length);
                    msRen.SetTriangles(trians, 0);
                }
            }

			thisInstance = characterPrefab.GetComponent<NetworkIdentity>();
			thisUser = NetworkUser.readOnlyInstancesList.Where(x => x.netId == thisInstance.netId).FirstOrDefault();

			boolAnim.animName = "isGrounded";
			boolAnim.characterDirection = characterDirection.gameObject;

			// set up the character body here
			CharacterBody bodyComponent = characterPrefab.GetComponent<CharacterBody>();
            bodyComponent.bodyIndex = -1;
            bodyComponent.baseNameToken = "ASELICA_NAME"; // name token
            bodyComponent.subtitleNameToken = "ASELICA_SUBTITLE"; // subtitle token- used for umbras
            bodyComponent.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent.rootMotionInMainState = false;
            bodyComponent.mainRootSpeed = 0;
            bodyComponent.baseMaxHealth = 90;
            bodyComponent.levelMaxHealth = 24;
            bodyComponent.baseRegen = 0.5f;
            bodyComponent.levelRegen = 0.25f;
            bodyComponent.baseMaxShield = 0;
            bodyComponent.levelMaxShield = 0;
            bodyComponent.baseMoveSpeed = 7;
            bodyComponent.levelMoveSpeed = 0;
            bodyComponent.baseAcceleration = 80;
            bodyComponent.baseJumpPower = 20;
            bodyComponent.levelJumpPower = 0;
            bodyComponent.baseDamage = 15;
            bodyComponent.levelDamage = 3f;
            bodyComponent.baseAttackSpeed = 1;
            bodyComponent.levelAttackSpeed = 0;
            bodyComponent.baseCrit = 1;
            bodyComponent.levelCrit = 0;
            bodyComponent.baseArmor = 0;
            bodyComponent.levelArmor = 0;
            bodyComponent.baseJumpCount = 2;
            bodyComponent.sprintingSpeedMultiplier = 1.45f;
            bodyComponent.wasLucky = false;
            bodyComponent.hideCrosshair = false;
            bodyComponent.aimOriginTransform = gameObject3.transform;
            bodyComponent.hullClassification = HullClassification.Human;
            bodyComponent.portraitIcon = Assets.charPortrait;
            bodyComponent.isChampion = false;
            bodyComponent.currentVehicle = null;
            bodyComponent.skinIndex = 0U;

			characterPrefab.AddComponent<AselicaScript>();

			// the charactermotor controls the survivor's movement and stuff
			CharacterMotor characterMotor = characterPrefab.GetComponent<CharacterMotor>();
            characterMotor.walkSpeedPenaltyCoefficient = 1f;
            characterMotor.characterDirection = characterDirection;
            characterMotor.muteWalkMotion = false;
            characterMotor.mass = 100f;
            characterMotor.airControl = 0.25f;
            characterMotor.disableAirControlUntilCollision = false;
            characterMotor.generateParametersOnAwake = true;
			characterMotor.velocity.y = 0;

            InputBankTest inputBankTest = characterPrefab.GetComponent<InputBankTest>();
            inputBankTest.moveVector = Vector3.zero;

            CameraTargetParams cameraTargetParams = characterPrefab.GetComponent<CameraTargetParams>();
            cameraTargetParams.cameraParams = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponent<CameraTargetParams>().cameraParams;
            cameraTargetParams.cameraPivotTransform = null;
            cameraTargetParams.aimMode = CameraTargetParams.AimType.Standard;
            cameraTargetParams.recoil = Vector2.zero;
            cameraTargetParams.idealLocalCameraPos = Vector3.zero;
            cameraTargetParams.dontRaycastToPivot = false;

            // this component is used to locate the character model(duh), important to set this up here
            ModelLocator modelLocator = characterPrefab.GetComponent<ModelLocator>();
            modelLocator.modelTransform = transform;
            modelLocator.modelBaseTransform = gameObject.transform;
            modelLocator.dontReleaseModelOnDeath = false;
            modelLocator.autoUpdateModelTransform = true;
            modelLocator.dontDetatchFromParent = false;
            modelLocator.noCorpse = false;
            modelLocator.normalizeToFloor = false; // set true if you want your character to rotate on terrain like acrid does
            modelLocator.preserveModel = false;

            // childlocator is something that must be set up in the unity project, it's used to find any child objects for things like footsteps or muzzle flashes
            // also important to set up if you want quality
            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            // this component is used to handle all overlays and whatever on your character, without setting this up you won't get any cool effects like burning or freeze on the character
            // it goes on the model object of course
            CharacterModel characterModel = model.AddComponent<CharacterModel>();
            characterModel.body = bodyComponent;
            characterModel.baseRendererInfos = new CharacterModel.RendererInfo[]
            {
                // set up multiple rendererinfos if needed, but for this example there's only the one
                new CharacterModel.RendererInfo
                {
                    defaultMaterial = model.GetComponentInChildren<SkinnedMeshRenderer>().material,
                    renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(),
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false
                }
            };

            characterModel.autoPopulateLightInfos = true;
            characterModel.invisibilityCount = 0;
            characterModel.temporaryOverlays = new List<TemporaryOverlay>();




            TeamComponent teamComponent = null;
            if (characterPrefab.GetComponent<TeamComponent>() != null) teamComponent = characterPrefab.GetComponent<TeamComponent>();
            else teamComponent = characterPrefab.GetComponent<TeamComponent>();
            teamComponent.hideAllyCardDisplay = false;
            teamComponent.teamIndex = TeamIndex.None;

            HealthComponent healthComponent = characterPrefab.GetComponent<HealthComponent>();
            healthComponent.health = 90f;
            healthComponent.shield = 0f;
            healthComponent.barrier = 0f;
            healthComponent.magnetiCharge = 0f;
            healthComponent.body = null;
            healthComponent.dontShowHealthbar = false;
            healthComponent.globalDeathEventChanceCoefficient = 1f;

            characterPrefab.GetComponent<Interactor>().maxInteractionDistance = 3f;
            characterPrefab.GetComponent<InteractionDriver>().highlightInteractor = true;

            // this disables ragdoll since the character's not set up for it, and instead plays a death animation
            CharacterDeathBehavior characterDeathBehavior = characterPrefab.GetComponent<CharacterDeathBehavior>();
            characterDeathBehavior.deathStateMachine = characterPrefab.GetComponent<EntityStateMachine>();
            characterDeathBehavior.deathState = new SerializableEntityStateType(typeof(GenericCharacterDeath));

            // edit the sfxlocator if you want different sounds
            SfxLocator sfxLocator = characterPrefab.GetComponent<SfxLocator>();
            sfxLocator.deathSound = "Play_ui_player_death";
            sfxLocator.barkSound = "";
            sfxLocator.openSound = "";
            sfxLocator.landingSound = "Play_char_land";
            sfxLocator.fallDamageSound = "Play_char_land_fall_damage";
            sfxLocator.aliveLoopStart = "";
            sfxLocator.aliveLoopStop = "";

            Rigidbody rigidbody = characterPrefab.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            CapsuleCollider capsuleCollider = characterPrefab.GetComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = false;
            capsuleCollider.material = null;
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;

            KinematicCharacterMotor kinematicCharacterMotor = characterPrefab.GetComponent<KinematicCharacterMotor>();
            kinematicCharacterMotor.CharacterController = characterMotor;
            kinematicCharacterMotor.Capsule = capsuleCollider;
            kinematicCharacterMotor.Rigidbody = rigidbody;

            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.center = new Vector3(0, 0, 0);
            capsuleCollider.material = null;

            kinematicCharacterMotor.DetectDiscreteCollisions = false;
            kinematicCharacterMotor.GroundDetectionExtraDistance = 0f;
            kinematicCharacterMotor.MaxStepHeight = 0.2f;
            kinematicCharacterMotor.MinRequiredStepDepth = 0.1f;
            kinematicCharacterMotor.MaxStableSlopeAngle = 55f;
            kinematicCharacterMotor.MaxStableDistanceFromLedge = 0.5f;
            kinematicCharacterMotor.PreventSnappingOnLedges = false;
            kinematicCharacterMotor.MaxStableDenivelationAngle = 55f;
            kinematicCharacterMotor.RigidbodyInteractionType = RigidbodyInteractionType.None;
            kinematicCharacterMotor.PreserveAttachedRigidbodyMomentum = true;
            kinematicCharacterMotor.HasPlanarConstraint = false;
            kinematicCharacterMotor.PlanarConstraintAxis = Vector3.up;
            kinematicCharacterMotor.StepHandling = StepHandlingMethod.None;
            kinematicCharacterMotor.LedgeHandling = true;
            kinematicCharacterMotor.InteractiveRigidbodyHandling = true;
            kinematicCharacterMotor.SafeMovement = false;

            // this sets up the character's hurtbox, kinda confusing, but should be fine as long as it's set up in unity right
            HurtBoxGroup hurtBoxGroup = model.AddComponent<HurtBoxGroup>();

            HurtBox componentInChildren = model.GetComponentInChildren<CapsuleCollider>().gameObject.AddComponent<HurtBox>();
            componentInChildren.gameObject.layer = LayerIndex.entityPrecise.intVal;
            componentInChildren.healthComponent = healthComponent;
            componentInChildren.isBullseye = true;
            componentInChildren.damageModifier = HurtBox.DamageModifier.Normal;
            componentInChildren.hurtBoxGroup = hurtBoxGroup;
            componentInChildren.indexInGroup = 0;

            hurtBoxGroup.hurtBoxes = new HurtBox[]
            {
                componentInChildren
            };

            hurtBoxGroup.mainHurtBox = componentInChildren;
            hurtBoxGroup.bullseyeCount = 1;

            // this is for handling footsteps, not needed but polish is always good
            FootstepHandler footstepHandler = model.AddComponent<FootstepHandler>();
            footstepHandler.baseFootstepString = "Play_player_footstep";
            footstepHandler.sprintFootstepOverrideString = "";
            footstepHandler.enableFootstepDust = true;
            footstepHandler.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericFootstepDust");

            // ragdoll controller is a pain to set up so we won't be doing that here..
            RagdollController ragdollController = model.AddComponent<RagdollController>();
            ragdollController.bones = null;
            ragdollController.componentsToDisableOnRagdoll = null;

            // this handles the pitch and yaw animations, but honestly they are nasty and a huge pain to set up so i didn't bother
            AimAnimator aimAnimator = model.AddComponent<AimAnimator>();
            aimAnimator.inputBank = inputBankTest;
            aimAnimator.directionComponent = characterDirection;
            aimAnimator.pitchRangeMax = 55f;
            aimAnimator.pitchRangeMin = -50f;
            aimAnimator.yawRangeMin = -44f;
            aimAnimator.yawRangeMax = 44f;
            aimAnimator.pitchGiveupRange = 30f;
            aimAnimator.yawGiveupRange = 10f;
            aimAnimator.giveupDuration = 8f;
        }

        private void RegisterCharacter()
        {
            // now that the body prefab's set up, clone it here to make the display prefab
            characterDisplay = PrefabAPI.InstantiateClone(characterPrefab.GetComponent<ModelLocator>().modelBaseTransform.gameObject, "AselicaDisplay", true, "C:\\Users\\Sean\\source\\repos\\PlayableAselica\\PlayableAselica\\Aselica.cs", "RegisterCharacter", 153);
            
            Transform[] trans = characterDisplay.GetComponentsInChildren<Transform>();
            for(int i = 0; i < trans.Length; i++)
            {
                trans[i].Rotate(0, -120, 0);
            }
                
            characterDisplay.AddComponent<NetworkIdentity>();
			characterDisplay.AddComponent<Animator>();
			characterDisplay.GetComponent<Animator>().Play("SkillTwo", 0);

			// clone rex's syringe projectile prefab here to use as our own projectile
			arrowProjectile = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/SyringeProjectile"), "Prefabs/Projectiles/ExampleArrowProjectile", true, "C:\\Users\\Sean\\source\\repos\\PlayableAselica\\PlayableAselica\\Aselica.cs", "RegisterCharacter", 155);
            
            // just setting the numbers to 1 as the entitystate will take care of those
            arrowProjectile.GetComponent<ProjectileController>().procCoefficient = 1f;
            arrowProjectile.GetComponent<ProjectileDamage>().damage = 1f;
            arrowProjectile.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;

            // register it for networking
            if (arrowProjectile) PrefabAPI.RegisterNetworkPrefab(arrowProjectile);

            // add it to the projectile catalog or it won't work in multiplayer
            ProjectileCatalog.getAdditionalEntries += list =>
            {
                list.Add(arrowProjectile);
            };


            // write a clean survivor description here!
            string desc = "This game needs a sexy angel..." + Environment.NewLine + Environment.NewLine;

            // add the language tokens
            LanguageAPI.Add("ASELICA_NAME", "Aselica");
            LanguageAPI.Add("ASELICA_DESCRIPTION", desc);
            LanguageAPI.Add("ASELICA_SUBTITLE", "Template for Custom Survivors");

            // add our new survivor to the game~
            SurvivorDef survivorDef = new SurvivorDef
            {
                name = "ASELICA_NAME",
                unlockableName = "",
                descriptionToken = "ASELICA_DESCRIPTION",
                primaryColor = characterColor,
                bodyPrefab = characterPrefab,
                displayPrefab = characterDisplay
            };


            SurvivorAPI.AddSurvivor(survivorDef);

            // set up the survivor's skills here
            SkillSetup();

            // gotta add it to the body catalog too
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(characterPrefab);
            };
        }

        void SkillSetup()
        {
            // get rid of the original skills first, otherwise we'll have commando's loadout and we don't want that
            foreach (GenericSkill obj in characterPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }

            PassiveSetup();
            PrimarySetup();
			SecondarySetup();
			SwordRainSetup();
			BuffSetup();
        }

        void RegisterStates()
        {
            // register the entitystates for networking reasons
            LoadoutAPI.AddSkill(typeof(EntityStates.AselicaStates.Primary));
			LoadoutAPI.AddSkill(typeof(EntityStates.AselicaStates.Secondary));
			LoadoutAPI.AddSkill(typeof(EntityStates.AselicaStates.SwordRain));
			LoadoutAPI.AddSkill(typeof(EntityStates.AselicaStates.Buff));
		}

        void PassiveSetup()
        {
            // set up the passive skill here if you want
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("ASELICA_PASSIVE_NAME", "Angel Wings");
            LanguageAPI.Add("ASELICA_PASSIVE_DESCRIPTION", "Allows Aselica to " + Assets.applyStyle("fly", TextStyle.UTILITY) + " when you press the space bar long.");

            component.passiveSkill.enabled = true;
            component.passiveSkill.skillNameToken = "ASELICA_PASSIVE_NAME";
            component.passiveSkill.skillDescriptionToken = "ASELICA_PASSIVE_DESCRIPTION";
            component.passiveSkill.icon = Assets.iconP;
        }

        void PrimarySetup()
        {
            SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("ASELICA_PRIMARY_CROSSBOW_NAME", "Fancy Angel Hit");
            LanguageAPI.Add("ASELICA_PRIMARY_CROSSBOW_DESCRIPTION", "Dealing 3 hits with " + Assets.applyStyle("100% Damage", TextStyle.DAMAGE) + " in 2 seconds");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.AselicaStates.Primary));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon1;
            mySkillDef.skillDescriptionToken = "ASELICA_PRIMARY_CROSSBOW_DESCRIPTION";
            mySkillDef.skillName = "ASELICA_PRIMARY_CROSSBOW_NAME";
            mySkillDef.skillNameToken = "ASELICA_PRIMARY_CROSSBOW_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.primary = characterPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };


            // add this code after defining a new skilldef if you're adding an alternate skill

            /*Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(newSkillDef.skillNameToken, false, null)
            };*/
        }

		void SecondarySetup()
		{
			SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

			LanguageAPI.Add("ASELICA_SECONDARY_CROSSBOW_NAME", "Sword of the Sun");
			LanguageAPI.Add("ASELICA_SECONDARY_CROSSBOW_DESCRIPTION", "Fire " + Assets.applyStyle("5 swords", TextStyle.STACK) + " which fly to positon which is aimed in " + Assets.applyStyle("1.5 second", TextStyle.UTILITY) + " and deal " + Assets.applyStyle("4x 100% Damage + 1x 200% Damage", TextStyle.DAMAGE));

			// set up your primary skill def here!

			SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
			mySkillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.AselicaStates.Secondary));
			mySkillDef.activationStateMachineName = "Weapon";
			mySkillDef.baseMaxStock = 1;
			mySkillDef.baseRechargeInterval = 10f;
			mySkillDef.beginSkillCooldownOnSkillEnd = true;
			mySkillDef.canceledFromSprinting = false;
			mySkillDef.fullRestockOnAssign = true;
			mySkillDef.interruptPriority = InterruptPriority.Any;
			mySkillDef.isBullets = false;
			mySkillDef.isCombatSkill = true;
			mySkillDef.mustKeyPress = false;
			mySkillDef.noSprint = true;
			mySkillDef.rechargeStock = 1;
			mySkillDef.requiredStock = 1;
			mySkillDef.shootDelay = 0f;
			mySkillDef.stockToConsume = 1;
			mySkillDef.icon = Assets.icon2;
			mySkillDef.skillDescriptionToken = "ASELICA_SECONDARY_CROSSBOW_DESCRIPTION";
			mySkillDef.skillName = "ASELICA_SECONDARY_CROSSBOW_NAME";
			mySkillDef.skillNameToken = "ASELICA_SECONDARY_CROSSBOW_NAME";

			LoadoutAPI.AddSkillDef(mySkillDef);

			component.secondary = characterPrefab.AddComponent<GenericSkill>();
			SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
			newFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(newFamily);
			component.secondary.SetFieldValue("_skillFamily", newFamily);
			SkillFamily skillFamily = component.secondary.skillFamily;

			skillFamily.variants[0] = new SkillFamily.Variant
			{
				skillDef = mySkillDef,
				unlockableName = "",
				viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
			};
		}

		void SwordRainSetup()
		{
			SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

			LanguageAPI.Add("ASELICA_SWORDRAIN_NAME", "Judgment of the Sun");
			LanguageAPI.Add("ASELICA_SWORDRAIN_DESCRIPTION", "It rains " + Assets.applyStyle("200 swords", TextStyle.STACK) + " which deal " + Assets.applyStyle("100% Damage", TextStyle.DAMAGE) + " on hit. In a field of 40x40 meters.");

			// set up your primary skill def here!

			SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
			mySkillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.AselicaStates.SwordRain));
			mySkillDef.activationStateMachineName = "Weapon";
			mySkillDef.baseMaxStock = 1;
			mySkillDef.baseRechargeInterval = 20f;
			mySkillDef.beginSkillCooldownOnSkillEnd = true;
			mySkillDef.canceledFromSprinting = false;
			mySkillDef.fullRestockOnAssign = true;
			mySkillDef.interruptPriority = InterruptPriority.Any;
			mySkillDef.isBullets = false;
			mySkillDef.isCombatSkill = true;
			mySkillDef.mustKeyPress = false;
			mySkillDef.noSprint = true;
			mySkillDef.rechargeStock = 1;
			mySkillDef.requiredStock = 1;
			mySkillDef.shootDelay = 0f;
			mySkillDef.stockToConsume = 1;
			mySkillDef.icon = Assets.icon3;
			mySkillDef.skillDescriptionToken = "ASELICA_SWORDRAIN_DESCRIPTION";
			mySkillDef.skillName = "ASELICA_SWORDRAIN_NAME";
			mySkillDef.skillNameToken = "ASELICA_SWORDRAIN_NAME";

			LoadoutAPI.AddSkillDef(mySkillDef);

			component.utility = characterPrefab.AddComponent<GenericSkill>();
			SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
			newFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(newFamily);
			component.utility.SetFieldValue("_skillFamily", newFamily);
			SkillFamily skillFamily = component.utility.skillFamily;

			skillFamily.variants[0] = new SkillFamily.Variant
			{
				skillDef = mySkillDef,
				unlockableName = "",
				viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
			};
		}

		void BuffSetup()
		{
			SkillLocator component = characterPrefab.GetComponent<SkillLocator>();

			LanguageAPI.Add("ASELICA_BUFF_NAME", "Blessing of the Sun");
			LanguageAPI.Add("ASELICA_BUFF_DESCRIPTION", "Applies the buffs " + Assets.applyStyle("Armor Boost, Meat Regen Boost, Full Crit, Attack Speed On Crit, Cloak Speed, Power Buff and Whip Boost for 10 seconds to herself and all teammates.", TextStyle.UTILITY));

			// set up your primary skill def here!

			SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
			mySkillDef.activationState = new SerializableEntityStateType(typeof(EntityStates.AselicaStates.Buff));
			mySkillDef.activationStateMachineName = "Weapon";
			mySkillDef.baseMaxStock = 1;
			mySkillDef.baseRechargeInterval = 60f;
			mySkillDef.beginSkillCooldownOnSkillEnd = true;
			mySkillDef.canceledFromSprinting = false;
			mySkillDef.fullRestockOnAssign = true;
			mySkillDef.interruptPriority = InterruptPriority.Any;
			mySkillDef.isBullets = false;
			mySkillDef.isCombatSkill = true;
			mySkillDef.mustKeyPress = false;
			mySkillDef.noSprint = true;
			mySkillDef.rechargeStock = 1;
			mySkillDef.requiredStock = 1;
			mySkillDef.shootDelay = 0f;
			mySkillDef.stockToConsume = 1;
			mySkillDef.icon = Assets.icon4;
			mySkillDef.skillDescriptionToken = "ASELICA_BUFF_DESCRIPTION";
			mySkillDef.skillName = "ASELICA_BUFF_NAME";
			mySkillDef.skillNameToken = "ASELICA_BUFF_NAME";

			LoadoutAPI.AddSkillDef(mySkillDef);

			component.special = characterPrefab.AddComponent<GenericSkill>();
			SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
			newFamily.variants = new SkillFamily.Variant[1];
			LoadoutAPI.AddSkillFamily(newFamily);
			component.special.SetFieldValue("_skillFamily", newFamily);
			SkillFamily skillFamily = component.special.skillFamily;

			skillFamily.variants[0] = new SkillFamily.Variant
			{
				skillDef = mySkillDef,
				unlockableName = "",
				viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
			};
		}

		private void CreateDoppelganger()
        {
            // set up the doppelganger for artifact of vengeance here
            // quite simple, gets a bit more complex if you're adding your own ai, but commando ai will do

            doppelganger = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), "AselicaMonsterMaster", true, "C:\\Users\\Sean\\source\\repos\\PlayableAselica\\PlayableAselica\\Aselica.cs", "CreateDoppelganger", 159);

            MasterCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(doppelganger);
            };

            CharacterMaster component = doppelganger.GetComponent<CharacterMaster>();
            component.bodyPrefab = characterPrefab;
        }
    }
}



