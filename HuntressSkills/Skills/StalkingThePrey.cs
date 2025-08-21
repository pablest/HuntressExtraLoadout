using System;
using BepInEx;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using EntityStates.Bandit2;

namespace HuntressSkills.Skills
{
    public class StalkingThePrey
    {
        public static BuffDef StalkingThePreyFirstHit; //buff that gives 100% crit chance and damage
        public static float StalkingThePreyFirstHitMultBuff = 0.3f;


        public static BuffDef PredatorFocus; //buff that gives crit chance and crit damage
        public static float PredatorFocusCritMultDamage = 0.5f;
        public static float PredatorFocusCritChance = 30f;

        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {
            // First we must load our survivor's Body prefab. For this tutorial, we are making a skill for Commando
            // If you would like to load a different survivor, you can find the key for their Body prefab at the following link
            // https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html
            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_NAME", "Stalking The Prey");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_DESCRIPTION", $"Fire a boomerang for <style=cIsDamage>300% damage</style>.");

            //Create the buffs we are going to do
            CreateBuffs();

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(StalkingThePreySkill));
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 7f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            // For the skill icon, you will have to load a Sprite from your own AssetBundle
            mySkillDef.icon = null;
            mySkillDef.skillDescriptionToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_DESCRIPTION";
            mySkillDef.skillName = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_NAME";
            mySkillDef.skillNameToken = HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_NAME";

            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(mySkillDef);

            // Now we add our skill to one of the survivor's skill families
            // You can change component.primary to component.secondary, component.utility and component.special
            SkillLocator skillLocator = huntressBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.utility.skillFamily;

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


        public static void StalkingThePreyFirstHitStatIncrease(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (characterBody.HasBuff(StalkingThePreyFirstHit))
            {
                args.damageMultAdd += StalkingThePreyFirstHitMultBuff;
                args.critAdd += 100f; 
            }
        }

        public static void PredatorFocusStatIncrease(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (characterBody.HasBuff(PredatorFocus))
            {
                args.critDamageMultAdd += PredatorFocusCritMultDamage;
                args.critAdd += PredatorFocusCritChance;
            }
        }

        public static void CreateBuffs()
        {
            //buff that gives 100% crit chance and damage
            StalkingThePreyFirstHit = ScriptableObject.CreateInstance<BuffDef>();
            StalkingThePreyFirstHit.name = "HuntressSkills: Stalking The Prey First Hit Buff";
            StalkingThePreyFirstHit.buffColor = new Color(255, 255, 255);
            StalkingThePreyFirstHit.isHidden = true;
            StalkingThePreyFirstHit.canStack = false;
            StalkingThePreyFirstHit.isDebuff = false;
            //HuntressInstincts.iconSprite = MainAssets.LoadAsset<Sprite>("NailBombNailCooldownIcon.png");
            ContentAddition.AddBuffDef(StalkingThePreyFirstHit);
            RecalculateStatsAPI.GetStatCoefficients += StalkingThePreyFirstHitStatIncrease;

            // another name FatalPrecision
            PredatorFocus = ScriptableObject.CreateInstance<BuffDef>();
            PredatorFocus.name = "HuntressSkills: Predator Focus Buff";
            PredatorFocus.buffColor = new Color(255, 255, 255);
            PredatorFocus.isHidden = false;
            PredatorFocus.canStack = false;
            PredatorFocus.isDebuff = false;
            //PredatorFocus.iconSprite = MainAssets.LoadAsset<Sprite>("NailBombNailCooldownIcon.png");

            ContentAddition.AddBuffDef(PredatorFocus);
            RecalculateStatsAPI.GetStatCoefficients += PredatorFocusStatIncrease;
        }

        public class StalkingThePreySkill: BaseSkillState
        {
            public static float buffDuration = 5f;

            public static string enterStealthSound = StealthMode.enterStealthSound;

            public static string exitStealthSound = StealthMode.exitStealthSound;

            private Animator animator;

            protected Boolean skillUsedAfterInvis = false; //variable used for making next skill after invis deal more damage and the damage is buff not consumed for an instace of damage dealt before the skill is used //this will need some fix in the future to ensure only the skill damage is buffed

            public override void OnEnter()
            {
                base.OnEnter();
                animator = GetModelAnimator();
                _ = (bool)animator;
                if ((bool)base.characterBody)
                {
                    //add invis buff
                    if (NetworkServer.active)
                    {
                        base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
                        base.characterBody.AddBuff(RoR2Content.Buffs.CloakSpeed);
                        base.characterBody.AddBuff(StalkingThePreyFirstHit);
                        skillUsedAfterInvis = false;
                    }

                    //add a function to stop invis and add buff when user use a skill
                    base.characterBody.onSkillActivatedAuthority += OnSkillActivatedRemoveInvis;
                    //add a function to remove the first hit buff when an enemy recieve damage from the atacker
                    On.RoR2.GlobalEventManager.OnHitEnemy += OnHitEnemyRemoveFirstHitBuff;


                }
                Util.PlaySound(enterStealthSound, base.gameObject);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                outer.SetNextStateToMain();
            }

            public override void OnExit()
            {
                base.OnExit();
            }

            private void OnSkillActivatedRemoveInvis(GenericSkill skill)
            {
                Util.PlaySound(exitStealthSound, base.gameObject);
                //animator.SetLayerWeight(animator.GetLayerIndex("Body, StealthWeapon"), 0f);

                //when user atacks, then the invis buff is removed and a damage buff is applied
                if (skill.skillDef.isCombatSkill)
                {

                    base.characterBody.RemoveBuff(RoR2Content.Buffs.CloakSpeed);
                    base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);

                    base.characterBody.AddTimedBuff(PredatorFocus, buffDuration);
                    skillUsedAfterInvis = true;

                    //after that we remove the atack detection to be checked and reset the skill
                    base.characterBody.onSkillActivatedAuthority -= OnSkillActivatedRemoveInvis;
                }
            }

            private void OnHitEnemyRemoveFirstHitBuff(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
            {
                var attacker = damageInfo.attacker;
                if (attacker & skillUsedAfterInvis) 
                {
                    var characterBody = attacker.GetComponent<CharacterBody>();
                    if (characterBody.HasBuff(StalkingThePreyFirstHit)){
                        characterBody.RemoveBuff(StalkingThePreyFirstHit);
                    }
                }
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }
    }
}
