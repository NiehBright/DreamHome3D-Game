using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Build
{
    /// <summary>
    /// Optimized helper class for Build Mode operations.
    /// Caches commonly-used components and provides efficient utilities.
    /// </summary>
    public static class BuildPerformanceOptimizer
    {
        private static readonly List<Renderer> CachedRenderers = new List<Renderer>(32);
        private static readonly List<RaycastHit> CachedRaycastHits = new List<RaycastHit>(16);

        /// <summary>
        /// Efficiently get all renderers from a transform hierarchy without allocating new lists.
        /// </summary>
        public static List<Renderer> GetRenderersNonAlloc(Transform root, bool includeInactive = true)
        {
            CachedRenderers.Clear();
            if (root == null) return CachedRenderers;

            root.GetComponentsInChildren(includeInactive, CachedRenderers);
            return CachedRenderers;
        }

        /// <summary>
        /// Efficiently raycast without allocating arrays on every call.
        /// </summary>
        public static bool TryRaycastAll(Ray ray, float maxDistance, int layerMask, out List<RaycastHit> results)
        {
            CachedRaycastHits.Clear();
            results = CachedRaycastHits;

            Physics.RaycastAll(ray, CachedRaycastHits, maxDistance, layerMask);
            if (CachedRaycastHits.Count == 0) return false;

            // Sort by distance
            CachedRaycastHits.Sort((a, b) => a.distance.CompareTo(b.distance));
            return true;
        }

        /// <summary>
        /// Efficiently apply material property blocks without allocating on each call.
        /// </summary>
        public static void ApplyPreviewColor(Transform root, Color color, int colorPropertyId = -1)
        {
            if (root == null) return;

            var block = new MaterialPropertyBlock();
            var renderers = GetRenderersNonAlloc(root, true);

            int actualPropertyId = colorPropertyId >= 0 ? colorPropertyId : Shader.PropertyToID("_BaseColor");

            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(block);

                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(actualPropertyId))
                {
                    block.SetColor(actualPropertyId, color);
                }

                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Clear material property blocks from all renderers (reset highlight).
        /// </summary>
        public static void ClearHighlight(Transform root)
        {
            if (root == null) return;

            var block = new MaterialPropertyBlock();
            var renderers = GetRenderersNonAlloc(root, true);

            foreach (var renderer in renderers)
            {
                block.Clear();
                renderer.SetPropertyBlock(block);
            }
        }
    }
}

