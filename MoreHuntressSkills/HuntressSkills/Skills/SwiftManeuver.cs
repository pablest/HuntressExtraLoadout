using System;
using EntityStates;
using R2API;
using RoR2;
using RoR2.UI;
using RoR2.Skills;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using EntityStates.Huntress;
using EntityStates.Huntress.HuntressWeapon;
using EntityStates.GravekeeperBoss;
using EntityStates.Huntress.Weapon;
using static UnityEngine.ParticleSystem.PlaybackState;
using System.Reflection;
using static Rewired.UI.ControlMapper.ControlMapper;
using static UnityEngine.UI.Image;

namespace HuntressSkills.Skills
{
    class SwiftManeuver 
    {
        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {
            // First we must load our survivor's Body prefab. For this tutorial, we are making a skill for Commando
            // If you would like to load a different survivor, you can find the key for their Body prefab at the following link
            // https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html
            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SPECIAL_SWIFTMANEUVERSKILL_NAME", "Swift Maneuver");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SPECIAL_SWIFTMANEUVERSKILL_DESCRIPTION", $"Fire a boomerang for <style=cIsDamage>300% damage</style>.");


            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(SwiftManeuverSkill));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 2;
            mySkillDef.baseRechargeInterval = 8f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            // For the skill icon, you will have to load a Sprite from your own AssetBundle
            mySkillDef.icon = null;
            mySkillDef.skillDescriptionToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SPECIAL_SWIFTMANEUVERSKILL_DESCRIPTION";
            mySkillDef.skillName = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SPECIAL_SWIFTMANEUVERSKILL_NAME  ";
            mySkillDef.skillNameToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SPECIAL_SWIFTMANEUVERSKILL_NAME";

            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(mySkillDef);

            // Now we add our skill to one of the survivor's skill families
            // You can change component.primary to component.secondary, component.utility and component.special
            SkillLocator skillLocator = huntressBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;

