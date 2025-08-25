using System;
using BepInEx;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.Huntress.HuntressWeapon;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;

//para hacerlo sencillito, habilidad de glaive q rebota, al rebotar se divide en 2, hasta 2 veces. las kills hace q no se dividan las cosas
namespace HuntressSkills.Skills
{
    public class SplittingGlaive
    {
        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {
            // First we must load our survivor's Body prefab. For this tutorial, we are making a skill for Commando
            // If you would like to load a different survivor, you can find the key for their Body prefab at the following link
            // https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html
            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SECONDARY_SPLITTINGGLAIVE_NAME", "Splitting Glaive");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SECONDARY_SPLITTINGGLAIVE_DESCRIPTION", $"Fire a boomerang for <style=cIsDamage>300% damage</style>.");

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(SplittingGlaiveAttack));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 7f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = true;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            // For the skill icon, you will have to load a Sprite from your own AssetBundle
            mySkillDef.icon = null;
            mySkillDef.skillDescriptionToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SECONDARY_SPLITTINGGLAIVE_DESCRIPTION";
            mySkillDef.skillName = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SECONDARY_SPLITTINGGLAIVE_NAME";
            mySkillDef.skillNameToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_SECONDARY_SPLITTINGGLAIVE_NAME";

            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(mySkillDef);

            // Now we add our skill to one of the survivor's skill families
            // You can change component.primary to component.secondary, component.utility and component.special
            SkillLocator skillLocator = huntressBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.secondary.skillFamily;

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

        public class SplittingGlaiveAttack : BaseSkillState
        {
            public float baseDuration = 1f;
            private float duration;
            public static float smallHopStrength = 1f;
            public static float antigravityStrength = 20f;

            public static float damageCoefficient = 4.8f;
            public static float damageCoefficientPerBounce = 0.7f;
            public static float glaiveProcCoefficient = 0.8f;
            public static int maxBounceCount = 3;
            public static float glaiveTravelSpeed = 45f;
            public static float glaiveBounceRange = 30f;

            public static GameObject chargePrefab = ThrowGlaive.chargePrefab;
            public static GameObject muzzleFlashPrefab = ThrowGlaive.muzzleFlashPrefab;
            private GameObject chargeEffect;
            private Animator animator;
            private float stopwatch;
            private HuntressTracker huntressTracker;
            private Transform modelTransform;
            private HurtBox initialOrbTarget;
            private ChildLocator childLocator;
            private bool hasTriedToThrowGlaive = false;
            private bool hasSuccessfullyThrownGlaive = false;


            public override void OnEnter()
            {
                base.OnEnter();
                stopwatch = 0f;
                duration = baseDuration / attackSpeedStat;
                modelTransform = GetModelTransform();
                animator = GetModelAnimator();
                huntressTracker = GetComponent<HuntressTracker>();
                Util.PlaySound(ThrowGlaive.attackSoundString, base.gameObject);
                if ((bool)huntressTracker && base.isAuthority)
                {
                    initialOrbTarget = huntressTracker.GetTrackingTarget();
                }
                if ((bool)base.characterMotor && smallHopStrength != 0f)
                {
                    base.characterMotor.velocity.y = smallHopStrength;
                }
                PlayAnimation("FullBody, Override", "ThrowGlaive", "ThrowGlaive.playbackRate", duration);
                if ((bool)modelTransform)
                {
                    childLocator = modelTransform.GetComponent<ChildLocator>();
                    if ((bool)childLocator)
                    {
                        Transform transform = childLocator.FindChild("HandR");
                        if ((bool)transform && (bool)chargePrefab)
                        {
                            chargeEffect = UnityEngine.Object.Instantiate(chargePrefab, transform.position, transform.rotation);
                            chargeEffect.transform.parent = transform;
                        }
                    }
                }
                if ((bool)base.characterBody)
                {
                    base.characterBody.SetAimTimer(duration);
                }
            }

            //This method runs once at the end
            //Here, we are doing nothing
            public override void OnExit()
            {
                base.OnExit();
                if ((bool)chargeEffect)
                {
                    EntityState.Destroy(chargeEffect);
                }
                int layerIndex = animator.GetLayerIndex("Impact");
                if (layerIndex >= 0)
                {
                    animator.SetLayerWeight(layerIndex, 1.5f);
                    animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
                }
                if (!hasTriedToThrowGlaive)
                {
                    FireOrbGlaive();
                }
                if (!hasSuccessfullyThrownGlaive && NetworkServer.active)
                {
                    base.skillLocator.secondary.AddOneStock();
                }
            }

            //FixedUpdate() runs almost every frame of the skill
            //Here, we end the skill once it exceeds its intended duration
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                stopwatch += Time.fixedDeltaTime;
                if (!hasTriedToThrowGlaive && animator.GetFloat("ThrowGlaive.fire") > 0f)
                {
                    if ((bool)chargeEffect)
                    {
                        EntityState.Destroy(chargeEffect);
                    }
                    FireOrbGlaive();
                }
                base.characterMotor.velocity.y += antigravityStrength * Time.fixedDeltaTime * (1f - stopwatch / duration);
                if (stopwatch >= duration && base.isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }

