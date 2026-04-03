using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInputReader : MonoBehaviour
{
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private bool allowMouseSimulation = true;

    public event Action<Vector2Int> OnSwipe;

    private Vector2 startPosition;
    private bool tracking;
    private bool trackingTouch;

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
                startPosition = primaryTouch.position.ReadValue();
                touchHandled = true;
            }
            else if (tracking && trackingTouch && primaryTouch.press.wasReleasedThisFrame)
            {
                tracking = false;
                trackingTouch = false;
                Vector2 endPosition = primaryTouch.position.ReadValue();
                TryEmitSwipe(endPosition - startPosition);
                touchHandled = true;
            }
        }

        if (!touchHandled && allowMouseSimulation && Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                tracking = true;
                trackingTouch = false;
                startPosition = Mouse.current.position.ReadValue();
            }
            else if (tracking && !trackingTouch && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                tracking = false;
                Vector2 endPosition = Mouse.current.position.ReadValue();
                TryEmitSwipe(endPosition - startPosition);
            }
        }
    }

    private void TryEmitSwipe(Vector2 delta)
    {
        if (delta.magnitude < minSwipeDistance)
        {
            return;
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
    }
}

