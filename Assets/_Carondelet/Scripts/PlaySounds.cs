using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    [SerializeField] private string SFXName;
    [Range(0,1)]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool isMusic = false;

    public void PLaySound()
    {
        if (isMusic)
        {
            Debug.Log("Playing music: " + SFXName + " at volume: " + volume);
            AudioManagerBDC.I.PlayMusic(SFXName, volume);
            return;
        }
        Debug.Log("Playing SFX: " + SFXName + " at volume: " + volume);
        AudioManagerBDC.I.PlaySFX(SFXName, volume: 0.8f);
    }
}
