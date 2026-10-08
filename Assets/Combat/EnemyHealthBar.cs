using UnityEngine;
using UnityEngine.UI;

public enum EnemyHealthBarKind { Normal, Boss }

[RequireComponent(typeof(Health))]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealthBarKind kind;
    [SerializeField] private Vector2 worldSize = new Vector2(1.5f, 0.15f);
    [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.3f);
    [SerializeField] private Vector2 bossSize = new Vector2(450f, 22f);
    [SerializeField] private Vector2 bossScreenOffset = new Vector2(0f, -60f);
    [SerializeField] private Color fillColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

    private Health health;
    private GameObject barRoot;
    private RectTransform fill;

    public EnemyHealthBarKind Kind => kind;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        CreateBar();
        health.Changed += Refresh;
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

    private void CreateBar()
    {
        barRoot = new GameObject(name + (kind == EnemyHealthBarKind.Boss ? " Boss HP" : " HP"), typeof(RectTransform), typeof(Canvas));
        Canvas canvas = barRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 20;
        RectTransform root = (RectTransform)barRoot.transform;
        RectTransform background;

        if (kind == EnemyHealthBarKind.Boss)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = barRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            background = CreateImage("Background", root, backgroundColor);
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 1f);
            background.pivot = new Vector2(0.5f, 1f);
            background.anchoredPosition = bossScreenOffset;
            background.sizeDelta = bossSize;
        }
        else
        {
            canvas.renderMode = RenderMode.WorldSpace;
            root.sizeDelta = worldSize * 100f;
            root.localScale = Vector3.one * 0.01f;
            background = CreateImage("Background", root, backgroundColor);
            Stretch(background);
            UpdateWorldPosition();
        }

        fill = CreateImage("Fill", background, fillColor);
        Stretch(fill);
        fill.pivot = new Vector2(0f, 0.5f);
    }

    private static RectTransform CreateImage(string imageName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return (RectTransform)imageObject.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void Refresh(int current, int maximum)
    {
        float ratio = maximum > 0 ? (float)current / maximum : 0f;
        fill.anchorMax = new Vector2(ratio, 1f);
    }

    private void LateUpdate()
    {
        if (kind == EnemyHealthBarKind.Normal)
            UpdateWorldPosition();
    }

    private void UpdateWorldPosition()
    {
        // Keep the bar level and independent of the enemy's sprite scale/facing.
        barRoot.transform.position = transform.position + (Vector3)worldOffset;
        barRoot.transform.rotation = Quaternion.identity;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Changed -= Refresh;
        if (barRoot != null)
            Destroy(barRoot);
    }

    private void OnValidate()
    {
        worldSize = new Vector2(Mathf.Max(0.01f, worldSize.x), Mathf.Max(0.01f, worldSize.y));
        bossSize = new Vector2(Mathf.Max(1f, bossSize.x), Mathf.Max(1f, bossSize.y));
    }
}
