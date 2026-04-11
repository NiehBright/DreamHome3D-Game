using UnityEngine;

namespace _Scripts.Runtime.Build
{
    public class BuildPlacedWanderer : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private bool spawnIfMissing = true;
        [SerializeField] private float yOffset;

        [Header("Bounds")]
        [SerializeField] private bool useBuildGridBounds = true;
        [SerializeField] private global::Runtime.Build.BuildModeController buildModeController;
        [SerializeField] private BoxCollider boundsCollider;
        [SerializeField] private Vector3 boundsCenter;
        [SerializeField] private Vector3 boundsSize = new Vector3(8f, 0f, 8f);

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float minMoveSpeed = 0.6f;
        [SerializeField, Min(0.01f)] private float maxMoveSpeed = 1.4f;
        [SerializeField, Min(0f)] private float minIdleSeconds = 0.3f;
        [SerializeField, Min(0f)] private float maxIdleSeconds = 1.2f;
        [SerializeField, Min(0.01f)] private float arrivalThreshold = 0.05f;
        [SerializeField] private bool faceMoveDirection = true;
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 10f;

        [Header("Avoidance")]
        [SerializeField] private bool avoidObstacles = true;
        [SerializeField, Min(0.01f)] private float avoidDistance = 0.6f;
        [SerializeField, Min(0.01f)] private float avoidRadius = 0.2f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Persistence")]
        [SerializeField] private bool persistPosition = true;
        [SerializeField] private string positionSaveKey = "BuildPlacedWanderer.Position";
        [SerializeField] private bool snapToBoundsOnEnable = true;

        private Vector3 _target;
        private float _moveSpeed;
        private float _idleTimer;
        private bool _hasTarget;

        private void OnEnable()
        {
            EnsureCharacter();
            RefreshBounds();

            bool restored = TryRestoreSavedPosition();
            if (!restored && snapToBoundsOnEnable)
            {
                SnapToRandomPositionInBounds();
            }

            PickNewTarget(true);
        }

        private void OnDisable()
        {
            SavePosition();
        }

