using System;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace HuntressSkills.Skills
{
    public class TranceFire 
    {
        public static BuffDef TranceBuff; //buff that gives 100% crit chance and damage

        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {
            
            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_TRANCEFIRE_NAME", "Trance Fire");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_TRANCEFIRE_DESCRIPTION", $"<style=cIsUtility>Agile</style>. Fire a seeking arrow for <style=cIsDamage>100% damage</style>. Consecutive shots without taking damage grant <style=cIsDamage>Focus</style>, increasing attack speed up to <style=cIsUtility>10</style> stacks. At max <style=cIsDamage>Focus</style>, fire <style=cIsDamage>2</style> arrows.");

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<HuntressTargetSkillDef>();

            //Create the buffs we are going to do
            CreateBuff();
            
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
            mySkillDef.icon = HuntressSkillsPlugin.mainAssets.LoadAsset<Sprite>("strafe_orange");
            mySkillDef.skillDescriptionToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_TRANCEFIRE_DESCRIPTION";
            mySkillDef.skillName = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_TRANCEFIRE_NAME";
            mySkillDef.skillNameToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_PRIMARY_TRANCEFIRE_NAME";

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

        public static void CreateBuff()
        {
            //buff that affects the TranceFire ability
            TranceBuff = ScriptableObject.CreateInstance<BuffDef>();
            TranceBuff.name = "Huntress Focus";
            TranceBuff.iconSprite = HuntressSkillsPlugin.mainAssets.LoadAsset<Sprite>("huntress_trance_buff");

            TranceBuff.isHidden = false;
            TranceBuff.canStack = true;
            TranceBuff.isDebuff = false;
            ContentAddition.AddBuffDef(TranceBuff);
            //Delete the buff if the user is hitten by an enemy
            On.RoR2.GlobalEventManager.OnHitEnemy += OnHitTranceBuffElimination;

        }

        public static void OnHitTranceBuffElimination(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            //if the victim has the buff, remove all stacks
            orig(self, damageInfo, victim);

            if (victim)
            {
                var body = victim.GetComponent<CharacterBody>();
                if (body && body.HasBuff(TranceBuff))
                {
                    body.ClearTimedBuffs(TranceBuff);
                }
            }
            
        }


        public class HuntressTargetSkillDef : SkillDef
        {
            public override bool CanExecute(GenericSkill skillSlot)
            {
                base.CanExecute(skillSlot);

                if (base.CanExecute(skillSlot))
                {
                    var body = skillSlot.characterBody;
                    if (body)
                    {
                        var tracker = body.GetComponent<HuntressTracker>();
                        return tracker && (tracker.GetTrackingTarget() != null);
                    }
                }
                return false;
            }
        }

        public class FireHeavyArrowAttack : BaseSkillState
        {
            public static float orbDamageCoefficient = 1.0f;

            public static float orbProcCoefficient = 1.0f;

            public static float baseDuration = 0.75f;

            public static float durationReductionPerBuff = 0.05f;

            private static float buffDuration = 3.5f;

            private static int maxBuffStack = 10;

            private float duration;

            protected bool isCrit;

            private int firedArrowCount = 0;

            private int numArrows = 1;

            private HurtBox initialOrbTarget;

            private HuntressTracker huntressTracker;

            private Animator animator;


            public override void OnEnter()
            {
                base.OnEnter();
          
                Transform modelTransform = GetModelTransform();
                huntressTracker = GetComponent<HuntressTracker>();
                initialOrbTarget = huntressTracker.GetTrackingTarget();
                if (!(bool)modelTransform || !(bool)initialOrbTarget) {return;}

                //childLocator = modelTransform.GetComponent<ChildLocator>();
                animator = modelTransform.GetComponent<Animator>();
                if (!(bool)huntressTracker && base.isAuthority){return;}

                int tranceBuffCount = characterBody.GetBuffCount(TranceBuff);
                duration = (baseDuration - (durationReductionPerBuff * tranceBuffCount)) / attackSpeedStat;
                if (tranceBuffCount >= maxBuffStack) {numArrows = 2;}
                isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);

                if ((bool)base.characterBody)
                {
                    base.characterBody.SetAimTimer(duration + 1f);
                }
                PlayCrossfade("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", duration, duration);
                
            }

            public override void OnExit()
            {
                base.OnExit();
            }

            protected virtual GenericDamageOrb CreateArrowOrb()
            {
                return new HuntressArrowOrb();
            }

            private void FireOrbArrow()
            {
                if (firedArrowCount < numArrows && NetworkServer.active)
                {
                    //apply buff, if crit, apply twice, until max 10
                    //for doing that and to refresh previously buff duration
                    int tranceBuffCount = characterBody.GetBuffCount(TranceBuff);
                    characterBody.ClearTimedBuffs(TranceBuff);

                    for (int i = 0; i < tranceBuffCount; i++)
                    {
                        characterBody.AddTimedBuff(TranceBuff, buffDuration, maxBuffStack);
                    }


                    if (isCrit){characterBody.AddTimedBuff(TranceBuff, buffDuration, maxBuffStack);
                        characterBody.AddTimedBuff(TranceBuff, buffDuration, maxBuffStack);}
                    else{characterBody.AddTimedBuff(TranceBuff, buffDuration, maxBuffStack);}

                    //UnityEngine.Debug.Log("Cantidad buff");
                    //UnityEngine.Debug.Log(tranceBuffCount);
                    firedArrowCount++;  

                    GenericDamageOrb genericDamageOrb = CreateArrowOrb();
                    genericDamageOrb.damageValue = base.characterBody.damage * orbDamageCoefficient;
                    genericDamageOrb.damageType = DamageType.Generic;
                    genericDamageOrb.isCrit = isCrit;
                    genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                    genericDamageOrb.attacker = base.gameObject;
                    genericDamageOrb.procCoefficient = orbProcCoefficient;
                    //genericDamageOrb.speed = 0.05f;
                    //genericDamageOrb.scale = 50f; 
                    HurtBox hurtBox = initialOrbTarget;

                    if ((bool)hurtBox)
                    {
                        //Transform transform = childLocator.FindChild(muzzleString);
                        //EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
                        genericDamageOrb.origin = transform.position;
                        genericDamageOrb.target = hurtBox;
                        OrbManager.instance.AddOrb(genericDamageOrb);
                    }
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();

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

        }
    }
}