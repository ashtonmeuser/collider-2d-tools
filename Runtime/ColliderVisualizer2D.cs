using System.Collections.Generic;
using UnityEngine;

namespace Collider2DTools
{
    /// <summary>
    /// Builds a combined mesh visualization for Collider2D shapes under a target root.
    /// Edge colliders are rendered with a LineRenderer instance.
    /// </summary>
    [DefaultExecutionOrder(100)] // Set lower priority to account for other scripts adding colliders dynamically
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ColliderVisualizer2D : MonoBehaviour
    {
        [Tooltip("Root object to scan for Collider2D components. Uses this GameObject when unset.")]
        [SerializeField] private GameObject _root;
        [Tooltip("Prefab used to visualize EdgeCollider2D paths. Optional.")]
        [SerializeField] private LineRenderer _lineRenderer;
        // TODO: Perhaps mesh renderer should also take a prefab

        private Mesh _mesh;

        private void Awake()
        {
            GameObject root = _root != null ? _root : gameObject;

            MeshFilter meshFilter = GetComponent<MeshFilter>();

            var combineList = new List<CombineInstance>();
            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D collider in colliders)
            {
                if (collider == null) continue;

                if (collider is EdgeCollider2D edge)
                {
                    if (_lineRenderer == null || edge.pointCount < 2) continue;
                    LineRenderer lr = Instantiate(_lineRenderer, transform);
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

            _mesh = new Mesh();
            _mesh.CombineMeshes(combineList.ToArray(), true, true);

            foreach (CombineInstance ci in combineList)
                Destroy(ci.mesh);

            meshFilter.mesh = _mesh;
        }

        private CombineInstance? CreateCombineInstance(Collider2D collider)
        {
            // If a Rigidbody2D is available, use its transform
            Mesh mesh = collider.CreateMesh(true, true);
            if (mesh == null) return null;

            // Mesh vertices are in world space; transform to our local space
            return new CombineInstance { mesh = mesh, transform = transform.worldToLocalMatrix };
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }
    }
}
