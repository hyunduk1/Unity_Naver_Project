using UnityEngine;
using UnityEngine.EventSystems;

public class UIWindowBringToFront : MonoBehaviour, IPointerDownHandler
{
    public RectTransform window;                // The window intended to be rearranged.
    Canvas root;

    /// <summary>Connect with the main Canvas</summary>
    void Awake()
    {
        if (window == null)
            Debug.LogError("[UIWindowBringToFront] " + transform.name + " hasn't a main window RectTransform assigned.");
        else
            root = window.root.GetComponent<Canvas>(); ;
    }

    /// <summary>Event to save the start position of the pointer</summary>
    public void OnPointerDown(PointerEventData pointer)
    {
        // Send to front:
        root.transform.SetAsLastSibling();
        if (FileManagement.activeBrowser != null)
            FileManagement.activeBrowser.sortingOrder = root.sortingOrder;
        FileManagement.activeBrowser = root;
        root.sortingOrder = FileManagement.frontSortOrder;
    }
}
