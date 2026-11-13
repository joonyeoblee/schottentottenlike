using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class AddressablesPool : IPunPrefabPool
{
    private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
    private readonly HashSet<string> _loadingSet = new HashSet<string>();


    public async void Preload(string address)
    {
        if (_prefabCache.ContainsKey(address) || _loadingSet.Contains(address))
            return;

        _loadingSet.Add(address);

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _prefabCache[address] = handle.Result;
            Debug.Log($"[AddressablesPool] '{address}' 로드 완료");
        }
        else
        {
            Debug.LogError($"[AddressablesPool] '{address}' 로드 실패");
        }

        _loadingSet.Remove(address);
    }
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[AddressablesPool] Instantiate 요청됨: {prefabId}");
        if (_prefabCache.TryGetValue(prefabId, out GameObject prefab))
        {
            // PUN 규약: 풀은 '비활성' 오브젝트를 돌려줘야 PhotonView 설정 후 활성화(Awake/Start)됩니다.
            var go = Object.Instantiate(prefab, position, rotation);
            go.SetActive(false);
            return go;
        }

        Debug.LogError($"[AddressablesPool] '{prefabId}' 프리팹이 사전 로드되지 않음");
        return null;
    }

    public void Destroy(GameObject gameObject)
    {
        Object.Destroy(gameObject);
    }
    public bool IsLoaded(string prefabId)
    {
        return _prefabCache.ContainsKey(prefabId);
    }

}
