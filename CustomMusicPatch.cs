using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using DG.Tweening;
using EFT;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;


namespace CustomMusic
{
    public class TempCustomMusicComponent : ModulePatch
    {
        internal static string CurrDir
        {
            get; set;
        }
        internal static List<AudioClip> musicClips = new List<AudioClip>();
        internal static string[] files;
        internal TempCustomMusicComponent()
        {
            CurrDir = BepInEx.Paths.PluginPath + "/CustomMusic";
            files = Directory.GetFiles(CurrDir + "/music");
        }

        static async Task<AudioClip> LoadClip(string path)
        {
            AudioClip clip = null;
            Logger.LogInfo("uhmm");
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
            {
                uwr.SendWebRequest();

                // wrap tasks in try/catch, otherwise it'll fail silently
                try
                {
                    while (!uwr.isDone)
                    {

                        Logger.LogInfo("g");
                        await Task.Delay(5);
                    }

                    if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Debug.Log($"{err.Message}, {err.StackTrace}");
                }
            }

            Logger.LogInfo("REEEE");
            return clip;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), "method_1");
        }

        [PatchPostfix]
        private static void PatchPostfix(ref AudioClip[] ___audioClip_0, object __instance)
        {
            var random = new System.Random();

            var randomSongs = files.OrderBy(x => random.Next()).Take(1).ToArray();

            ConcurrentBag<AudioClip> audioClips = new ConcurrentBag<AudioClip>();
            Parallel.For(0, randomSongs.Length,
                async i =>
                {
                    string url = "file:///" + randomSongs[i].Replace("\\", "/");
                    Logger.LogInfo(url);
                    AudioClip song = await LoadClip(url);
                    Logger.LogInfo($"got song from await! {song.name} asd");
                    if (song == null)
                    {
                        Logger.LogInfo("Something went wrong, song is null!");
                    }
                    else
                    {
                        audioClips.Add(song);
                        Logger.LogInfo("adding song!");
                    }
                }
           );

            Logger.LogInfo("Ended!");

            //assume replacing original
            ___audioClip_0 = audioClips.ToArray();
        }
    }

    public class CustomMusicComponent
    {
        class Song
        {
            public string SongPath;
            public int Frequency;
            public Song(string songPath, int freq)
            {
                this.SongPath = songPath; this.Frequency = freq;
            }
        }

        static Random rand;
        static List<Song> songs = new List<Song>();

        static void InitializeSongShuffler()
        {
            rand = new Random();
            var files = Directory.GetFiles(BepInEx.Paths.PluginPath + "/CustomMusic" + "/music");
            foreach (var song in files)
            {
                songs.Add(new Song(song, 0));
            }
        }

        static IEnumerator PlaySong(GUISounds instance)
        {
            // If nextSong is null, allocate and load the song
            instance.method_4();
            // load and play the song

            var sortedSongs = songs.OrderBy(x => x.Frequency);
            yield return null;
            var onlyLowFreq = sortedSongs.Where(x => x.Frequency == songs[0].Frequency);
            yield return null;

            Song song = onlyLowFreq.OrderBy(x => rand.Next()).First();
            yield return null;

            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(song.SongPath, AudioType.MPEG))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }



            Traverse audioSource_3 = Traverse.Create(instance).Field("audioSource_3");
            audioSource_3.GetValue<AudioSource>().clip = clip;
            audioSource_3.GetValue<AudioSource>().Play();

            Traverse<Coroutine> coroutine_0 = Traverse.Create(instance).Field<Coroutine>("coroutine_0");
            coroutine_0.Value = StaticManager.Instance.WaitSeconds(clip.length, new Action(instance.method_3));

        }

        [HarmonyPatch(typeof(GUISounds), "method_1")]
        [HarmonyPostfix]
        static void OnMusicLoad(ref AudioClip[] ___audioClip_0)
        {
            ___audioClip_0 = null;
            InitializeSongShuffler();
        }

        [HarmonyPatch(typeof(GUISounds), "method_3")]
        [HarmonyPrefix]
        static void ChooseNextSong(GUISounds __instance, ref bool __runOriginal)
        {
            // Skip running the original method
            __runOriginal = false;
            IEnumerator playSong = PlaySong(__instance);
            __instance.StartCoroutine(playSong);
        }
    }

    public class EndScreenLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TarkovApplication), "method_46");
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {

            //assume replacing original
            var guisounds = Singleton<GUISounds>.Instance;

            //force reload by invoking method_1 of guisounds
            Logger.LogInfo("Force Reload of all item sounds");
            var method1 = AccessTools.Method(typeof(GUISounds), "method_1").Invoke(guisounds, null);

        }
    }

    public class HideoutScreenLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuController), nameof(MainMenuController.method_17));
        }

        [PatchPostfix]
        private static void PatchPostfix(MainMenuController __instance)
        {
            var guiSounds = Singleton<GUISounds>.Instance;
            guiSounds.method_6(true);

            guiSounds.MasterMixer.DOSetFloat("HideoutVolume", Singleton<SharedGameSettingsClass>.Instance.Sound.Settings.HideoutVolumeValue, 0.5f);

        }
    }
}


