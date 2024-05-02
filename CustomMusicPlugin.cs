using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CustomMusic
{
    [BepInPlugin("com.dvize.CustomMusicPlayer", "dvize.CustomMusicPlayer", "1.3.0")]
    public class CustomMusicPlugin : BaseUnityPlugin
    {
        public static ManualLogSource logger = null;

        private void Awake()
        {
            logger = Logger;

            //new CustomMusicComponent().Enable();
            new EndScreenLoadPatch().Enable();
            new HideoutScreenLoadPatch().Enable();

            Harmony.CreateAndPatchAll(typeof(CustomMusicComponent));
        }
    }
}
