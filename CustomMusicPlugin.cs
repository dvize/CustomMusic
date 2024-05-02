using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CustomMusic
{
    [BepInPlugin("com.dvize.CustomMusicPlayer", "dvize.CustomMusicPlayer", "1.3.0")]
    public class CustomMusicPlugin : BaseUnityPlugin
    {
        public static ManualLogSource logger = null;
        public static ConfigEntry<int> SongLoadAmount
        {
            get; set;
        }

        private void Awake()
        {
            logger = Logger;
            SongLoadAmount = Config.Bind(
                "Song Loading Configuration", 
                "Song Load Amount", 
                5, 
                new ConfigDescription("Sets the amount of songs to load on Game Start or Raid End. Yes it is randomized. More Songs = more time waiting",
                null));

            //new CustomMusicComponent().Enable();
            new EndScreenLoadPatch().Enable();
            new HideoutScreenLoadPatch().Enable();

            Harmony.CreateAndPatchAll(typeof(CustomMusicComponent));
        }
    }
}
