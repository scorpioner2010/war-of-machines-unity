using System;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public static class AudioRobotTargetIds
    {
        public const string Common = "common";
        public const string IronAllianceAll = "faction:iron_alliance";
        public const string NovaSyndicateAll = "faction:nova_syndicate";
    }

    public class AudioResources : MonoBehaviour
    {
        [Button]
        public void SetNames()
        {
            SetNames(uiClips);
            SetNames(soundCategories);
            SetNames(engineProfiles);
            SetNames(musicClips);
        }

        public AudionElement[] uiClips;
        public AudioSoundCategory[] soundCategories;
        public AudioEngineProfile[] engineProfiles;
        [Tooltip("Overall volume multiplier for every robot engine loop and one-shot sound.")]
        [Range(0, 1)] public float engineMasterVolume = 1f;
        public AudionElement[] musicClips;

        public AudionElement GetResource(string clipName)
        {
            return FindResource(clipName, uiClips)
                ?? FindResource(clipName, soundCategories)
                ?? FindResource(clipName, musicClips);
        }

        public AudionElement GetResource(string clipName, SoundType type)
        {
            return type switch
            {
                SoundType.Ui => FindResource(
                    clipName,
                    uiClips),
                SoundType.Music => FindResource(clipName, musicClips),
                SoundType.Sfx => FindResource(clipName, soundCategories),
                _ => null
            };
        }

        public AudionElement GetResource(string clipName, SoundType type, string robotCode)
        {
            return type switch
            {
                SoundType.Ui => FindResource(clipName, uiClips),
                SoundType.Music => FindResource(clipName, musicClips),
                SoundType.Sfx => FindResource(clipName, soundCategories, robotCode),
                _ => null
            };
        }

        public AudioEngineProfile GetEngineProfile(string robotCode)
        {
            if (engineProfiles == null || engineProfiles.Length == 0)
            {
                return null;
            }

            string factionTarget = GetFactionTarget(robotCode);
            AudioEngineProfile bestMatch = null;
            int bestRank = int.MaxValue;

            foreach (AudioEngineProfile profile in engineProfiles)
            {
                if (profile == null)
                {
                    continue;
                }

                string target = string.IsNullOrWhiteSpace(profile.robotTarget)
                    ? AudioRobotTargetIds.Common
                    : profile.robotTarget;
                int rank = GetTargetRank(target, robotCode, factionTarget);
                if (rank >= bestRank)
                {
                    continue;
                }

                bestMatch = profile;
                bestRank = rank;
            }

            return bestRank <= 2 ? bestMatch : null;
        }

        private static void SetNames(AudionElement[] elements)
        {
            if (elements == null)
            {
                return;
            }

            foreach (AudionElement element in elements)
            {
                SetNames(element);
            }
        }

        private static void SetNames(AudionElement element)
        {
            if (element != null && string.IsNullOrWhiteSpace(element.name))
            {
                element.name = GetFirstClipName(element);
            }
        }

        private static void SetNames(AudioSoundCategory[] categories)
        {
            if (categories == null)
            {
                return;
            }

            foreach (AudioSoundCategory category in categories)
            {
                if (category != null)
                {
                    SetNames(category.robotGroups);
                }
            }
        }

        private static void SetNames(AudioEngineProfile[] profiles)
        {
            if (profiles == null)
            {
                return;
            }

            foreach (AudioEngineProfile profile in profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                SetNames(profile.idleLoop);
                SetNames(profile.runLoop);
                SetNames(profile.highLoop);
                SetNames(profile.startClip);
                SetNames(profile.stopClip);
                SetNames(profile.accelerationClips);
            }
        }

        private static void SetNames(AudioRobotSoundGroup[] groups)
        {
            if (groups == null)
            {
                return;
            }

            foreach (AudioRobotSoundGroup group in groups)
            {
                if (group != null)
                {
                    SetNames(group.clips);
                }
            }
        }

        private static AudionElement FindResource(
            string clipName,
            params AudionElement[][] collections)
        {
            if (collections == null || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            foreach (AudionElement[] elements in collections)
            {
                if (elements == null)
                {
                    continue;
                }

                foreach (AudionElement resource in elements)
                {
                    if (resource != null && clipName == resource.name)
                    {
                        return resource;
                    }
                }
            }

            return null;
        }

        private static AudionElement FindResource(
            string clipName,
            AudioSoundCategory[] categories)
        {
            if (categories == null || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            foreach (AudioSoundCategory category in categories)
            {
                if (category == null || category.robotGroups == null)
                {
                    continue;
                }

                foreach (AudioRobotSoundGroup group in category.robotGroups)
                {
                    AudionElement resource = group == null
                        ? null
                        : FindResource(clipName, group.clips);
                    if (resource != null)
                    {
                        return resource;
                    }
                }
            }

            return null;
        }

        private static AudionElement FindResource(
            string clipName,
            AudioSoundCategory[] categories,
            string robotCode)
        {
            if (categories == null || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            string factionTarget = GetFactionTarget(robotCode);
            AudionElement bestMatch = null;
            int bestRank = int.MaxValue;

            foreach (AudioSoundCategory category in categories)
            {
                if (category == null || category.robotGroups == null)
                {
                    continue;
                }

                foreach (AudioRobotSoundGroup group in category.robotGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    string target = string.IsNullOrWhiteSpace(group.robotTarget)
                        ? category.factionTarget
                        : group.robotTarget;
                    int rank = GetTargetRank(target, robotCode, factionTarget);
                    if (rank >= bestRank)
                    {
                        continue;
                    }

                    AudionElement resource = FindResource(clipName, group.clips);
                    if (resource != null)
                    {
                        bestMatch = resource;
                        bestRank = rank;
                    }
                }
            }

            return bestMatch;
        }

        private static int GetTargetRank(
            string target,
            string robotCode,
            string factionTarget)
        {
            if (!string.IsNullOrWhiteSpace(robotCode)
                && string.Equals(target, robotCode, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(factionTarget)
                && string.Equals(target, factionTarget, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(target, AudioRobotTargetIds.Common, StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 3;
        }

        private static string GetFactionTarget(string robotCode)
        {
            if (string.IsNullOrWhiteSpace(robotCode))
            {
                return string.Empty;
            }

            if (robotCode.StartsWith("ia_", StringComparison.OrdinalIgnoreCase))
            {
                return AudioRobotTargetIds.IronAllianceAll;
            }

            if (robotCode.StartsWith("nv_", StringComparison.OrdinalIgnoreCase))
            {
                return AudioRobotTargetIds.NovaSyndicateAll;
            }

            int separatorIndex = robotCode.IndexOf('_');
            return separatorIndex > 0
                ? "faction:" + robotCode.Substring(0, separatorIndex).ToLowerInvariant()
                : string.Empty;
        }

        private static string GetFirstClipName(AudionElement element)
        {
            if (element == null
                || element.clips == null
                || element.clips.Length == 0
                || element.clips[0] == null
                || element.clips[0].clip == null)
            {
                return string.Empty;
            }

            return element.clips[0].clip.name;
        }

    }

    [Serializable]
    public class AudioSoundCategory
    {
        public string name;
        public string factionTarget = AudioRobotTargetIds.Common;
        public AudioRobotSoundGroup[] robotGroups;
    }

    [Serializable]
    public class AudioEngineProfile
    {
        public string robotTarget;
        [Range(0.1f, 3f)] public float pitchMultiplier = 1f;
        public AudionElement idleLoop;
        public AudionElement runLoop;
        public AudionElement highLoop;
        public AudionElement[] accelerationClips;
        public AudionElement startClip;
        public AudionElement stopClip;
    }

    [Serializable]
    public class AudioRobotSoundGroup
    {
        public string robotTarget;
        public AudionElement[] clips;
    }
    
    [Serializable]
    public class AudionElement
    {
        public string name;
        [Range(0,1)]
        public float volume = 1f;
        [Range(0,1)]
        public float delay;
        public AudioClipEntry[] clips;
    }

    [Serializable]
    public class AudioClipEntry
    {
        public AudioClip clip;
        [Range(0, 1)]
        public float volume = 1f;
    }

}
