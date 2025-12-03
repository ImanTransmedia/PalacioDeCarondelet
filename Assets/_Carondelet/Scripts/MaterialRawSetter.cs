using UnityEngine;
using UnityEngine.UI;

public class MaterialRawSetter : MonoBehaviour
{

    [SerializeField] private Material originalMat;
    [SerializeField] private Material targetMat;
    [SerializeField] private RawImage image;

    public void ChangeMaterial()
    {
        if (image != null && targetMat != null)
        {
            image.material = targetMat;
        }
    }

    public void ResetMaterial()
    {
        if (image != null && originalMat != null)
        {
            Debug.Log("Resetting material");
            image.material = originalMat;
        }
    }

}
