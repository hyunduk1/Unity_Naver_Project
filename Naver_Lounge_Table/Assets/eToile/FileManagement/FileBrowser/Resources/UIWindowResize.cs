using UnityEngine;
using UnityEngine.EventSystems;

/*
 * Generic script to resize a window on 2D and 3D environments:
 * 1 - Attach this script to the UI element intended to handle the window (to resize the window, bottom-left always).
 * 2 - Assign the main virual element that works as the widow itself (can be other than "handler" gameObject).
 */

public class UIWindowResize : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform window;                // The window intended to be resized.
    public int minWidth = 400;                  // Minimum width of the window to clamp to.
    public int minHeight = 300;                 // Minimum height of the window to clamp to.
    public bool clampToCanvas = true;           // Clamp the window inside the container canvas.
    RectTransform rootCanvas;                   // The container of this window (reference calculations).
    Vector2 pointerOffset;                      // First clic delta coordinates ot the bottom-right corner.

    /// <summary>Connect with the window</summary>
    void Start()
    {
        if (window == null)
            Debug.LogError("[UIWindowResize] " + transform.name + " hasn't a main window RectTransform assigned.");
        else
            rootCanvas = window.parent.GetComponent<RectTransform>();
    }

    /// <summary>Event to save the start position of the pointer</summary>
    public void OnPointerDown(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(window, pointer.pressPosition, pointer.pressEventCamera, out pointerOffset);
        Vector3[] wCorners = new Vector3[4];
        window.GetLocalCorners(wCorners);
        pointerOffset = (Vector2)wCorners[3] - pointerOffset;
    }

    /// <summary>Event to update the size while dragging</summary>
    public void OnDrag(PointerEventData pointer)
    {
        Vector2 pointerPosition;
        // Pointer drag in canvas coordinates:
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas, pointer.position, pointer.pressEventCamera, out pointerPosition);
        Vector2 corner = pointerPosition + pointerOffset;               // The target bottom-right corner.
        // Check min size limits:
        if (corner.x < window.offsetMin.x + minWidth) corner.x = window.offsetMin.x + minWidth;
        if (corner.y > window.offsetMax.y - minHeight) corner.y = window.offsetMax.y - minHeight;
        // Check canvas limits:
        if (clampToCanvas)
        {
            Vector3[] cCorners = new Vector3[4];
            rootCanvas.GetLocalCorners(cCorners);
            if (corner.x > cCorners[3].x) corner.x = cCorners[3].x;
            if (corner.y < cCorners[3].y) corner.y = cCorners[3].y;
        }
        // Apply the new size:
        window.offsetMax = new Vector2(corner.x, window.offsetMax.y);   // Upper right corner.
        window.offsetMin = new Vector2(window.offsetMin.x, corner.y);   // Lower left corner.
    }
}
