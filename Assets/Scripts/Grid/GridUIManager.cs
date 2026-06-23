using UnityEngine;
using TMPro; 

public class GridUIManager : MonoBehaviour
{
    [Header("References")]
    public GridSystem LinkedGridSystem;
    public TextMeshProUGUI CounterText;

    private void OnEnable()
    {
        if (LinkedGridSystem != null)
        {
            LinkedGridSystem.OnGridStateChanged += UpdateTextDisplay;
        }
    }

    private void OnDisable()
    {
        if (LinkedGridSystem != null)
        {
            LinkedGridSystem.OnGridStateChanged -= UpdateTextDisplay;
        }
    }

    private void UpdateTextDisplay(int objectsLeft)
    {
        if (CounterText != null)
        {
            CounterText.text = "Pipes Left: " + objectsLeft.ToString();
        }
    }
}