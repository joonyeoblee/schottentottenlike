using System;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class UI_Cards : MonoBehaviour
{
    public Texture frontTexture;
    public Texture backTexture;
    public Sprite SizeSprite;

    public UI_CardDrawShow ShowAnimation;
    public SpriteRenderer Rend;
    private MaterialPropertyBlock block;

    public GameObject DeadEffect;

    private void Awake()
    {
        if(Rend == null) Rend = GetComponent<SpriteRenderer>();
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

    public void ShowDeadEffect()
    {
        Instantiate(DeadEffect, transform.position, Quaternion.identity);
    }

    public void ShowDraw()
    {
        StartCoroutine(ShowAnimation.DrawProcessCoroutine());
    }

    public void SwitchRenderer(bool isOn)
    {
        Rend.enabled = isOn ? true : false;

    }

    public void ApplyTextures()
    {
        if (Rend == null) Rend = GetComponent<SpriteRenderer>();
        if (block == null) block = new MaterialPropertyBlock();
        if(Rend.sprite == null) Rend.sprite = SizeSprite;

        Rend.GetPropertyBlock(block);
        block.SetTexture("_Front", frontTexture);
        block.SetTexture("_Back", backTexture);
        Rend.SetPropertyBlock(block);
    }
}
