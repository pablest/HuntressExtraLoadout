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
    public class FireHeavyArrow
    {
        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {
            // First we must load our survivor's Body prefab. For this tutorial, we are making a skill for Commando
            // If you would like to load a different survivor, you can find the key for their Body prefab at the following link
            // https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html
            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_FIREHEAVYARROW_NAME", "Heavy Arrow");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_FIREHEAVYARROW_DESCRIPTION", $"Fire a boomerang for <style=cIsDamage>300% damage</style>.");

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(FireHeavyArrowAttack));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
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
            mySkillDef.skillDescriptionToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_FIREHEAVYARROW_DESCRIPTION";
            mySkillDef.skillName = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_FIREHEAVYARROW_NAME";
            mySkillDef.skillNameToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_FIREHEAVYARROW_NAME";

            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(mySkillDef);

            // Now we add our skill to one of the survivor's skill families
            // You can change component.primary to component.secondary, component.utility and component.special
            SkillLocator skillLocator = huntressBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.primary.skillFamily;

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
        public class FireHeavyArrowAttack : BaseSkillState
        {
            [SerializeField]
            public float orbDamageCoefficient;

            [SerializeField]
            public float orbProcCoefficient;

            [SerializeField]
            public string muzzleString;

            [SerializeField]
            public GameObject muzzleflashEffectPrefab;

            [SerializeField]
            public string attackSoundString;

            [SerializeField]
            public float baseDuration;

            [SerializeField]
            public int maxArrowCount;

            [SerializeField]
            public float baseArrowReloadDuration;

            private float duration;

            protected float arrowReloadDuration;

            private float arrowReloadTimer;

            protected bool isCrit;

            private int firedArrowCount;

            private HurtBox initialOrbTarget;

            private ChildLocator childLocator;

            private HuntressTracker huntressTracker;

            private Animator animator;

            public override void OnEnter()
            {
                base.OnEnter();
                Transform modelTransform = GetModelTransform();
                huntressTracker = GetComponent<HuntressTracker>();
                if ((bool)modelTransform)
                {
                    childLocator = modelTransform.GetComponent<ChildLocator>();
                    animator = modelTransform.GetComponent<Animator>();
                }
                Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
                if ((bool)huntressTracker && base.isAuthority)
                {
                    initialOrbTarget = huntressTracker.GetTrackingTarget();
                }
                duration = baseDuration / attackSpeedStat;
                arrowReloadDuration = baseArrowReloadDuration / attackSpeedStat;
                if ((bool)base.characterBody)
                {
                    base.characterBody.SetAimTimer(duration + 1f);
                }
                PlayCrossfade("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", duration, duration * 0.2f / attackSpeedStat);
                PlayCrossfade("Gesture, Additive", "FireSeekingShot", "FireSeekingShot.playbackRate", duration, duration * 0.2f / attackSpeedStat);
                isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            }

            public override void OnExit()
            {
                base.OnExit();
                FireOrbArrow();
            }

            protected virtual GenericDamageOrb CreateArrowOrb()
            {
                return new HuntressArrowOrb();
            }

            private void FireOrbArrow()
            {
                if (firedArrowCount < maxArrowCount && !(arrowReloadTimer > 0f) && NetworkServer.active)
                {
                    firedArrowCount++;
                    arrowReloadTimer = arrowReloadDuration;
                    GenericDamageOrb genericDamageOrb = CreateArrowOrb();
                    genericDamageOrb.damageValue = base.characterBody.damage * orbDamageCoefficient;
                    genericDamageOrb.isCrit = isCrit;
                    genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                    genericDamageOrb.attacker = base.gameObject;
                    genericDamageOrb.procCoefficient = orbProcCoefficient;
                    HurtBox hurtBox = initialOrbTarget;
                    if ((bool)hurtBox)
                    {
                        Transform transform = childLocator.FindChild(muzzleString);
                        EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
                        genericDamageOrb.origin = transform.position;
                        genericDamageOrb.target = hurtBox;
                        OrbManager.instance.AddOrb(genericDamageOrb);
                    }
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                arrowReloadTimer -= Time.fixedDeltaTime;
                if (animator.GetFloat("FireSeekingShot.fire") > 0f)
                {
                    FireOrbArrow();
                }
                if (base.fixedAge > duration && base.isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }

            public override void OnSerialize(NetworkWriter writer)
            {
                writer.Write(HurtBoxReference.FromHurtBox(initialOrbTarget));
            }

            public override void OnDeserialize(NetworkReader reader)
            {
                initialOrbTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
            }
        }
    }
}