            private void FireOrbGlaive()
            {
                if (NetworkServer.active && !hasTriedToThrowGlaive)
                {
                    hasTriedToThrowGlaive = true;
                    DividingGlaiveOrb divLightningOrb = new DividingGlaiveOrb();
                    divLightningOrb.damageValue = base.characterBody.damage * damageCoefficient;
                    divLightningOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
                    divLightningOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                    divLightningOrb.attacker = base.gameObject;
                    divLightningOrb.procCoefficient = glaiveProcCoefficient;
                    divLightningOrb.bouncesRemaining = maxBounceCount;
                    divLightningOrb.speed = glaiveTravelSpeed;
                    divLightningOrb.bouncedObjects = new List<HealthComponent>();
                    divLightningOrb.range = glaiveBounceRange;
                    divLightningOrb.damageCoefficientPerBounce = damageCoefficientPerBounce;
                    HurtBox hurtBox = initialOrbTarget;

                    if ((bool)hurtBox)
                    {
                        hasSuccessfullyThrownGlaive = true;
                        Transform transform = childLocator.FindChild("HandR");
                        EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, "HandR", transmit: true);
                        divLightningOrb.origin = transform.position;
                        divLightningOrb.target = hurtBox;
                        OrbManager.instance.AddOrb(divLightningOrb);
                    }
                }
            }


            //GetMinimumInterruptPriority() returns the InterruptPriority required to interrupt this skill
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }

        public class DividingGlaiveOrb : Orb
        {

            public float speed = 100f;

            public int glaiveSplitCount = 2;

            public float damageValue;

            public GameObject attacker;

            public GameObject inflictor;

            public int bouncesRemaining;

            public List<HealthComponent> bouncedObjects;

            public TeamIndex teamIndex;

            public bool isCrit;

            public ProcChainMask procChainMask;

            public float procCoefficient = 1f;

            public DamageColorIndex damageColorIndex;

            public float range = 40f;

            public float damageCoefficientPerBounce = 0.75f;

            public DamageType damageType;

            private bool canBounceOnSameTarget;

            private BullseyeSearch search;

            public static event Action<LightningOrb> onLightningOrbKilledOnAllBounces;

            public static string path = "Prefabs/Effects/OrbEffects/HuntressGlaiveOrbEffect";

            public float currentReductionRatio = 1f;

            public float reductionPerBounce = 0.5f;

            public override void Begin()
            {
                base.duration = 0.1f;

                base.duration = base.distanceToTarget / speed;
                canBounceOnSameTarget = true;
                EffectData effectData = new EffectData
                {
                    origin = origin,
                    genericFloat = base.duration,
                    scale = currentReductionRatio

                };
                effectData.SetHurtBoxReference(target);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>(path), effectData, transmit: true);
            }

            public override void OnArrival()
            {
                if (!target)
                {
                    return;
                }
                HealthComponent healthComponent = target.healthComponent;
                if ((bool)healthComponent)
                {
                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = damageValue;
                    damageInfo.attacker = attacker;
                    damageInfo.inflictor = inflictor;
                    damageInfo.force = Vector3.zero;
                    damageInfo.crit = isCrit;
                    damageInfo.procChainMask = procChainMask;
                    damageInfo.procCoefficient = procCoefficient;
                    damageInfo.position = target.transform.position;
                    damageInfo.damageColorIndex = damageColorIndex;
                    damageInfo.damageType = damageType;
                    healthComponent.TakeDamage(damageInfo);
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                }
                bool failedToKill = !healthComponent || healthComponent.alive;
                if (bouncesRemaining > 0)
                {
                    if (bouncedObjects != null)
                    {
                        bouncedObjects.Clear();
                        bouncedObjects.Add(target.healthComponent);

                    }
                    for (int divisionIndex = 0; divisionIndex < glaiveSplitCount; divisionIndex++)
                    {
                        HurtBox hurtBox = PickNextTarget(target.transform.position);
                        if ((bool)hurtBox)
                        {
                            DividingGlaiveOrb lightningOrb = new DividingGlaiveOrb();
                            lightningOrb.search = search;
                            lightningOrb.origin = target.transform.position;
                            lightningOrb.target = hurtBox;
                            lightningOrb.attacker = attacker;
                            lightningOrb.inflictor = inflictor;
                            lightningOrb.teamIndex = teamIndex;
                            lightningOrb.damageValue = damageValue * damageCoefficientPerBounce;
                            lightningOrb.bouncesRemaining = bouncesRemaining - 1;
                            lightningOrb.isCrit = isCrit;
                            lightningOrb.bouncedObjects = bouncedObjects;
                            lightningOrb.procChainMask = procChainMask;
                            lightningOrb.procCoefficient = procCoefficient;
                            lightningOrb.damageColorIndex = damageColorIndex;
                            lightningOrb.damageCoefficientPerBounce = damageCoefficientPerBounce;
                            lightningOrb.speed = speed;
                            lightningOrb.range = range;
                            lightningOrb.damageType = damageType;
                            lightningOrb.currentReductionRatio = currentReductionRatio * reductionPerBounce;
                            lightningOrb.reductionPerBounce = reductionPerBounce;
                            OrbManager.instance.AddOrb(lightningOrb);
                        }
                    }
                    
                }
            }

            public HurtBox PickNextTarget(Vector3 position)
            {
                if (search == null)
                {
                    search = new BullseyeSearch();
                }
                search.searchOrigin = position;
                search.searchDirection = Vector3.zero;
                search.teamMaskFilter = TeamMask.allButNeutral;
                search.teamMaskFilter.RemoveTeam(teamIndex);
                search.filterByLoS = false;
                search.sortMode = BullseyeSearch.SortMode.Distance;
                search.maxDistanceFilter = range;
                search.RefreshCandidates();
                HurtBox hurtBox = (from v in search.GetResults()
                                   where !bouncedObjects.Contains(v.healthComponent)
                                   select v).FirstOrDefault();
                if ((bool)hurtBox)
                {
                    bouncedObjects.Add(hurtBox.healthComponent);
                }
                return hurtBox;
            }
        }
    }
}
