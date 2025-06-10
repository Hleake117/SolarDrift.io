using UnityEngine;
using TMPro;

public class PointsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 36;
    [SerializeField] private Vector2 padding = new Vector2(20f, 20f);

    private void Start()
    {
        if (pointsText == null)
        {
            // Create the text object if it doesn't exist
            GameObject textObj = new GameObject("PointsText");
            textObj.transform.SetParent(transform, false);
            
            pointsText = textObj.AddComponent<TextMeshProUGUI>();
            pointsText.color = textColor;
            pointsText.fontSize = fontSize;
            pointsText.alignment = TextAlignmentOptions.TopRight;
            
            // Position the text in the top right corner
            RectTransform rectTransform = pointsText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = -padding;
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