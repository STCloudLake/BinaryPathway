using UnityEngine;

/// <summary>
/// Restarts the current room's puzzle when selected/grabbed.
/// Attach to a projector object with ISDK interactable components.
/// </summary>
public class RestartTrigger : MonoBehaviour
{
    /// <summary>Called by InteractableUnityEventWrapper.WhenSelect</summary>
    public void OnRestart()
    {
        var rooms = FindObjectsOfType<RoomPuzzleManager>();
        var player = GameObject.Find("CenterEyeAnchor")?.transform;
        if (player == null) return;

        RoomPuzzleManager closest = null;
        float best = float.MaxValue;
        foreach (var r in rooms)
        {
            float d = Vector3.Distance(
                new Vector3(r.transform.position.x, 0, r.transform.position.z),
                new Vector3(player.position.x, 0, player.position.z));
            if (d < best) { best = d; closest = r; }
        }

        if (closest != null)
        {
            Debug.Log($"[RestartTrigger] Restarting {closest.roomName}");
            closest.RestartPuzzle();
        }
    }
}
