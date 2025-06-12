using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    private Coroutine _musicCoroutine;
    
    public enum SoundType
    {
        Stack_Complete,
        Level_Complete,
        Background_Music
        // Add more sound types as needed
    }

    [System.Serializable]
    public class Sound
    {
        public SoundType Type;
        public AudioClip Clip;

        [Range(0f, 1f)]
        public float Volume = 1f;

        [HideInInspector]
        public AudioSource Source;
    }

    //Singleton
    public static AudioManager Instance;

    //All sounds and their associated type - Set these in the inspector
    public Sound[] AllSounds;

    //Runtime collections
    private Dictionary<SoundType, List<Sound>> _soundDictionary = new Dictionary<SoundType, List<Sound>>();
    private AudioSource _musicSource;

    private void Awake()
    {
        //Assign singleton
        Instance = this;

        //Set up sounds
        for (int i = 0; i < AllSounds.Length; i++)
        {
            if (!_soundDictionary.ContainsKey(AllSounds[i].Type))
            {
                _soundDictionary[AllSounds[i].Type] = new List<Sound>();
            }
            _soundDictionary[AllSounds[i].Type].Add(AllSounds[i]);
        }
    }



    //Call this method to play a sound
    public void Play(SoundType type)
    {
        //Make sure there's a sound assigned to your specified type
        if (!_soundDictionary.TryGetValue(type, out List<Sound> s))
        {
            Debug.LogWarning($"Sound type {type} not found!");
            return;
        }

        //Creates a new sound object
        var soundObj = new GameObject($"Sound_{type}");
        var audioSrc = soundObj.AddComponent<AudioSource>();

        //Randomly selects a sound from the list
        int index = Random.Range(0, s.Count);

        //Assigns your sound properties
        audioSrc.clip = s[index].Clip;
        audioSrc.volume = s[index].Volume;

        //Play the sound
        audioSrc.Play();

        //Destroy the object
        Destroy(soundObj, s[index].Clip.length);
    }

    //Call this method to change music tracks
    public void ChangeMusic(SoundType type)
    {
        if (!_soundDictionary.TryGetValue(type, out List<Sound> track))
        {
            Debug.LogWarning($"Music track {type} not found!");
            return;
        }

        if (_musicSource == null)
        {
            var container = new GameObject("SoundTrackObj");
            _musicSource = container.AddComponent<AudioSource>();
            _musicSource.loop = false;
        }

        // Stop any previous coroutine running
        if (_musicCoroutine != null)
        {
            StopCoroutine(_musicCoroutine);
            _musicCoroutine = null;
        }

        // Start the coroutine to play songs continuously
        _musicCoroutine = StartCoroutine(PlayMusicLoop(track));
    }

    public void StopMusic()
    {
        _musicSource.Stop();
        StopCoroutine(_musicCoroutine);
        _musicCoroutine = null;
    }

    private IEnumerator PlayMusicLoop(List<Sound> track)
    {
        while (true)
        {
            int index = Random.Range(0, track.Count);
            _musicSource.clip = track[index].Clip;
            _musicSource.volume = track[index].Volume;
            _musicSource.Play();

            // Wait until clip is done playing
            yield return new WaitForSeconds(_musicSource.clip.length);
        }
    }
}