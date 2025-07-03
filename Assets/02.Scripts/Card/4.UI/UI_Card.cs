using UnityEngine;

[ExecuteAlways]
public class UI_Cards : MonoBehaviour
{
    public Texture frontTexture;
    public Texture backTexture;

    private Renderer rend;
    private MaterialPropertyBlock block;

    private void OnEnable()
    {
        ApplyTextures();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyTextures();
    }
#endif

    void ApplyTextures()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block);
        block.SetTexture("_Front", frontTexture);
        block.SetTexture("_Back", backTexture);
        rend.SetPropertyBlock(block);
    }
}
