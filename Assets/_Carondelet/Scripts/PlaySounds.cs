using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    [SerializeField] private string SFXName;
    [Range(0,1)]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool isMusic = false;
    [SerializeField] private bool playonStart = false;

    private void Start()
    {
        if (playonStart)
        {
            
            PLaySound();
        }
    }

    public void PLaySound()
    {
        if (isMusic)
        {
            Debug.Log("Playing music: " + SFXName + " at volume: " + volume);
            AudioManagerBDC.I.StopMusic();
            AudioManagerBDC.I.PlayMusic(SFXName, volume);
            return;
        }
        Debug.Log("Playing SFX: " + SFXName + " at volume: " + volume);
        AudioManagerBDC.I.PlaySFX(SFXName, volume: volume);
    }

    public void PlaySoundPersistent()
    {
        Debug.Log("Playing Persistent SFX: " + SFXName + " at volume: " + volume);
        AudioManagerBDC.I.PlaySFXBetweenScenes(SFXName, volume: volume);
    }
}
