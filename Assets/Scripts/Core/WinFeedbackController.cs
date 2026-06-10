using System.Collections;
using UnityEngine;

/// <summary>
/// Plays visual and audio feedback when the puzzle is won.
/// Attached to the GameManager GameObject.
/// </summary>
public class WinFeedbackController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem winParticles;

    [Header("Text")]
    public TMPro.TextMeshPro winText;
    public float textDisplayDuration = 5f;

    [Header("Sound (optional)")]
    public AudioSource audioSource;
    public AudioClip winSound;

    private Coroutine _textRoutine;

    public void PlayWinEffects()
    {
        if (winParticles != null)
            winParticles.Play();

        if (winText != null)
        {
            winText.gameObject.SetActive(true);
            if (_textRoutine != null) StopCoroutine(_textRoutine);
            _textRoutine = StartCoroutine(HideTextAfterDelay());
        }

        if (audioSource != null && winSound != null)
            audioSource.PlayOneShot(winSound);

        Debug.Log("[WinFeedback] Win effects triggered.");
    }

    IEnumerator HideTextAfterDelay()
    {
        yield return new WaitForSeconds(textDisplayDuration);
        if (winText != null)
            winText.gameObject.SetActive(false);
    }
}
