using System.Collections.Generic;
using UnityEngine;

namespace Collider2DTools
{
    /// <summary>
    /// Builds a combined mesh visualization for Collider2D shapes under a target root.
    /// Edge colliders are rendered with a LineRenderer instance.
    /// </summary>
    [DefaultExecutionOrder(100)] // Set lower priority to account for other scripts adding colliders dynamically
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(LineRenderer))]
    public class ColliderVisualizer2D : MonoBehaviour
    {
        [Tooltip("Root object to scan for Collider2D components. Uses this GameObject when unset.")]
        [SerializeField] private GameObject _root;
        [Tooltip("Include inactive Collider2D components")]
        [SerializeField] private bool _includeInactive;

        private void Start()
        {
            if (!enabled) return;

            GameObject root = _root != null ? _root : gameObject;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = false; // This LineRenderer is used as a template

            var combineList = new List<CombineInstance>();
            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(_includeInactive);

            foreach (Collider2D collider in colliders)
            {
                if (collider == null) continue;

                if (collider is EdgeCollider2D edge)
                {
                    if (edge.pointCount < 2) continue;

                    var go = new GameObject("Line");
                    go.transform.SetParent(transform, false);
                    var lr = go.AddComponent<LineRenderer>();
                    CopyLineRenderer(lineRenderer, lr);
                    lr.positionCount = edge.pointCount;
                    var positions = new Vector3[edge.pointCount];
                    for (int i = 0; i < edge.pointCount; i++)
                    {
                        Vector3 worldPoint = edge.transform.TransformPoint(edge.points[i]);
                        positions[i] = lr.useWorldSpace ? worldPoint : lr.transform.InverseTransformPoint(worldPoint);
                    }
                    lr.SetPositions(positions);
                    continue;
                }

                if (CreateCombineInstance(collider) is { } combine)
                    combineList.Add(combine);
            }

            meshFilter.mesh.CombineMeshes(combineList.ToArray(), true, true);

            foreach (CombineInstance ci in combineList)
                Destroy(ci.mesh);
        }

        private CombineInstance? CreateCombineInstance(Collider2D collider)
        {
            // Temporarily force enable the collider to ensure a mesh is created
            var previouslyEnabled = collider.enabled;
            collider.enabled = true;

            Mesh mesh = collider.CreateMesh(true, true);

            collider.enabled = previouslyEnabled;

            if (mesh == null) return null;

            // Mesh vertices are in world space; transform to our local space
            return new CombineInstance { mesh = mesh, transform = transform.worldToLocalMatrix };
        }

        private static void CopyLineRenderer(LineRenderer src, LineRenderer dst)
        {
            dst.sharedMaterial = src.sharedMaterial;
            dst.sharedMaterials = src.sharedMaterials;
            dst.widthMultiplier = src.widthMultiplier;
            dst.widthCurve = src.widthCurve;
            dst.colorGradient = src.colorGradient;
            dst.loop = src.loop;
            dst.alignment = src.alignment;
            dst.textureMode = src.textureMode;
            dst.numCapVertices = src.numCapVertices;
            dst.numCornerVertices = src.numCornerVertices;
            dst.generateLightingData = src.generateLightingData;
            dst.shadowCastingMode = src.shadowCastingMode;
            dst.receiveShadows = src.receiveShadows;
            dst.lightProbeUsage = src.lightProbeUsage;
            dst.reflectionProbeUsage = src.reflectionProbeUsage;
            dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
            dst.sortingLayerID = src.sortingLayerID;
            dst.sortingOrder = src.sortingOrder;
            dst.maskInteraction = src.maskInteraction;
            dst.useWorldSpace = src.useWorldSpace;
        }
    }
}
