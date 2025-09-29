using BepInEx;
using R2API;
using R2API.Utils;
using System.Security;
using System.Security.Permissions;
 
//rename this namespace
namespace HuntressSkills
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HuntressSkillsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pablest";
        public const string PluginName = "HuntressSkills";
        public const string PluginVersion = "0.5.0";

        // a prefix for name tokens to prevent conflicts- please capitalize all name tokens for convention
        public const string DEVELOPER_PREFIX = "PBLST";

        public static HuntressSkillsPlugin instance;

        void Awake()
        {
            instance = this;

            //easy to use logger
            Log.Init(Logger);

            //or
            //UnityEngine.Debug.Log("PlaySound");

            Skills.FireHeavyArrow.Initialize(this);
            Skills.SplittingGlaive.Initialize(this);
            Skills.StalkingThePrey.Initialize(this);
            Skills.SwiftManeuver.Initialize(this);
            //damage type .BypassArmor
            // make a content pack and add it. this has to be last
            //new Modules.ContentPacks().Initialize();
        }
    }
}