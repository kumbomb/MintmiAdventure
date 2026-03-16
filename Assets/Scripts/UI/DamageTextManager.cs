using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    const string DamageFontResourcePath = "Fonts/DNFBitBitv2 SDF";

    static DamageTextManager instance;

    [SerializeField] int initialPoolSize = 24;
    readonly List<DamageTextUI> pool = new List<DamageTextUI>();
    Canvas targetCanvas;
    TMP_FontAsset damageFontAsset;

    public static void ShowDamage(Vector3 worldPosition, int damage, Color color)
    {
        DamageTextManager manager = EnsureInstance();
        if (manager == null)
            return;

        manager.ShowInternal(worldPosition, damage, color);
    }

    static DamageTextManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject canvasObject = GameObject.Find("Canvas_HP");
        if (canvasObject == null)
            return null;

        instance = canvasObject.GetComponent<DamageTextManager>();
        if (instance == null)
            instance = canvasObject.AddComponent<DamageTextManager>();

        instance.Initialize();
        return instance;
    }

    void Initialize()
    {
        if (targetCanvas != null)
            return;

        targetCanvas = GetComponent<Canvas>();
        damageFontAsset = Resources.Load<TMP_FontAsset>(DamageFontResourcePath);
        if (damageFontAsset == null)
            damageFontAsset = TMP_Settings.defaultFontAsset;

        for (int i = pool.Count; i < initialPoolSize; i++)
            pool.Add(CreateText());
    }

    DamageTextUI CreateText()
    {
        GameObject go = new GameObject("DamageText");
        DamageTextUI damageText = go.AddComponent<DamageTextUI>();
        damageText.Bind(targetCanvas);
        damageText.SetFont(damageFontAsset);
        return damageText;
    }

    void ShowInternal(Vector3 worldPosition, int damage, Color color)
    {
        Initialize();
        DamageTextUI available = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
            {
                available = pool[i];
                break;
            }
        }

        if (available == null)
        {
            available = CreateText();
            pool.Add(available);
        }

        available.Show(targetCanvas, worldPosition, damage, color);
    }
}
