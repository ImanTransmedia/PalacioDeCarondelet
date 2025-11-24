using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public class CarouselItem
{
    [Header("Configuración del Objeto")]
    public LocalizedString itemName;
    public LocalizedString itemSubTitle;
    public Sprite itemImage;
}