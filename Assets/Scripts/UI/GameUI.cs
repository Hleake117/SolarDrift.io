using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private Canvas mainCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
        }

        if (pointsText == null)
        {
            // Create points text
            GameObject textObj = new GameObject("PointsText");
            textObj.transform.SetParent(transform, false);
            
            pointsText = textObj.AddComponent<TextMeshProUGUI>();
            pointsText.color = Color.white;
            pointsText.fontSize = 36;
            pointsText.alignment = TextAlignmentOptions.TopRight;
            
            // Position in top right
            RectTransform rectTransform = pointsText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-20, -20);
        }
    }

    public void UpdatePoints(float points)
    {
        if (pointsText != null)
        {
            pointsText.text = $"Points: {Mathf.Floor(points)}";
        }
    }
} 