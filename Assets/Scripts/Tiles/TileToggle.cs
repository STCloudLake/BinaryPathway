// Scripts/Tiles/TileToggle.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Player-togglable tile. Extends LogicTile with poke-to-flip interaction.
/// On poke: PureValue(0)↔PureValue(1), ValueWithLogic flips value,
/// LogicOnly flips logic (AND↔NAND, OR↔NOR, XOR↔XNOR).
/// </summary>
public class ToggleTile : LogicTile
{
    [Header("Toggle Interaction")]
    [Tooltip("Trigger sub-object collider scale multiplier")]
    public float triggerScale = 1.5f;

    private XRSimpleInteractable _toggleInteractable;
    private GameObject _toggleTrigger;
    private XRInteractionManager _interactionManager;

    protected override void Awake()
    {
        base.Awake();
        _interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    public override void OnPlaced(GridContainer container, GridIndex index)
    {
        base.OnPlaced(container, index);

        // Create toggle trigger on first placement
        if (_toggleTrigger == null)
        {
            _toggleTrigger = new GameObject("ToggleTrigger");
            _toggleTrigger.transform.SetParent(transform, false);
            _toggleTrigger.transform.localPosition = Vector3.zero;
            _toggleTrigger.transform.localScale = Vector3.one * triggerScale;
            _toggleTrigger.layer = gameObject.layer;

            var col = _toggleTrigger.AddComponent<BoxCollider>();
            col.isTrigger = true;

            _toggleInteractable = _toggleTrigger.AddComponent<XRSimpleInteractable>();
            _toggleInteractable.interactionManager = _interactionManager;
            _toggleInteractable.selectMode = InteractableSelectMode.Single;
            _toggleInteractable.selectEntered.AddListener(_ => Toggle());
        }

        _toggleTrigger.SetActive(true);
        if (_interactionManager != null && _toggleInteractable != null)
            _interactionManager.RegisterInteractable((IXRInteractable)_toggleInteractable);
    }

    public override void OnRemoved(GridContainer container, GridIndex index)
    {
        base.OnRemoved(container, index);
        if (_toggleTrigger != null)
            _toggleTrigger.SetActive(false);
    }

    /// <summary>
    /// Flip the cell state: value flips, logic flips.
    /// Uses CellState.ApplyNot() for consistent behavior with SprayBottle.
    /// </summary>
    public void Toggle()
    {
        ApplyNot();
    }
}
