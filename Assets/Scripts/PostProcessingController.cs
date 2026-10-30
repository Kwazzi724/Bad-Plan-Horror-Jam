using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;
    [SerializeField] private Volume insaneVolume;

    void Awake()
    {
        ResetPostProcessing();
    }

    public void SetIntensity(float progress)
    {
        if(insaneVolume == null)
            return;
        
        insaneVolume.weight = Mathf.Clamp01(progress);
    }

    public void ResetPostProcessing()
    {
        if (volume != null)
            volume.weight = 1f;

        if (insaneVolume != null)
            insaneVolume.weight = 0f;
    }
}

