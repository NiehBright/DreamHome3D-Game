using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Runtime.Build;

namespace _Scripts.Runtime.Build
{
    public class MainModeCameraPanController : MonoBehaviour
    {
        private const string BoundsObjectName = "MainCameraBounds";

        [Header("References")]
        [SerializeField] private ModeController modeController;
        [SerializeField] private Camera targetCamera;

        [Header("Pan")]
        [SerializeField] private bool enablePan = true;
        [SerializeField, Min(1f)] private float panStartPixels = 28f;

        [Header("Bounds")]
        [SerializeField] private bool clampToBounds = true;
        [SerializeField] private BoxCollider cameraBoundsCollider;
        [SerializeField] private bool useColliderBoundsOnly = true;
        [SerializeField] private Vector2 cameraMinXZ = new Vector2(-4f, -4f);
        [SerializeField] private Vector2 cameraMaxXZ = new Vector2(12f, 12f);
        [SerializeField] private float groundPlaneY;

        private bool pointerIsDown;
        private bool pointerBlockedByUi;
        private int activePointerId = -1;
        private Vector2 pointerDownScreenPosition;
        private Vector2 pointerLastScreenPosition;

        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>(8);

        private void Awake()
        {
            if (modeController == null)
            {
                modeController = FindFirstObjectByType<ModeController>();
            }

            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            TryAutoAssignBoundsCollider();
        }

        [ContextMenu("Create/Assign Camera Bounds Collider")]
        private void CreateOrAssignCameraBoundsCollider()
        {
            if (cameraBoundsCollider != null)
            {
                return;
            }

            GameObject boundsObject = new GameObject(BoundsObjectName);
            boundsObject.transform.SetParent(transform, false);
            boundsObject.transform.localPosition = Vector3.zero;

            cameraBoundsCollider = boundsObject.AddComponent<BoxCollider>();
            cameraBoundsCollider.isTrigger = true;
            cameraBoundsCollider.size = new Vector3(16f, 20f, 16f);
        }

        private void TryAutoAssignBoundsCollider()
        {
            if (cameraBoundsCollider != null)
            {
                return;
            }

            cameraBoundsCollider = GetComponentInChildren<BoxCollider>();
        }

        private void Update()
        {
            if (!enablePan || targetCamera == null || modeController == null || modeController.CurrentMode != GameMode.Main)
            {
                return;
            }

            if (!TryGetPointerState(out Vector2 pointerScreenPosition, out bool pressedThisFrame, out bool releasedThisFrame, out bool isPressed, out int pointerId))
            {
                return;
            }

            if (pressedThisFrame)
            {
                pointerIsDown = true;
                activePointerId = pointerId;
                pointerDownScreenPosition = pointerScreenPosition;
                pointerLastScreenPosition = pointerScreenPosition;
                pointerBlockedByUi = IsPointerOverUi(activePointerId, pointerScreenPosition);
            }

            if (pointerIsDown && isPressed)
            {
                if (IsPointerOverUi(activePointerId, pointerScreenPosition))
                {
                    pointerBlockedByUi = true;
                }

                if (pointerBlockedByUi)
                {
                    pointerLastScreenPosition = pointerScreenPosition;
                    return;
                }

                float dragDistanceSqr = (pointerScreenPosition - pointerDownScreenPosition).sqrMagnitude;
                if (dragDistanceSqr > panStartPixels * panStartPixels)
                {
                    PanCamera(pointerLastScreenPosition, pointerScreenPosition);
                }

                pointerLastScreenPosition = pointerScreenPosition;
            }

            if (!releasedThisFrame)
            {
                return;
            }

            pointerIsDown = false;
            pointerBlockedByUi = false;
            activePointerId = -1;
        }

        private void PanCamera(Vector2 fromScreenPosition, Vector2 toScreenPosition)
        {
            if (!TryScreenToGroundPoint(fromScreenPosition, out Vector3 fromWorld)
                || !TryScreenToGroundPoint(toScreenPosition, out Vector3 toWorld))
            {
                return;
            }

            Vector3 delta = toWorld - fromWorld;
            targetCamera.transform.position += new Vector3(delta.x, 0f, delta.z);
            ClampCameraPosition();
        }

        private void ClampCameraPosition()
        {
            if (!clampToBounds || targetCamera == null)
            {
                return;
            }

            Vector3 cameraPosition = targetCamera.transform.position;

            if (cameraBoundsCollider != null)
            {
                Bounds bounds = cameraBoundsCollider.bounds;
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, bounds.min.x, bounds.max.x);
                cameraPosition.z = Mathf.Clamp(cameraPosition.z, bounds.min.z, bounds.max.z);
            }
            else if (useColliderBoundsOnly)
            {
                return;
            }
            else
            {
                cameraPosition.x = Mathf.Clamp(cameraPosition.x, Mathf.Min(cameraMinXZ.x, cameraMaxXZ.x), Mathf.Max(cameraMinXZ.x, cameraMaxXZ.x));
                cameraPosition.z = Mathf.Clamp(cameraPosition.z, Mathf.Min(cameraMinXZ.y, cameraMaxXZ.y), Mathf.Max(cameraMinXZ.y, cameraMaxXZ.y));
            }

            targetCamera.transform.position = cameraPosition;
        }

        private bool TryScreenToGroundPoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
            worldPoint = default;

            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundPlaneY, 0f));
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            return true;
        }

        private static bool IsPointerOverUi(int pointerId, Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (pointerId >= 0 && EventSystem.current.IsPointerOverGameObject(pointerId))
            {
                return true;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            UiRaycastResults.Clear();
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            EventSystem.current.RaycastAll(eventData, UiRaycastResults);
            return UiRaycastResults.Count > 0;
        }

        private static bool TryGetPointerState(
            out Vector2 position,
            out bool pressedThisFrame,
            out bool releasedThisFrame,
            out bool isPressed,
            out int pointerId)
        {
            position = default;
            pressedThisFrame = false;
            releasedThisFrame = false;
            isPressed = false;
            pointerId = -1;

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                position = touch.position.ReadValue();
                pressedThisFrame = touch.press.wasPressedThisFrame;
                releasedThisFrame = touch.press.wasReleasedThisFrame;
                isPressed = touch.press.isPressed;
                pointerId = touch.touchId.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                isPressed = Mouse.current.leftButton.isPressed;
                return true;
            }

            return false;
        }
    }
}

