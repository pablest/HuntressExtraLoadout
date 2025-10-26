using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;


namespace HuntressSkills.Skills
{
    public class StalkingThePrey
    {
        public static BuffDef StalkingThePreyFirstHit; //buff that gives 100% crit chance and damage

        public static BuffDef PredatorFocus; //buff that gives crit chance and crit damage
        public static float PredatorFocusCritMultDamage = 0.3f;
        public static float PredatorFocusCritChance = 25f;

        public static void Initialize(HuntressSkillsPlugin pluginInfo)
        {

            GameObject huntressBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab").WaitForCompletion();

            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_NAME", "Stalking The Prey");
            LanguageAPI.Add(HuntressSkillsPlugin.DEVELOPER_PREFIX + "HUNTRESS_UTILITY_STALKINGTHEPREY_DESCRIPTION", $"<style=cIsUtility>Agile</style>. Become <style=cIsUtility>Invisible</style>, make next ability <style=cIsHealth>Critical</style> and gain movement speed until a damage ability is used. Upon use, <style=cIsHealth>increases Critical Damage by 30%</style> and grants <style=cIsHealth>25% Critical Chance</style>.");

            //Create the buffs we are going to do
            CreateBuffs();

            // Now we must create a SkillDef
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            mySkillDef.activationState = new SerializableEntityStateType(typeof(StalkingThePreySkill));
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 8f;
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
            mySkillDef.icon = HuntressSkillsPlugin.mainAssets.LoadAsset<Sprite>("huntress_invis");
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
            StalkingThePreyFirstHit.name = "StalkingThePrey: CritGuaranteed";
            StalkingThePreyFirstHit.buffColor = new Color(255, 255, 255);
            StalkingThePreyFirstHit.isHidden = true;
            StalkingThePreyFirstHit.canStack = false;
            StalkingThePreyFirstHit.isDebuff = false;
            //HuntressInstincts.iconSprite = MainAssets.LoadAsset<Sprite>("NailBombNailCooldownIcon.png");
            ContentAddition.AddBuffDef(StalkingThePreyFirstHit);
            RecalculateStatsAPI.GetStatCoefficients += StalkingThePreyFirstHitStatIncrease;

            // another name FatalPrecision
            PredatorFocus = ScriptableObject.CreateInstance<BuffDef>();
            PredatorFocus.name = "Stalking The Prey";
            PredatorFocus.buffColor = new Color(255, 255, 255);
            PredatorFocus.iconSprite = HuntressSkillsPlugin.mainAssets.LoadAsset<Sprite>("critBuff_orange");
            PredatorFocus.isHidden = false;
            PredatorFocus.canStack = false;
            PredatorFocus.isDebuff = false;

            ContentAddition.AddBuffDef(PredatorFocus);
            RecalculateStatsAPI.GetStatCoefficients += PredatorFocusStatIncrease;
        }

        public class StalkingThePreySkill: BaseSkillState
        {
            public static float buffDuration = 4f;

            public static string enterStealthSound = "Play_bandit2_shift_enter"; //usar el del latigo

            public static GameObject featherEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Feather/FeatherEffect.prefab").WaitForCompletion();

            private Animator animator;

            public override void OnEnter()
            {
                base.OnEnter();
                animator = GetModelAnimator();
                _ = (bool)animator;
                if ((bool)base.characterBody)
                {
                    //Util.PlaySound(enterStealthSound, base.gameObject);
                    GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SprintOutOfCombat/SprintActivate.prefab").WaitForCompletion();
                    //make effect and sound
                    EffectManager.SpawnEffect(prefab, new EffectData
                    {
                        origin = transform.position,
                        rotation = transform.rotation
                    }, true);

                    //add invis buff
                    if (NetworkServer.active)
                    {
                        base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);
                        base.characterBody.AddBuff(RoR2Content.Buffs.CloakSpeed);
                        base.characterBody.AddBuff(StalkingThePreyFirstHit);
                    }

                    //add a function to stop invis and add buff when user use a skill
                    base.characterBody.onSkillActivatedAuthority += OnSkillActivatedRemoveInvis;
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                outer.SetNextStateToMain();
            }

            public override void OnExit()
            {
                base.OnExit();
                On.EntityStates.EntityState.OnExit += RemoveFirstHitBuff;
            }

            private void RemoveFirstHitBuff(On.EntityStates.EntityState.orig_OnExit orig, EntityState self)
            {
                //UnityEngine.Debug.Log("Huntress RemoveFirstHitBuff");
                orig(self);
                
                // If the onExit is a baseSkill
                if (self is BaseState skillState)
                {
                    //UnityEngine.Debug.Log("BaseState");
                    var body = self.characterBody;
                    // and is executed by huntress
                    if ((body != null) & (body == base.characterBody))
                    {
                        //UnityEngine.Debug.Log("BaseState");
                        var characterBody = body.GetComponent<CharacterBody>();
                        if (characterBody.HasBuff(StalkingThePreyFirstHit))
                        {
                            //UnityEngine.Debug.Log("BaseState");
                            characterBody.RemoveBuff(StalkingThePreyFirstHit);
                            On.EntityStates.EntityState.OnExit -= RemoveFirstHitBuff;
                        }
                    }
                }
            }


            private void OnSkillActivatedRemoveInvis(GenericSkill skill)
            {
                //when user atacks, then the invis buff is removed and a damage buff is applied
                if (skill.skillDef.isCombatSkill)
                {

                    //make effect and sound
                    EffectManager.SpawnEffect(featherEffectPrefab, new EffectData
                    {
                        origin = transform.position,
                        rotation = transform.rotation
                    }, true);

                    //animator.SetLayerWeight(animator.GetLayerIndex("Body, StealthWeapon"), 0f);

                    // delete the RemoveFirstHitBuff

                    base.characterBody.RemoveBuff(RoR2Content.Buffs.CloakSpeed);
                    base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);

                    base.characterBody.AddTimedBuff(PredatorFocus, buffDuration);

                    //after that we remove the atack detection to be checked and reset the skill
                    base.characterBody.onSkillActivatedAuthority -= OnSkillActivatedRemoveInvis;
                }
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }
    }
}
