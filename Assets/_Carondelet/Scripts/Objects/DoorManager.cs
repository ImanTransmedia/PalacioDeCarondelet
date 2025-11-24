using UnityEngine;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance;
    public string LastDoorUsed { get; set; }

    public string AdressableAdress { get; set; }
    public SceneInstance PreviousScene { get; set; }

    public bool isAccesible = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private List<string> storedStrings = new List<string>();

    public void StoreString(string newString)
    {
        storedStrings.Add(newString);
    }

    public bool ContainsString(string searchString)
    {
        return storedStrings.Contains(searchString);
    }

    public void SaveAdressableString(string AdressableAdressToSave)
    {
        AdressableAdress = AdressableAdressToSave;
    }

    public string GetAdressableAdress()
    {
        return AdressableAdress;
    }

    public void IsAccesibleChange(bool accesibility)
    {
        isAccesible = accesibility;
    }

}
