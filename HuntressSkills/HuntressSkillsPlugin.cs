using BepInEx;
using R2API;
using R2API.Utils;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using System.IO;

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
        public const string PluginName = "HuntressExtraLoadout";
        public const string PluginVersion = "0.7.0";

        // a prefix for name tokens to prevent conflicts- please capitalize all name tokens for convention
        public const string DEVELOPER_PREFIX = "PBLST";

        public static HuntressSkillsPlugin instance;

        //Load the asset bundle
        public static AssetBundle mainAssets;
        public static string assetBundleFolder = "Assets";
        public static string assetName = "icons";

        public static PluginInfo PInfo { get; private set; }

        void Awake()
        {
            instance = this;

            //UnityEngine.Debug.Log("PlaySound");

            //otro nombre huntress extra loadout
            //assets can be found here:
            // https://xiaoxiao921.github.io/GithubActionCacheTest/assetPathsDump.html

            // Load AssetBundle
            PInfo = Info;
            var assetBundlePath = Path.Combine(Path.GetDirectoryName(HuntressSkillsPlugin.PInfo.Location), assetBundleFolder, assetName);

            mainAssets = AssetBundle.LoadFromFile(assetBundlePath);

            //ruta de mi mod C:\Users\Pablest\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\mod dev\BepInEx\plugins\Unknown-HuntressSkills.dll
            foreach (var name in mainAssets.GetAllAssetNames())
            {
                Debug.Log("Asset: " + name);
            }

            Skills.TranceFire.Initialize(this);
            Skills.SplittingGlaive.Initialize(this);
            Skills.StalkingThePrey.Initialize(this);
            Skills.SwiftManeuver.Initialize(this);
            //damage type .BypassArmor
            
            // make a content pack and add it. this has to be last
            //new Modules.ContentPacks().Initialize();
        }
    }
}