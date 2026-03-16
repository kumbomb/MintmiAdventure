using TMPro;
using UnityEngine;

public class DamageTextUI : MonoBehaviour
{
    TextMeshProUGUI textComponent;
    RectTransform rectTransform;
    Canvas canvas;
    RectTransform canvasRect;
    Camera uiCamera;
    Vector3 worldPosition;
    Vector3 worldOffset;
    float lifetime;
    float elapsed;
    float riseAmount;
    Color baseColor;

    void Awake()
    {
        rectTransform = gameObject.AddComponent<RectTransform>();
        gameObject.AddComponent<CanvasRenderer>();
        textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = false;
        textComponent.fontSize = 24f;
        textComponent.raycastTarget = false;
        textComponent.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        textComponent.outlineWidth = 0.18f;
        textComponent.color = Color.white;
        rectTransform.sizeDelta = new Vector2(180f, 48f);
        gameObject.SetActive(false);
    }

    public void Bind(Canvas targetCanvas)
    {
        canvas = targetCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
        uiCamera = canvas.worldCamera;
        transform.SetParent(canvas.transform, false);
    }

    public void SetFont(TMP_FontAsset fontAsset)
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (fontAsset != null)
            textComponent.font = fontAsset;
    }

    public void Show(Canvas targetCanvas, Vector3 targetWorldPosition, int damage, Color color, float duration = 0.65f, float verticalRise = 0.8f)
    {
        if (canvas != targetCanvas)
            Bind(targetCanvas);

        worldPosition = targetWorldPosition;
        worldOffset = new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);
        lifetime = Mathf.Max(0.75f, duration);
        elapsed = 0f;
        riseAmount = Mathf.Max(1.35f, verticalRise);
        baseColor = color;
        textComponent.color = color;
        textComponent.text = damage.ToString();
        gameObject.SetActive(true);
        UpdateVisual();
    }

    void Update()
    {
        if (!gameObject.activeSelf)
            return;

        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (canvasRect == null || Camera.main == null)
            return;

        float progress = Mathf.Clamp01(elapsed / lifetime);
        float riseProgress = 1f - Mathf.Pow(1f - progress, 2f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition + worldOffset + Vector3.up * (riseProgress * riseAmount));
        if (screenPos.z < 0f)
            screenPos.z *= -1f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPos);
        rectTransform.localPosition = localPos;

        Color color = baseColor;
        color.a = 1f - progress;
        textComponent.color = color;
        rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, progress);
    }
}
