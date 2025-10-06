using System;
using System.Linq;
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
                element.name = element.clips.First().name;
            }
            
            foreach (AudionElement element in musicClips)
            {
                element.name = element.clips.First().name;
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
            
            Debug.LogError("return null");
            return null;
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
