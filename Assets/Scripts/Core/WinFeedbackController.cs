// Scripts/Core/WinFeedbackController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays particle burst, shows "Puzzle Solved!" text, and draws a
/// glowing path from start to goal on victory.
/// </summary>
public class WinFeedbackController : MonoBehaviour
{
    [Header("Win Text")]
    public string winMessage = "Complete!";
    public float textDuration = 5f;
    public float textScaleBase = 0.003f;
    public float textScaleMax = 0.01f;

    [Header("Light Path")]
    public Color pathColor = Color.green;
    public float pathWidth = 0.04f;
    [SerializeField] private float pathYOffset = 0.0f;
    public float pathDrawDuration = 3f;

    [Header("Transition")]
    public float transitionDelay = 1.5f;
    public float transitionGrowDuration = 1.5f;

    [Header("Audio")]
    public AudioClip winSound;

    public System.Action OnTransitionComplete;

    private Canvas _winCanvas;
    private Text _winText;
    private Camera _cam;
    private LineRenderer _pathLine;
    private GridContainer _grid;
    private ConnectivityVisualizer _connectivity;

    void Awake()
    {
        _cam = Camera.main ?? FindFirstObjectByType<Camera>();

        // Create "Complete!" world-space text (single line)
        var canvasGO = new GameObject("WinTextCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = Vector3.up * 0.3f;
        _winCanvas = canvasGO.AddComponent<Canvas>();
        _winCanvas.renderMode = RenderMode.WorldSpace;
        _winCanvas.worldCamera = _cam;
        var rect = _winCanvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 80);

        var textGO = new GameObject("WinText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _winText = textGO.AddComponent<Text>();
        _winText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        _winText.fontSize = 48;
        _winText.alignment = TextAnchor.MiddleCenter;
        _winText.color = Color.green;
        _winText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _winText.verticalOverflow = VerticalWrapMode.Overflow;

        _winCanvas.gameObject.SetActive(false);

        // Create LineRenderer for victory light path
        var pathGO = new GameObject("WinPathLine");
        pathGO.transform.SetParent(transform, false);
        _pathLine = pathGO.AddComponent<LineRenderer>();
        _pathLine.startWidth = pathWidth;
        _pathLine.endWidth = pathWidth;
        _pathLine.startColor = pathColor;
        _pathLine.endColor = pathColor;
        _pathLine.positionCount = 0;
        _pathLine.enabled = false;
        _pathLine.useWorldSpace = true;
        _pathLine.material = new Material(Shader.Find("Sprites/Default"));

        _grid = FindFirstObjectByType<GridContainer>();
        _connectivity = FindFirstObjectByType<ConnectivityVisualizer>();
    }

    void Update()
    {
        if (_winCanvas == null || !_winCanvas.gameObject.activeSelf) return;

        // Billboard
        if (_cam != null)
        {
            _winCanvas.worldCamera = _cam;
            var canvasT = _winCanvas.transform;
            canvasT.rotation = Quaternion.LookRotation(canvasT.position - _cam.transform.position);

            float dist = Vector3.Distance(canvasT.position, _cam.transform.position);
            float scale = Mathf.Clamp(dist * 0.004f, textScaleBase, textScaleMax);
            canvasT.localScale = Vector3.one * scale;
        }
    }

    public void PlayWinEffects()
    {
        Debug.Log("[WinFeedback] Playing win effects!");
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        // Phase 1: Fade out all tile colors
        FadeOutAllTiles();

        // Phase 2: Animate light path from start to goal (~3s)
        yield return StartCoroutine(AnimateLightPath());

        // Phase 3: Sound + text
        if (winSound != null)
            AudioSource.PlayClipAtPoint(winSound, transform.position);

        if (_winText != null)
        {
            _winText.text = winMessage;
            _winCanvas.gameObject.SetActive(true);
        }

        // Phase 4: Transition — text grows to fill screen, then load next
        yield return new WaitForSeconds(transitionDelay);
        yield return StartCoroutine(GrowTextToFullscreen());
        OnTransitionComplete?.Invoke();
    }

    IEnumerator GrowTextToFullscreen()
    {
        if (_winCanvas == null) yield break;

        float elapsed = 0f;
        Vector3 startScale = _winCanvas.transform.localScale;
        float targetScale = 0.05f; // large enough to fill view

        while (elapsed < transitionGrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionGrowDuration;
            _winCanvas.transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, t);
            yield return null;
        }
    }

    void FadeOutAllTiles()
    {
        if (_grid == null) return;
        for (int x = 0; x < _grid.width; x++)
            for (int y = 0; y < _grid.height; y++)
                for (int z = 0; z < _grid.layers; z++)
                {
                    var node = _grid.GetNode(new GridIndex(x, y, z));
                    if (node != null && node.placedTile is LogicTile lt && lt.targetRenderer != null)
                        lt.targetRenderer.enabled = false;
                }
    }

    IEnumerator AnimateLightPath()
    {
        if (_grid == null || _connectivity == null || _pathLine == null) yield break;

        var path = _grid.GetConnectedPath(_connectivity.startIndex, _connectivity.goalIndex);
        if (path == null || path.Count < 2) yield break;

        // Build world positions
        var worldPath = new List<Vector3>(path.Count);
        for (int i = 0; i < path.Count; i++)
            worldPath.Add(_grid.GetWorldPos(path[i])); // same level as markers

        // Start with just the first point
        _pathLine.positionCount = 1;
        _pathLine.SetPosition(0, worldPath[0]);
        _pathLine.enabled = true;

        // Animate: add one point at a time
        float stepTime = pathDrawDuration / (path.Count - 1);
        for (int i = 1; i < path.Count; i++)
        {
            yield return new WaitForSeconds(stepTime);
            _pathLine.positionCount = i + 1;
            _pathLine.SetPosition(i, worldPath[i]);
        }

        // Full path — bright pulse
        _pathLine.startColor = pathColor * 1.5f;
        _pathLine.endColor = pathColor * 1.5f;

        // Turn goal marker green
        var goalMarker = GameObject.Find("GoalMarker");
        if (goalMarker != null)
        {
            var r = goalMarker.GetComponent<Renderer>();
            if (r != null && r.material != null)
                r.material.color = Color.green;
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator HideWinText(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_winCanvas != null)
            _winCanvas.gameObject.SetActive(false);
    }

    public void HideImmediately()
    {
        StopAllCoroutines();
        if (_winCanvas != null)
            _winCanvas.gameObject.SetActive(false);
        if (_pathLine != null)
            _pathLine.enabled = false;

        // Reset goal marker color
        var goalMarker = GameObject.Find("GoalMarker");
        if (goalMarker != null)
        {
            var r = goalMarker.GetComponent<Renderer>();
            if (r != null && r.material != null)
                r.material.color = Color.red;
        }
    }
}
