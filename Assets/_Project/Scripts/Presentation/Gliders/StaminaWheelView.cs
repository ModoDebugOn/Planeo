using UnityEngine;

namespace Gliding.Presentation.Gliders
{
    /// <summary>
    /// A stamina wheel beside the character, facing the camera: a dim ring with a green arc that
    /// shrinks as the stamina drains (red when little is left). Hidden while there is nothing to
    /// show (not gliding, or a method without stamina).
    /// </summary>
    public sealed class StaminaWheelView : MonoBehaviour
    {
        const int Segments = 48;

        [SerializeField] Color fullColor = new Color(0.35f, 0.95f, 0.35f);
        [SerializeField] Color lowColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color ringColor = new Color(0f, 0f, 0f, 0.45f);
        [Tooltip("Below this much stamina the wheel turns red.")]
        [SerializeField, Range(0f, 1f)] float low = 0.25f;
        [SerializeField, Range(0.1f, 3f)] float radius = 0.7f;
        [SerializeField, Range(0.02f, 1f)] float width = 0.22f;
        [Tooltip("Where it sits, from the character's centre: metres to the screen's right and up.")]
        [SerializeField] Vector2 offset = new Vector2(1.6f, 1.2f);

        System.Func<Vector3> anchor;
        System.Func<float?> stamina;
        LineRenderer ring, fill;

        /// <param name="centre">The character's centre, in the world.</param>
        /// <param name="staminaLeft">0..1, or null to hide the wheel.</param>
        public void Bind(System.Func<Vector3> centre, System.Func<float?> staminaLeft)
        {
            anchor = centre;
            stamina = staminaLeft;
        }

        public void Configure(Material material)
        {
            ring = Line("Ring", material, 1);
            fill = Line("Fill", material, 0);
        }

        LineRenderer Line(string name, Material material, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.numCapVertices = 2;
            line.sortingOrder = -order;   // the fill over the ring
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        void LateUpdate()
        {
            var left = stamina?.Invoke();
            var camera = Camera.main;
            bool show = left.HasValue && anchor != null && camera != null && ring != null;
            if (ring != null) ring.enabled = show;
            if (fill != null) fill.enabled = show && left > 0f;
            if (!show) return;

            Vector3 right = camera.transform.right, up = camera.transform.up;
            Vector3 centre = anchor() + right * offset.x + up * offset.y;
            Draw(ring, centre, right, up, 1f, ringColor);
            Draw(fill, centre, right, up, left.Value, left.Value < low ? lowColor : fullColor);
        }

        /// <summary>An arc from the top, clockwise, covering that fraction of the circle.</summary>
        void Draw(LineRenderer line, Vector3 centre, Vector3 right, Vector3 up, float fraction, Color color)
        {
            int points = Mathf.Max(2, Mathf.CeilToInt(Segments * fraction) + 1);
            line.positionCount = points;
            line.loop = fraction >= 1f;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            for (int i = 0; i < points; i++)
            {
                float angle = fraction * i / (points - 1) * Mathf.PI * 2f;
                line.SetPosition(i, centre + (right * Mathf.Sin(angle) + up * Mathf.Cos(angle)) * radius);
            }
        }
    }
}
