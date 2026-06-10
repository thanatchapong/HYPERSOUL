using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float smoothness = 0.1f; 
    [SerializeField] private float intensity = 50f; 
    [SerializeField] private Vector2 maxDistance = new Vector2(100f, 100f); 

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 currentVelocity;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition; 
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        
        Vector2 offset = new Vector2(
            (mousePos.x - screenCenter.x) / screenCenter.x,
            (mousePos.y - screenCenter.y) / screenCenter.y
        );

        float targetX = Mathf.Clamp(offset.x * intensity, -maxDistance.x, maxDistance.x);
        float targetY = Mathf.Clamp(offset.y * intensity, -maxDistance.y, maxDistance.y);
        
        Vector2 targetPos = startPosition + new Vector2(targetX, targetY);

        rectTransform.anchoredPosition = Vector2.SmoothDamp(
            rectTransform.anchoredPosition, 
            targetPos, 
            ref currentVelocity, 
            smoothness,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }
}