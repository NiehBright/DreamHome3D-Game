using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInputReader : MonoBehaviour
{
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private bool allowMouseSimulation = true;
    [SerializeField] private bool emitDuringDrag = true;

    public event Action<Vector2Int> OnSwipe;

    private Vector2 startPosition;
    private bool tracking;
    private bool trackingTouch;
    private bool swipeSentInGesture;

    private void Update()
    {
        bool touchHandled = false;

        if (Touchscreen.current != null)
        {
            var primaryTouch = Touchscreen.current.primaryTouch;

            if (primaryTouch.press.wasPressedThisFrame)
            {
                tracking = true;
                trackingTouch = true;
                swipeSentInGesture = false;
                startPosition = primaryTouch.position.ReadValue();
                touchHandled = true;
            }
            else if (tracking && trackingTouch && emitDuringDrag && primaryTouch.press.isPressed && !swipeSentInGesture)
            {
                Vector2 currentPosition = primaryTouch.position.ReadValue();
                if (TryEmitSwipe(currentPosition - startPosition))
                {
                    swipeSentInGesture = true;
                }
            }
            else if (tracking && trackingTouch && primaryTouch.press.wasReleasedThisFrame)
            {
                tracking = false;
                trackingTouch = false;
                if (!swipeSentInGesture)
                {
                    Vector2 endPosition = primaryTouch.position.ReadValue();
                    TryEmitSwipe(endPosition - startPosition);
                }
                touchHandled = true;
            }
        }

        if (!touchHandled && allowMouseSimulation && Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                tracking = true;
                trackingTouch = false;
                swipeSentInGesture = false;
                startPosition = Mouse.current.position.ReadValue();
            }
            else if (tracking && !trackingTouch && emitDuringDrag && Mouse.current.leftButton.isPressed && !swipeSentInGesture)
            {
                Vector2 currentPosition = Mouse.current.position.ReadValue();
                if (TryEmitSwipe(currentPosition - startPosition))
                {
                    swipeSentInGesture = true;
                }
            }
            else if (tracking && !trackingTouch && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                tracking = false;
                if (!swipeSentInGesture)
                {
                    Vector2 endPosition = Mouse.current.position.ReadValue();
                    TryEmitSwipe(endPosition - startPosition);
                }
            }
        }
    }

    private bool TryEmitSwipe(Vector2 delta)
    {
        if (delta.magnitude < minSwipeDistance)
        {
            return false;
        }

        Vector2Int direction;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            direction = delta.x > 0f ? Vector2Int.right : Vector2Int.left;
        }
        else
        {
            direction = delta.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        OnSwipe?.Invoke(direction);
        return true;
    }
}

