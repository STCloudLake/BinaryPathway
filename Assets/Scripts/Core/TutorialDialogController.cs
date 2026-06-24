// Scripts/Core/TutorialDialogController.cs
using TMPro;
using UnityEngine;

/// <summary>
/// Updates the Dialog UI (TitleTextLabel + BodyText) based on the current level.
/// Called by GameManager when a level loads.
/// </summary>
public class TutorialDialogController : MonoBehaviour
{
    [System.Serializable]
    public struct TutorialText
    {
        public string title;
        [TextArea(2, 6)]
        public string body;
    }

    [Header("UI References")]
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI bodyLabel;

    [Header("Tutorial Texts")]
    public TutorialText[] tutorials;

    void Awake()
    {
        if (titleLabel == null)
        {
            var t = transform.Find("TitleTextLabel");
            if (t != null) titleLabel = t.GetComponent<TextMeshProUGUI>();
        }
        if (bodyLabel == null)
        {
            var b = transform.Find("BodyText");
            if (b != null) bodyLabel = b.GetComponent<TextMeshProUGUI>();
        }
    }

    public void ShowTutorial(int index)
    {
        if (tutorials == null || index < 0 || index >= tutorials.Length) return;
        var t = tutorials[index];
        if (titleLabel != null) titleLabel.text = t.title;
        if (bodyLabel != null) bodyLabel.text = t.body;
        Debug.Log($"[TutorialDialog] Showing tutorial {index}: {t.title}");
    }
}
