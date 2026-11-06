using UnityEngine;

namespace Gliding.Presentation.Gliders
{
    /// <summary>
    /// An arrow in the world (a velocity, a gravity…), drawn with a LineRenderer so the cameras see
    /// it and the recording kit records it (Gizmos only show in the editor). The head always faces
    /// the camera, so the arrow reads from any side.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class VectorArrowView : MonoBehaviour
    {
        /// <summary>Where the arrow starts and how long it is (already in metres).</summary>
        public readonly struct Arrow
        {
            public readonly Vector3 Origin;
            public readonly Vector3 Vector;

            public Arrow(Vector3 origin, Vector3 vector)
            {
                Origin = origin;
                Vector = vector;
            }
        }

        [SerializeField] Color color = Color.yellow;
        [SerializeField, Range(0.02f, 1f)] float width = 0.25f;
        [Tooltip("Length of the head, as a fraction of the arrow (capped by Max Head).")]
        [SerializeField, Range(0.05f, 0.5f)] float headFraction = 0.25f;
        [SerializeField, Range(0.1f, 5f)] float maxHead = 1.5f;

        System.Func<Arrow?> source;
        LineRenderer line;

        public void Bind(System.Func<Arrow?> arrow) => source = arrow;

        /// <summary>For views created from code (the composition root).</summary>
        public void Configure(Material material, Color arrowColor, float arrowWidth)
        {
            color = arrowColor;
            width = arrowWidth;
            Line.sharedMaterial = material;
        }

        LineRenderer Line
        {
            get
            {
                if (line != null) return line;
                line = GetComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 5;
                line.numCapVertices = 2;
                line.numCornerVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return line;
            }
        }

        void LateUpdate()
        {
            var arrow = source?.Invoke();
            if (arrow == null || arrow.Value.Vector.sqrMagnitude < 1e-4f)
            {
                Line.enabled = false;
                return;
            }

            Vector3 origin = arrow.Value.Origin, tip = origin + arrow.Value.Vector;
            Vector3 direction = arrow.Value.Vector.normalized;
            float head = Mathf.Min(arrow.Value.Vector.magnitude * headFraction, maxHead);

            // The two strokes of the head, in the plane that faces the camera.
            var camera = Camera.main;
            Vector3 view = camera != null ? (tip - camera.transform.position).normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(direction, view);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(direction, Vector3.up);
            side = side.normalized * head * 0.6f;
            Vector3 back = tip - direction * head;

            Line.enabled = true;
            Line.startColor = Line.endColor = color;
            Line.startWidth = Line.endWidth = width;
            Line.SetPosition(0, origin);
            Line.SetPosition(1, tip);
            Line.SetPosition(2, back + side);
            Line.SetPosition(3, tip);
            Line.SetPosition(4, back - side);
        }
    }
}
