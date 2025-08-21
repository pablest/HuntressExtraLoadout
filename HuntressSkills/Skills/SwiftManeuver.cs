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
using System.Collections.Generic;
using System.Linq;
using EntityStates.Huntress;

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
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 14f;
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

        public class SwiftManeuverSkill : BaseSkillState
        {
            private Transform modelTransform;

            public float firstBlinkDuration = 0.3f;

            public float shotDuration;

            public float secondBlinkDuration = 0.3f;

            public float jumpCoefficient = 12f;

            public static GameObject blinkPrefab = BaseBeginArrowBarrage.blinkPrefab;

            public static string blinkSoundString = BaseBeginArrowBarrage.blinkSoundString;

            [SerializeField]
            public Vector3 blinkVector;

            private Vector3 worldBlinkVector;

            private float prepDuration;

            private bool beginBlink;

            private CharacterModel characterModel;

            private HurtBoxGroup hurtboxGroup;

            protected CameraTargetParams.AimRequest aimRequest;

            public override void OnEnter()
            {
                base.OnEnter();
                Util.PlaySound(blinkSoundString, base.gameObject);
                modelTransform = GetModelTransform();
                if ((bool)modelTransform)
                {
                    characterModel = modelTransform.GetComponent<CharacterModel>();
                    hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
                }
                prepDuration = firstBlinkDuration / attackSpeedStat;
                PlayAnimation("FullBody, Override", "BeginArrowRain", "BeginArrowRain.playbackRate", prepDuration);
                if ((bool)base.characterMotor)
                {
                    base.characterMotor.velocity = Vector3.zero;
                }
                if ((bool)base.cameraTargetParams)
                {
                    aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
                }
                Vector3 direction = GetAimRay().direction;
                direction.y = 0f;
                direction.Normalize();
                Vector3 up = Vector3.up;
                worldBlinkVector = Matrix4x4.TRS(base.transform.position, Util.QuaternionSafeLookRotation(direction, up), new Vector3(1f, 1f, 1f)).MultiplyPoint3x4(blinkVector) - base.transform.position;
                worldBlinkVector.Normalize();
            }

            private void CreateBlinkEffect(Vector3 origin)
            {
                /*
                EffectData effectData = new EffectData();
                effectData.rotation = Util.QuaternionSafeLookRotation(worldBlinkVector);
                effectData.origin = origin;
                EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
                */
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                /*
                if (base.fixedAge >= prepDuration && !beginBlink)
                {
                    beginBlink = true;
                    CreateBlinkEffect(base.transform.position);
                    if ((bool)characterModel)
                    {
                        characterModel.invisibilityCount++;
                    }
                    if ((bool)hurtboxGroup)
                    {
                        HurtBoxGroup hurtBoxGroup = hurtboxGroup;
                        int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
                        hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
                    }
                }
                if (beginBlink && (bool)base.characterMotor)
                {
                    base.characterMotor.velocity = Vector3.zero;
                    base.characterMotor.rootMotion += worldBlinkVector * (base.characterBody.jumpPower * jumpCoefficient * Time.fixedDeltaTime);
                }
                if (base.fixedAge >= shotDuration + prepDuration && base.isAuthority)
                {
                    outer.SetNextState(new AimArrowSnipe());
                }
                */
            }

            public override void OnExit()
            {
                /*
                CreateBlinkEffect(base.transform.position);
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
                if ((bool)characterModel)
                {
                    characterModel.invisibilityCount--;
                }
                if ((bool)hurtboxGroup)
                {
                    HurtBoxGroup hurtBoxGroup = hurtboxGroup;
                    int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
                    hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
                }
                aimRequest?.Dispose();
                */
                base.OnExit();
            }
        }
    }
}
