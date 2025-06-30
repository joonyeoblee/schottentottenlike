using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CardSon : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite defaultSprite;
    
    public void SetData(CardData data)
    {
        LoadSpriteFromAddressables(data.ImageName);
    }

    private void LoadSpriteFromAddressables(string address)
    {
        Addressables.LoadAssetAsync<Sprite>(address).Completed += OnSpriteLoaded;
    }

    private void OnSpriteLoaded(AsyncOperationHandle<Sprite> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            spriteRenderer.sprite = handle.Result;
        }
        else
        {
            Debug.LogWarning($"[CardSon] Missing sprite for key: {handle.DebugName}. Using default sprite.");
            spriteRenderer.sprite = defaultSprite;
        }
    }
}