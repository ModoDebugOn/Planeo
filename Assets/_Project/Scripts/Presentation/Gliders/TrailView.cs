using System.Collections.Generic;
using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Presentation.Gliders
{
    /// <summary>
    /// Draws the path flown as a trail of dots, evenly spaced: pale during the jump (the same for
    /// every method) and in the method's colour once the glide opens, with a bigger dot where it
    /// opened. Pale again if it lets go of the glide (out of stamina). The trail stays after landing, so several of them can be compared.
    /// </summary>
    public sealed class TrailView : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] Material dotMaterial;
        [SerializeField] Color glideColor = Color.white;
        [SerializeField] Color jumpColor = new Color(0.93f, 0.93f, 0.93f);
        [Tooltip("Metres between dots.")]
        [SerializeField, Range(0.2f, 5f)] float spacing = 1f;
        [SerializeField, Range(0.05f, 1f)] float dotSize = 0.35f;
        [Tooltip("Size of the dot where the glide opens, compared to the others.")]
        [SerializeField, Range(1f, 4f)] float openingDotScale = 2.2f;

        System.Func<Trajectory> source;
        readonly List<Renderer> pool = new List<Renderer>();
        MaterialPropertyBlock block;

        public void Bind(System.Func<Trajectory> trajectory) => source = trajectory;

        /// <summary>For views created from code (the composition root).</summary>
        public void Configure(Material material, Color color)
        {
            dotMaterial = material;
            glideColor = color;
        }

        void Awake() => block = new MaterialPropertyBlock();

        void LateUpdate()
        {
            var trajectory = source?.Invoke();
            int used = 0;
            if (trajectory != null && trajectory.Count > 0)
            {
                var points = trajectory.Points;
                Vector3 last = points[0].Position.ToUnity();
                bool opened = false;
                Place(Dot(used++), last, points[0].Phase, dotSize);
                for (int i = 1; i < points.Count; i++)
                {
                    var point = points[i];
                    Vector3 position = point.Position.ToUnity();
                    bool opening = !opened && point.Phase == FlightPhase.Gliding;
                    if (opening) opened = true;
                    if (!opening && Vector3.Distance(position, last) < spacing) continue;
                    Place(Dot(used++), position, point.Phase, opening ? dotSize * openingDotScale : dotSize);
                    last = position;
                }
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
        }

        void Place(Renderer dot, Vector3 position, FlightPhase phase, float size)
        {
            dot.enabled = true;
            dot.transform.SetPositionAndRotation(position, Quaternion.identity);
            dot.transform.localScale = Vector3.one * size;
            // Pale wherever it is plain gravity: the jump, and the fall after letting go of the glide.
            bool plainGravity = phase == FlightPhase.Jumping || phase == FlightPhase.Falling;
            block.SetColor(BaseColor, plainGravity ? jumpColor : glideColor);
            dot.SetPropertyBlock(block);
        }

        Renderer Dot(int index)
        {
            while (pool.Count <= index)
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(dot.GetComponent<Collider>());
                dot.name = $"Dot {pool.Count}";
                dot.transform.SetParent(transform, false);
                var renderer = dot.GetComponent<Renderer>();
                renderer.sharedMaterial = dotMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pool.Add(renderer);
            }
            return pool[index];
        }
    }
}
