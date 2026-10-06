using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;
    private static Sprite fallbackSprite;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (fillImage == null)
            return;

        // A Filled Image needs a sprite; a plain Image with no sprite ignores fillAmount.
        if (fillImage.sprite == null)
        {
            if (fallbackSprite == null)
            {
                Texture2D white = Texture2D.whiteTexture;
                fallbackSprite = Sprite.Create(
                    white,
                    new Rect(0, 0, white.width, white.height),
                    new Vector2(0.5f, 0.5f)
                );
            }

            fillImage.sprite = fallbackSprite;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
    }

    private void OnEnable()
    {
        if (health == null || fillImage == null)
        {
            Debug.LogError("HealthBarUI thiếu Health hoặc Fill Image.", this);
            return;
        }

        health.Changed += Refresh;
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        if (health != null)
            health.Changed -= Refresh;
    }

    private void Refresh(int current, int maximum)
    {
        fillImage.fillAmount = maximum > 0
            ? (float)current / maximum
            : 0f;
    }
}
