using System;
using UnityEngine;

[ExecuteAlways]
public class UI_Cards : MonoBehaviour
{
    public Texture frontTexture;
    public Texture backTexture;
    public Sprite SizeSprite;

    public UI_CardDrawShow ShowAnimation;
    private SpriteRenderer rend;
    private MaterialPropertyBlock block;


    private void Awake()
    {
        if (ShowAnimation == null) ShowAnimation = GetComponent<UI_CardDrawShow>();
    }

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

    public void ShowDraw()
    {
        StartCoroutine(ShowAnimation.DrawProcessCoroutine());
    }

    public void ApplyTextures()
    {
        if (rend == null) rend = GetComponent<SpriteRenderer>();
        if (block == null) block = new MaterialPropertyBlock();
        if(rend.sprite == null) rend.sprite = SizeSprite;

        rend.GetPropertyBlock(block);
        block.SetTexture("_Front", frontTexture);
        block.SetTexture("_Back", backTexture);
        rend.SetPropertyBlock(block);
    }
}
