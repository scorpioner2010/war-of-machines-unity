using System;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.Audio
{
    public class AudioResources : MonoBehaviour
    {
        [Button]
        public void SetNames()
        {
            foreach (AudionElement element in soundClips)
            {
                element.name = GetFirstClipName(element);
            }
            
            foreach (AudionElement element in musicClips)
            {
                element.name = GetFirstClipName(element);
            }
        }
        
        public AudionElement[] soundClips;
        public AudionElement[] musicClips;

        public AudionElement GetResource(string clipName)
        {
            foreach (AudionElement resource in soundClips)
            {
                if (clipName == resource.name)
                {
                    return resource;
                }
            }
            
            foreach (AudionElement resource in musicClips)
            {
                if (clipName == resource.name)
                {
                    return resource;
                }
            }
            
            return null;
        }

        private static string GetFirstClipName(AudionElement element)
        {
            if (element == null || element.clips == null || element.clips.Length == 0 || element.clips[0] == null)
            {
                return string.Empty;
            }

            return element.clips[0].name;
        }
    }
    
    [Serializable]
    public class AudionElement
    {
        public string name;
        [Range(0,1)]
        public float volume;
        [Range(0,1)]
        public float delay;
        public AudioClip[] clips;
    }
}