        private void Update()
        {
            if (characterRoot == null)
            {
                return;
            }

            if (avoidObstacles && IsObstacleAhead())
            {
                PickNewTarget(false);
                return;
            }

            if (_idleTimer > 0f)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f)
                {
                    PickNewTarget(false);
                }
                return;
            }

            if (!_hasTarget)
            {
                PickNewTarget(false);
            }

            Vector3 current = characterRoot.position;
            Vector3 next = Vector3.MoveTowards(current, _target, _moveSpeed * Time.deltaTime);
            characterRoot.position = next;

            Vector3 planarDelta = new Vector3(_target.x - current.x, 0f, _target.z - current.z);
            if (faceMoveDirection && planarDelta.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(planarDelta.normalized, Vector3.up);
                characterRoot.rotation = Quaternion.Slerp(characterRoot.rotation, targetRot, rotationLerpSpeed * Time.deltaTime);
            }

            if ((next - _target).sqrMagnitude <= arrivalThreshold * arrivalThreshold)
            {
                if (maxIdleSeconds > 0f)
                {
                    float idleMin = Mathf.Max(0f, minIdleSeconds);
                    float idleMax = Mathf.Max(idleMin, maxIdleSeconds);
                    _idleTimer = Random.Range(idleMin, idleMax);
                }
                else
                {
                    _idleTimer = 0f;
                    _hasTarget = false;
                }

                if (_idleTimer <= 0f)
                {
                    PickNewTarget(false);
                }
                else
                {
                    _hasTarget = false;
                }
            }
        }

        private void EnsureCharacter()
        {
            if (characterRoot != null)
            {
                return;
            }

            if (!spawnIfMissing || characterPrefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(characterPrefab, transform);
            instance.name = characterPrefab.name;
            characterRoot = instance.transform;
            characterRoot.position = transform.position;
        }

        private void RefreshBounds()
        {
            if (useBuildGridBounds)
            {
                RefreshBoundsFromBuildGrid();
            }

            if (boundsCollider == null)
            {
                return;
            }

            Bounds colliderBounds = boundsCollider.bounds;
            boundsCenter = colliderBounds.center;
            boundsSize = colliderBounds.size;
        }

        private void RefreshBoundsFromBuildGrid()
        {
            if (buildModeController == null)
            {
                buildModeController = FindFirstObjectByType<global::Runtime.Build.BuildModeController>();
            }

            if (buildModeController == null)
            {
                return;
            }

            float cellSize = Mathf.Max(0.001f, buildModeController.CellSize);
            float width = Mathf.Max(1f, buildModeController.GridWidth) * cellSize;
            float height = Mathf.Max(1f, buildModeController.GridHeight) * cellSize;
            Vector3 origin = buildModeController.GridOrigin;

            boundsSize = new Vector3(width, boundsSize.y, height);
            boundsCenter = origin + new Vector3((buildModeController.GridWidth - 1) * cellSize * 0.5f, 0f,
                (buildModeController.GridHeight - 1) * cellSize * 0.5f);
        }

        private bool IsObstacleAhead()
        {
            if (characterRoot == null)
            {
                return false;
            }

            Vector3 forward = characterRoot.forward;
            Vector3 origin = characterRoot.position + Vector3.up * 0.2f;
            return Physics.SphereCast(origin, avoidRadius, forward, out _, avoidDistance, obstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private bool TryRestoreSavedPosition()
        {
             if (!persistPosition || characterRoot == null || string.IsNullOrWhiteSpace(positionSaveKey))
             {
                 return false;
             }

             if (!PlayerPrefs.HasKey(positionSaveKey + ".x"))
             {
                 return false;
             }

             try
             {
                 float x = PlayerPrefs.GetFloat(positionSaveKey + ".x", characterRoot.position.x);
                 float y = PlayerPrefs.GetFloat(positionSaveKey + ".y", characterRoot.position.y);
                 float z = PlayerPrefs.GetFloat(positionSaveKey + ".z", characterRoot.position.z);
                 characterRoot.position = new Vector3(x, y, z);
                 return true;
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning($"BuildPlacedWanderer: Failed to restore position. {ex.Message}");
                 return false;
             }
        }

        private void SnapToRandomPositionInBounds()
        {
            if (characterRoot == null)
            {
                return;
            }

            RefreshBounds();

            Vector3 size = boundsSize;
            if (size.x <= 0.001f)
            {
                size.x = 1f;
            }

            if (size.z <= 0.001f)
            {
                size.z = 1f;
            }

            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;

            float targetX = Random.Range(boundsCenter.x - halfX, boundsCenter.x + halfX);
            float targetZ = Random.Range(boundsCenter.z - halfZ, boundsCenter.z + halfZ);
            Vector3 current = characterRoot.position;
            characterRoot.position = new Vector3(targetX, current.y + yOffset, targetZ);
        }

        private void SavePosition()
        {
             if (!persistPosition || characterRoot == null || string.IsNullOrWhiteSpace(positionSaveKey))
             {
                 return;
             }

             try
             {
                 Vector3 pos = characterRoot.position;
                 PlayerPrefs.SetFloat(positionSaveKey + ".x", pos.x);
                 PlayerPrefs.SetFloat(positionSaveKey + ".y", pos.y);
                 PlayerPrefs.SetFloat(positionSaveKey + ".z", pos.z);
                 PlayerPrefs.Save();
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning($"BuildPlacedWanderer: Failed to save position. {ex.Message}");
             }
        }

        private void PickNewTarget(bool immediate)
        {
            if (characterRoot == null)
            {
                return;
            }

            RefreshBounds();

            Vector3 size = boundsSize;
            if (size.x <= 0.001f)
            {
                size.x = 1f;
            }

            if (size.z <= 0.001f)
            {
                size.z = 1f;
            }

            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;

            float targetX = Random.Range(boundsCenter.x - halfX, boundsCenter.x + halfX);
            float targetZ = Random.Range(boundsCenter.z - halfZ, boundsCenter.z + halfZ);
            float baseY = characterRoot.position.y;

            _target = new Vector3(targetX, baseY + yOffset, targetZ);
            _moveSpeed = Random.Range(minMoveSpeed, Mathf.Max(minMoveSpeed, maxMoveSpeed));
            _hasTarget = true;

            if (immediate)
            {
                return;
            }

            if (maxIdleSeconds > 0f)
            {
                float idleMin = Mathf.Max(0f, minIdleSeconds);
                float idleMax = Mathf.Max(idleMin, maxIdleSeconds);
                _idleTimer = Random.Range(idleMin, idleMax);
            }
            else
            {
                _idleTimer = 0f;
            }
        }
    }
}
