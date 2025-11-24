using UnityEngine;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private Transform defaultSpawnPoint;


    private static bool firstSpawn = true;

    IEnumerator Start()
    {
        // Esperamos un frame para que todo en la escena este listo
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        // Teleport al punto correspondiente
        Transform targetSpawn = GetSpawnPoint();
        player.transform.SetPositionAndRotation(targetSpawn.position, targetSpawn.rotation);


        if (firstSpawn)
        {
            firstSpawn = false;
            Debug.Log("Primer spawn: movimiento deshabilitado.");
        }
        else
        {
            player.GetComponent<FirstPersonMovement>().enabled = true;
            Debug.Log("Spawn subsecuente: movimiento habilitado.");
        }
    }

    private Transform GetSpawnPoint()
    {
        // Early exit si no hay puerta usada
        if (string.IsNullOrEmpty(DoorManager.Instance.LastDoorUsed))
            return defaultSpawnPoint;

        // Buscamos todas las puertas activas
        var doors = FindObjectsByType<DoorSceneLoader>(FindObjectsSortMode.InstanceID);
        foreach (var door in doors)
        {
            if (door != null && door.doorID == DoorManager.Instance.LastDoorUsed)
                return door.GetSpawnPoint();
        }

        Debug.LogWarning($"Door {DoorManager.Instance.LastDoorUsed} not found! Using default spawn.");
        return defaultSpawnPoint;
    }
}
