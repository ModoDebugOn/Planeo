using System.Collections.Generic;
using UnityEngine;

namespace Gliding.Presentation.Air
{
    /// <summary>
    /// An updraft made visible: thin streaks of wind rising inside its box and starting again at the
    /// bottom. Shown only while the updrafts are on.
    /// </summary>
    public sealed class UpdraftView : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] Color color = new Color(0.88f, 0.96f, 1f);
        [Tooltip("Streaks per square metre of ground.")]
        [SerializeField, Range(0.05f, 2f)] float density = 0.5f;
        [SerializeField, Range(0.5f, 6f)] float streakLength = 2.5f;
        [SerializeField, Range(0.02f, 0.5f)] float streakWidth = 0.08f;
        [Tooltip("How fast they rise compared to the real lift (more reads better on video).")]
        [SerializeField, Range(0.5f, 5f)] float speedFactor = 2f;

        System.Func<bool> enabledSource;
        Vector3 bottom, size;
        float speed;
        readonly List<(Transform streak, float x, float z, float y)> streaks = new List<(Transform, float, float, float)>();

        public void Bind(System.Func<bool> on) => enabledSource = on;

        /// <param name="bottomCentre">Bottom centre of the box, in the world.</param>
        /// <param name="boxSize">Width (X), height (Y) and length (Z).</param>
        /// <param name="liftSpeed">m/s, as in the simulation.</param>
        public void Configure(Material material, Vector3 bottomCentre, Vector3 boxSize, float liftSpeed)
        {
            bottom = bottomCentre;
            size = boxSize;
            speed = liftSpeed * speedFactor;

            var random = new System.Random(7);   // the same streaks every time
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            int count = Mathf.Clamp(Mathf.RoundToInt(size.x * size.z * density), 4, 300);
            for (int i = 0; i < count; i++)
            {
                var streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(streak.GetComponent<Collider>());
                streak.name = "Streak " + i;
                streak.transform.SetParent(transform, false);
                streak.transform.localScale = new Vector3(streakWidth, streakLength, streakWidth);
                var renderer = streak.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.SetPropertyBlock(block);
                streaks.Add((streak.transform, Next(random) - 0.5f, Next(random) - 0.5f, Next(random)));
            }
        }

        static float Next(System.Random random) => (float)random.NextDouble();

        void LateUpdate()
        {
            bool on = enabledSource == null || enabledSource();
            float rise = size.y <= 0f ? 0f : speed * Time.time / size.y;
            for (int i = 0; i < streaks.Count; i++)
            {
                var (streak, x, z, y) = streaks[i];
                streak.gameObject.SetActive(on);
                if (!on) continue;
                float height = Mathf.Repeat(y + rise, 1f) * size.y;
                streak.position = bottom + new Vector3(x * size.x, height, z * size.z);
            }
        }
    }
}
