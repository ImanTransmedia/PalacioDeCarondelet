using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class AddresableAssetLoader : MonoBehaviour
{
    [SerializeField] private string AssetAddress;

    private void Awake()
    {
        Addressables.LoadAssetAsync<GameObject>(AssetAddress).Completed += (AsyncOperationHandle) =>
        {
            if (AsyncOperationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Instantiate(AsyncOperationHandle.Result);
            }
            else
            {
                Debug.Log("Error al cargar!");
            }
        };
    }
}
