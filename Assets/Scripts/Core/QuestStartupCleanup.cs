using UnityEngine;

/// <summary>
/// On Quest standalone builds, disables screen-space canvases and tutorial
/// panels from the Meta XR template that cause stereo rendering artifacts.
/// </summary>
public class QuestStartupCleanup : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CleanupScreenSpaceCanvases();
        CleanupTutorialPanels();
#endif
    }

    void CleanupScreenSpaceCanvases()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            // Screen Space - Overlay canvases render incorrectly in stereo VR
            if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Debug.Log($"[QuestCleanup] Disabling canvas: {c.name} (mode={c.renderMode})");
                c.gameObject.SetActive(false);
            }
        }
    }

    void CleanupTutorialPanels()
    {
        // Disable known Meta XR tutorial/coaching objects by name
        string[] tutorialNames = { "Dialog", "CoachingUI", "Tutorial", "Instruction",
                                   "Coach", "Onboarding", "Tooltip", "HelpPanel",
                                   "VideoPlayer", "Video", "VideoScreen" };
        foreach (var name in tutorialNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                Debug.Log($"[QuestCleanup] Disabling: {name}");
                obj.SetActive(false);
            }
        }
    }
}
