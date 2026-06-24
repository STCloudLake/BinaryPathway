using System.Collections;
using UnityEngine;

/// <summary>
/// Sets the player's initial virtual position after ISDK finishes initializing.
/// Uses ISDK CharacterController.SetPosition() for proper locomotion integration.
/// OVRCameraRig stays at (0,0,0) — PlayerController is the virtual character.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Where the player starts in virtual space (world coordinates).")]
    public Vector3 spawnPosition = new Vector3(-12f, 0f, 0f);

    [Tooltip("Frames to wait for ISDK to fully initialize.")]
    public int delayFrames = 4;

    private Oculus.Interaction.Locomotion.CharacterController _isdkCC;

    IEnumerator Start()
    {
        // Wait for ISDK to initialize (FirstPersonLocomotor overrides position during its Start)
        for (int i = 0; i < delayFrames; i++)
            yield return null;

        var pc = GameObject.Find("PlayerController");
        if (pc == null)
        {
            Debug.LogWarning("[PlayerSpawn] PlayerController not found!");
            yield break;
        }

        _isdkCC = pc.GetComponent<Oculus.Interaction.Locomotion.CharacterController>();
        if (_isdkCC == null)
        {
            Debug.LogWarning("[PlayerSpawn] ISDK CharacterController not found!");
            yield break;
        }

        // Build target position: hotspot X/Z, preserve ISDK-managed height
        Vector3 targetPos = _isdkCC.Pose.position;
        targetPos.x = spawnPosition.x;
        targetPos.z = spawnPosition.z;
        // Only override Y if spawnPosition has a non-zero Y
        if (spawnPosition.y != 0f)
            targetPos.y = spawnPosition.y;

        _isdkCC.SetPosition(targetPos);

        Debug.Log($"[PlayerSpawn] Spawned at {targetPos} (world: {pc.transform.position})");
    }
}
