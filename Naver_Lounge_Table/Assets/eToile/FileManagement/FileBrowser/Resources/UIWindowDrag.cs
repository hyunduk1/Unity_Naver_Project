using UnityEngine;
using UnityEngine.EventSystems;

/*
 * Generic script to drag a window on 2D and 3D environments:
 * 1 - Attach this script to the UI element intended to handle the window (to drag the window).
 * 2 - Assign the main virual element that works as the widow itself (can be other than "handler" gameObject).
 */

public class UIWindowDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform window;                // The window intended to be dragged.
    public bool clampToCanvas = true;           // Clamp the window inside the container canvas.
    RectTransform rootCanvas;                   // The container canvas of this window (reference calculations).
    Vector2 pointerOffset;                      // Point where the drag started.

    /// <summary>Connect with the window</summary>
    void Start()
    {
        if (window == null)
            Debug.LogError("[UIWindowDrag] " + transform.name + " hasn't a main window RectTransform assigned.");
        else
            rootCanvas = window.parent.GetComponent<RectTransform>();
    }

    /// <summary>Event to save the start position of the pointer</summary>
    public void OnPointerDown(PointerEventData pointer)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(window, pointer.pressPosition, pointer.pressEventCamera, out pointerOffset);
    }

    /// <summary>Event to update the position while dragging</summary>
    public void OnDrag(PointerEventData pointer)
    {
        Vector2 touchPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(window, pointer.position, pointer.pressEventCamera, out touchPos);
        window.anchoredPosition += touchPos - pointerOffset;
        if (clampToCanvas)
        {
            // Get window and canvas dimensions:
            Vector3[] cCorners = new Vector3[4];
            rootCanvas.GetLocalCorners(cCorners);
            Vector2 _position = window.anchoredPosition;
            Rect winRect = window.rect;
            // Clamp the position of the windows inside the canvas:
            if (window.offsetMin.x < cCorners[0].x) _position.x = cCorners[0].x + winRect.width / 2f;   // Left
            if (window.offsetMax.x > cCorners[2].x) _position.x = cCorners[2].x - winRect.width / 2f;   // Right
            if (window.offsetMax.y > cCorners[2].y) _position.y = cCorners[2].y - winRect.height / 2f;  // Top
            if (window.offsetMin.y < cCorners[0].y) _position.y = cCorners[0].y + winRect.height / 2f;  // Bottom
            // Apply the canvas position:
            window.anchoredPosition = _position;
        }
    }
}