            // If this is an alternate skill, use this code.
            // Here, we add our skill as a variant to the existing Skill Family.
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
        }

       //2 ideas q tp dispares manualmente y blink extra en crit
       //o 3 cargas blink disp auto a donde miras o al visor de huntress criticos devuelven carga
        public class SwiftManeuverSkill : BaseSkillState
        {
            private Transform modelTransform;

            public static float baseFirstBlinkDuration = 0.15f;

            public static float baseShotDuration = 0.3f;

            public static float baseSecondBlinkDuration = 0.15f;

            public static float distanceCoefficient = 5f;

            public static  float damageCoefficient = 7.5f;

            public static float procCoefficient = 1f;

            public static float cooldownReductionOnCrit = 3f;

            public static GameObject blinkPrefab = BaseBeginArrowBarrage.blinkPrefab;

            public static string blinkSoundString = BaseBeginArrowBarrage.blinkSoundString;

            public static string shotMuzzleString;

            public static GameObject shotMuzzleEffectPrefab;

            public static GameObject projectilePrefab = ArrowRain.projectilePrefab;

            public static SkillDef primarySkillDef = AimArrowSnipe.primarySkillDef;

            public static GameObject crosshairOverridePrefab = AimArrowSnipe.crosshairOverridePrefab;

            public HuntressTracker huntressTracker;

            public Vector3 blinkVector = Vector3.zero;

            public float firstBlinkUpForce = 0.3f;

            public float secondBlinkDownForce = 0.3f;

            private float firstBlinkDuration;

            public float shotDuration;

            public float fsDuration;

            private float secondBlinkDuration;

            public float fssDuration;

            public HurtBox huntressTrackerTarget;

            private bool beginShot = false;

            private bool beginSecondBlink = false;

            private CharacterModel characterModel;


            public Vector3 GetUserMovVector()
            {
                return ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
            }

            private void CreateBlinkEffect(Vector3 origin)
            {
                EffectData effectData = new EffectData();
                effectData.rotation = Util.QuaternionSafeLookRotation(blinkVector);
                effectData.origin = origin;
                EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
            }

            public override void OnEnter()
            {
                base.OnEnter();

                Util.PlaySound(blinkSoundString, base.gameObject);
                huntressTracker = GetComponent<HuntressTracker>();
     

                if ((bool)huntressTracker)
                {
                    huntressTrackerTarget = huntressTracker.GetTrackingTarget();
                    huntressTracker.enabled = false;
                }
                modelTransform = GetModelTransform();
                if ((bool)modelTransform)
                {
                    characterModel = modelTransform.GetComponent<CharacterModel>();
                }

                firstBlinkDuration = baseFirstBlinkDuration / attackSpeedStat;
                secondBlinkDuration = baseSecondBlinkDuration / attackSpeedStat;
                shotDuration = baseShotDuration / attackSpeedStat;
                fsDuration = firstBlinkDuration + shotDuration;
                fssDuration = fsDuration + secondBlinkDuration;

                shotMuzzleString = FireHook.muzzleString;
                shotMuzzleEffectPrefab = FireHook.muzzleflashEffectPrefab;

                //PlayAnimation("FullBody, Override", "BeginArrowRain", "BeginArrowRain.playbackRate", firstBlinkDuration);
                if ((bool)base.characterMotor)
                {
                    base.characterMotor.velocity = Vector3.zero;
                }

                base.characterMotor.rootMotion += Vector3.up; //malke little initial hop
                Vector3 up = Vector3.up;
                blinkVector = (GetUserMovVector() + up * firstBlinkUpForce).normalized;
                CreateBlinkEffect(base.transform.position);
                //PlayCrossfade("Body", "ArrowBarrageLoop", 0.1f);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (base.characterMotor)
                {
                    base.characterMotor.velocity = Vector3.zero;
                    base.characterMotor.rootMotion += blinkVector * (moveSpeedStat * distanceCoefficient * Time.fixedDeltaTime);
                }
               
                //shoot the arrows mechanic
                if (base.fixedAge >= firstBlinkDuration && !beginShot)
                {
                    beginShot = true;
                    blinkVector = Vector3.zero;
                    PlayAnimation("Body", "FireArrowSnipe", "FireArrowSnipe.playbackRate", shotDuration);
                    SwiftShot();
                }
                //make the last blink
                if (base.fixedAge >= fsDuration && !beginSecondBlink)
                {
                    Vector3 down = Vector3.down;
                    blinkVector = (GetUserMovVector() + down * secondBlinkDownForce).normalized;
                    CreateBlinkEffect(base.transform.position);
                    beginSecondBlink = true;
                    //outer.SetNextState(new AimArrowSnipe());
                }

                //exit
                if (base.fixedAge >= fssDuration && base.isAuthority)
                {
                    PlayAnimation("Body", "FireArrowSnipe", "FireArrowSnipe.playbackRate", shotDuration);
                    SwiftShot();
                    outer.SetNextStateToMain();
                }
            }

            public override void OnExit()
            {
                CreateBlinkEffect(base.transform.position);
                /*
                modelTransform = GetModelTransform();
                if ((bool)modelTransform)
                {
                    TemporaryOverlay temporaryOverlay = modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay.duration = 0.6f;
                    temporaryOverlay.animateShaderAlpha = true;
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                    temporaryOverlay.AddToCharacerModel(modelTransform.GetComponent<CharacterModel>());
                    TemporaryOverlay temporaryOverlay2 = modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay2.duration = 0.7f;
                    temporaryOverlay2.animateShaderAlpha = true;
                    temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay2.destroyComponentOnEnd = true;
                    temporaryOverlay2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
                    temporaryOverlay2.AddToCharacerModel(modelTransform.GetComponent<CharacterModel>());
                }
                */
                if ((bool)characterModel)
                {
                    characterModel.invisibilityCount--;
                }

                if ((bool)huntressTracker)
                {
                    huntressTracker.enabled = true;
                }


                base.OnExit();
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }

            public void SwiftShot()
            {

                //PlayCrossfade("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", duration, duration * 0.2f / attackSpeedStat);
                //PlayCrossfade("Gesture, Additive", "FireSeekingShot", "FireSeekingShot.playbackRate", duration, duration * 0.2f / attackSpeedStat);

                Ray aimRay = GetAimRay();
                FireArrowSnipe f = new FireArrowSnipe(); // NOTE try to use prefabs instead of instanciate f
                Util.PlayAttackSpeedSound(f.fireSoundString, base.gameObject, attackSpeedStat);

                //2 modes, if enemy is tracked then the sot goes for it, if not, it shot a bullet where the character is looking
                Vector3 aimVector;
                if (huntressTrackerTarget)
                {
                    Vector3 enemyPosition = huntressTrackerTarget.transform.position; // or enemyTransform.position
                    aimVector = (enemyPosition - aimRay.origin).normalized;
                }
                else
                {
                    aimVector = aimRay.direction;
                }

                //rotate character to face the enemy
                Vector3 flatDirection = new Vector3(aimVector.x, 0f, aimRay.origin.z);
                if (flatDirection != Vector3.zero)
                {
                    modelTransform.forward = flatDirection; // instantly snap to direction
                }
                

                base.healthComponent.TakeDamageForce(aimRay.direction * -400f, alwaysApply: true);


                Boolean isCrit = RollCrit();
                //recharge cooldown if crit
                if (isCrit)
                {
                    GenericSkill specialSkill = characterBody.skillLocator.special;
                    if (specialSkill.stock < specialSkill.maxStock)
                    {
                        specialSkill.rechargeStopwatch += cooldownReductionOnCrit;
                        //Ensure the cooldown dont pass the max cooldown
                        if (specialSkill.rechargeStopwatch > specialSkill.skillDef.baseRechargeInterval)
                        {
                            specialSkill.rechargeStopwatch = specialSkill.skillDef.baseRechargeInterval;
                        }
                    }
                }

                BulletAttack bullet = new BulletAttack
                {
                    aimVector = aimVector,
                    origin = aimRay.origin,
                    owner = base.gameObject,
                    weapon = null,
                    bulletCount = (uint)1,
                    damage = damageStat * damageCoefficient,
                    damageColorIndex = DamageColorIndex.Default,
                    falloffModel = BulletAttack.FalloffModel.Buckshot,
                    force = 2000f,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = procCoefficient,
                    maxDistance = 4000f,
                    isCrit = isCrit,
                    muzzleName = f.muzzleName,
                    hitEffectPrefab = f.hitEffectPrefab,
                    spreadPitchScale = f.spreadPitchScale,
                    spreadYawScale = f.spreadYawScale,
                    tracerEffectPrefab = f.tracerEffectPrefab
                };
                bullet.Fire();

            }

            
        }
    }
}
