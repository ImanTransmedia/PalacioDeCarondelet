using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GeneralConfig
{
    public List<SalonConfig> salones = new List<SalonConfig>();
}

[Serializable]
public class SalonConfig
{
    public string salon;
    public List<InteractableEntry> objetos = new List<InteractableEntry>();
}

[Serializable]
public class InteractableEntry
{
    public string identifier;
    public string objectName;
    public string type;
    public string scene;
    public Vector3 eyeOffset;
    public bool isInCarrousel;
    public int carrouselIndex;

    public String salonDescarga;
    public string imageName;
    public List<string> imageNames;

    public bool isVideo;
    public string videoName;
    public string videoReverse;
    public Vector2 videoPosition;
    public Vector3 videoScale;
    public bool oscilate;
}