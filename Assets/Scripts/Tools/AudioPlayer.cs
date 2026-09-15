using UnityEngine;
using Zenject;

public class AudioPlayer : MonoBehaviour
{
    private enum Mode
    {
        Local,
        AudioSource,
        Camera
    }
    [SerializeField] private Mode mode = Mode.Local;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clips;
    
    [Inject] private readonly CameraSystem _cameraSystem;

    public void Play(int clipIndex)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        if (clipIndex < 0 || clipIndex >= clips.Length)
        {
            return;
        }
        
        var clip = clips[clipIndex];

        switch (mode)
        {
            case Mode.Local:
                AudioSource.PlayClipAtPoint(clip, transform.position);
                break;
            case Mode.AudioSource:
                if (audioSource)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
                break;
            case Mode.Camera:
                _cameraSystem.AudioSource.clip = clip;
                _cameraSystem.AudioSource.Play();
                break;
        }
    }
}
