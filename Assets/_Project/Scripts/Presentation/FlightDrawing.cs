using System.Collections.Generic;
using Gliding.Domain.Flight;
using Gliding.Presentation.Air;
using Gliding.Presentation.Gliders;
using UnityEngine;

namespace Gliding.Presentation
{
    /// <summary>
    /// Everything drawn over the flight, in layers that switch on and off for the video:
    /// - Trails and landing marks (T): for comparing the methods.
    /// - Arrows (V): the velocity, its two parts, and the gravity the character really feels.
    /// - Always: the character's lean, the stamina wheel (only if it tires) and the wind.
    /// It only reads the simulation; whoever builds the demo tells it what to draw.
    /// </summary>
    public sealed class FlightDrawing : MonoBehaviour
    {
        [SerializeField] Material dotMaterial;
        [SerializeField] Material lineMaterial;

        [Header("Layers")]
        [SerializeField] bool showTrails = true;
        [SerializeField] bool showVectors = true;

        [Header("Arrows")]
        [Tooltip("Arrows show where the velocity would take the character in this many seconds.")]
        [SerializeField, Range(0.1f, 2f)] float arrowSeconds = 0.5f;
        [SerializeField] Color velocityColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] Color horizontalColor = new Color(0.25f, 0.8f, 1f);
        [SerializeField] Color verticalColor = new Color(1f, 0.3f, 0.3f);
        [Tooltip("The gravity the character really feels (how its velocity changes), drawn from its back.")]
        [SerializeField] Color gravityColor = new Color(1f, 0.3f, 0.85f);
        [Tooltip("Metres of arrow per m/s² of gravity: with 0.4, a gravity of 20 m/s² is 8 m.")]
        [SerializeField, Range(0.05f, 1f)] float gravityArrowScale = 0.4f;
        [Tooltip("Longest gravity arrow, in metres: the jolt of opening a glide lasts a single step.")]
        [SerializeField, Range(1f, 40f)] float maxGravityArrow = 16f;

        readonly List<GameObject> trails = new List<GameObject>();
        readonly List<GameObject> arrows = new List<GameObject>();

        public bool ShowTrails
        {
            get => showTrails;
            set { showTrails = value; ApplyVisibility(); }
        }

        public bool ShowVectors
        {
            get => showVectors;
            set { showVectors = value; ApplyVisibility(); }
        }

        /// <summary>Everything one glider needs: its lean, trail, landing mark, arrows and stamina wheel.</summary>
        /// <param name="color">The method's colour (trail and landing mark).</param>
        public void Draw(Glider glider, Transform character, Color color)
        {
            if (!character.TryGetComponent(out GliderView view)) view = character.gameObject.AddComponent<GliderView>();
            view.Bind(() => glider.State);

            var trail = Create<TrailView>($"{character.name} Trail", trails);
            trail.Configure(dotMaterial, color);
            trail.Bind(() => glider.Trajectory);

            // The landing mark goes with the trails: both are for comparing.
            var mark = Create<LandingMarkView>($"{character.name} Landing", trails);
            mark.Configure(dotMaterial, color);
            mark.Bind(() => glider.Finished ? glider.End?.ToUnity() : null);

            // Velocity and its two parts, from the centre of the character.
            Arrow(character.name + " Velocity", velocityColor, 0.3f, () => Velocity(glider, v => v));
            Arrow(character.name + " Horizontal", horizontalColor, 0.2f, () => Velocity(glider, v => new Vector3(v.x, 0f, v.z)));
            Arrow(character.name + " Vertical", verticalColor, 0.2f, () => Velocity(glider, v => new Vector3(0f, v.y, 0f)));
            Arrow(character.name + " Gravity", gravityColor, 0.2f, () => Gravity(glider));

            // The stamina wheel: only if it tires, from the glide until it lands.
            var wheel = Create<StaminaWheelView>($"{character.name} Stamina", null);
            wheel.Configure(lineMaterial);
            wheel.Bind(() => glider.State.Position.ToUnity() + Vector3.up,
                       () => glider.Stamina != null && (glider.Phase == FlightPhase.Gliding || glider.Phase == FlightPhase.Falling)
                           ? glider.Stamina.Fraction
                           : (float?)null);
        }

        /// <summary>The wind streaks of an updraft, shown while <paramref name="on"/> says so.</summary>
        public void DrawUpdraft(string name, Vector3 bottomCentre, Vector3 size, float liftSpeed, System.Func<bool> on)
        {
            var wind = Create<UpdraftView>($"{name} Wind", null);
            wind.Configure(dotMaterial, bottomCentre, size, liftSpeed);
            wind.Bind(on);
        }

        /// <summary>A view as a child of this object, added to a layer (null: always shown).</summary>
        T Create<T>(string name, List<GameObject> layer) where T : Component
        {
            var view = new GameObject(name).AddComponent<T>();
            view.transform.SetParent(transform, false);
            if (layer != null)
            {
                layer.Add(view.gameObject);
                view.gameObject.SetActive(layer == trails ? showTrails : showVectors);
            }
            return view;
        }

        void Arrow(string name, Color color, float width, System.Func<VectorArrowView.Arrow?> source)
        {
            var arrow = Create<VectorArrowView>(name, arrows);
            arrow.Configure(lineMaterial, color, width);
            arrow.Bind(source);
        }

        VectorArrowView.Arrow? Velocity(Glider glider, System.Func<Vector3, Vector3> part)
        {
            if (!glider.InTheAir) return null;
            var state = glider.State;
            var centre = state.Position.ToUnity() + Vector3.up;
            return new VectorArrowView.Arrow(centre, part(state.Velocity.ToUnity()) * arrowSeconds);
        }

        /// <summary>The gravity it feels, from behind its centre so it does not hide under the red
        /// vertical arrow (or a tail).</summary>
        VectorArrowView.Arrow? Gravity(Glider glider)
        {
            if (!glider.InTheAir) return null;
            var state = glider.State;
            var origin = state.Position.ToUnity() + Vector3.up - state.Facing.ToUnity() * 1.6f;
            var vector = Vector3.ClampMagnitude(glider.Acceleration.ToUnity() * gravityArrowScale, maxGravityArrow);
            return new VectorArrowView.Arrow(origin, vector);
        }

        void ApplyVisibility()
        {
            foreach (var trail in trails) trail.SetActive(showTrails);
            foreach (var arrow in arrows) arrow.SetActive(showVectors);
        }

        void OnValidate() => ApplyVisibility();
    }
}
