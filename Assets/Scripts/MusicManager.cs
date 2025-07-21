using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    static private MusicManager instance;

    [SerializeField] private float fadeTime;

    private AudioSource[] musics;
    private int current;
    private HashSet<AudioSource> fading;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        fading = new();
    }

    private void Start()
    {
        musics = gameObject.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in musics)
        { audio.volume = 0.0f; }

        current = 0;
        Play_Instance(0);
    }

    private void FixedUpdate()
    {
        HashSet<AudioSource> toBeRemoved = new();
        foreach (AudioSource audio in fading)
        {
            if (audio == musics[current])
            {
                if (audio.volume < 1)
                { audio.volume += fadeTime * Time.fixedDeltaTime; }
                if (audio.volume >= 1)
                {
                    audio.volume = 1;
                    toBeRemoved.Add(audio);
                }
            }
            else if (audio.volume > 0)
            {
                audio.volume -= fadeTime * Time.fixedDeltaTime;
                if (audio.volume <= 0)
                {
                    audio.volume = 0;
                    toBeRemoved.Add(audio);
                }
            }
        }
        foreach (AudioSource audio in toBeRemoved)
        { fading.Remove(audio); }
    }

  static public int CurrentIndex => instance.current;

    static public void Play(int index)
    => instance.Play_Instance(index);
    public void Play_Instance(int index)
    {
        fading.Add(musics[current]);
        current = index;
        fading.Add(musics[current]);
    }
}
