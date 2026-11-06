using System.Collections.Generic;
using Gliding.Application;
using Gliding.Domain.Air;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using Gliding.Domain.Terrain;
using Gliding.Infrastructure.Profiles;
using Gliding.Presentation;
using Gliding.Presentation.Cameras;
using UnityEngine;

namespace Gliding.Infrastructure
{
    /// <summary>
    /// Composition root of the demo: the one place where the layers meet. It reads the scene into
    /// the simulation (the ground from the colliders, the updrafts from the UpdraftZones), turns
    /// each character with a method into a glider, hands them to the drawing, and drives it all
    /// frame by frame.
    ///
    /// Every glider uses the same jump (set here, once). Only the method changes, and it comes from
    /// the profile asset: swap the asset, or change its numbers in Play mode, and the next step
    /// already glides the new way.
    ///
    /// They jump by turns, in the order of the list: the cameras follow the one whose turn it is
    /// and go back to the ledge for the next one.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(FlightDrawing))]
    public sealed class FlightBootstrap : MonoBehaviour
    {
        /// <summary>A character and the way it glides. No profile: it just stands there.</summary>
        [System.Serializable]
        sealed class Slot
        {
            public Transform character;
            public GlideProfile profile;
        }

        [Header("Gliders (they jump in this order)")]
        [SerializeField] Slot[] gliders;
        [Tooltip("Seconds watching a landing before the turn passes to the next one.")]
        [SerializeField, Range(0f, 5f)] float afterLanding = 1f;
        [Tooltip("Seconds between the turn passing and the next one jumping: the camera's trip back.")]
        [SerializeField, Range(0f, 5f)] float beforeJump = 1.5f;

        [Header("The jump (the same for everybody)")]
        [Tooltip("Run-up speed, and horizontal speed of the jump, in m/s.")]
        [SerializeField, Range(1f, 20f)] float runSpeed = 8f;
        [Tooltip("Upward speed when leaving the edge, in m/s.")]
        [SerializeField, Range(0f, 20f)] float jumpSpeed = 7f;
        [Tooltip("m/s². Games use more than the real 9.8 so jumps feel snappy.")]
        [SerializeField, Range(1f, 40f)] float gravity = 20f;

        [Header("Cameras")]
        [Tooltip("The shots (side, overview, chase): they follow the one whose turn it is.")]
        [SerializeField] CameraShots cameras;

        [Header("Ground")]
        [Tooltip("Everything you can land on: every BoxCollider under it (axis-aligned).")]
        [SerializeField] Transform solids;

        [Header("Air")]
        [SerializeField] bool updraftsOn = true;

        public FlightWorld World { get; private set; }
        public TakeTurns Turns { get; private set; }

        readonly List<(Glider glider, GlideProfile profile, int version)> bound = new List<(Glider, GlideProfile, int)>();
        readonly AirCurrents air = new AirCurrents();
        FlightDrawing drawing;

        JumpSettings Jump => new JumpSettings(runSpeed, jumpSpeed, gravity);

        // The player's hand on the dive, set from the keyboard every frame. Only the methods that
        // can dive get it (with their autopilot, which needs to know how long its glider has been
        // gliding); the rest never see it. The same goes for the updrafts.
        readonly DiveStick diveStick = new DiveStick();

        IGlideModel CreateModel(GlideProfile profile, System.Func<Glider> glider) => profile switch
        {
            DiveGlideProfile dive => dive.CreateModel(diveStick, () => glider()?.GlideTime ?? 0f),
            ParagliderProfile paraglider => paraglider.CreateModel(air),
            _ => profile.CreateModel(),
        };

        void Awake()
        {
            drawing = GetComponent<FlightDrawing>();
            ReadAir();
            World = new FlightWorld(ReadGround(), Jump);
            foreach (var slot in gliders)
                if (slot.character != null && slot.profile != null)
                    AddGlider(slot.character, slot.profile);

            Turns = new TakeTurns(World);
            // Before the first turn, the camera waits by the first one in the list.
            if (cameras != null && World.Gliders.Count > 0)
                cameras.Bind(() => (Turns.Current ?? World.Gliders[0]).State);
        }

        /// <summary>The top of every box collider, as a block to land on.</summary>
        Ground ReadGround()
        {
            var ground = new Ground();
            if (solids == null) return ground;
            foreach (var box in solids.GetComponentsInChildren<BoxCollider>())
            {
                var b = box.bounds;
                ground.Add(new Block(b.min.x, b.max.x, b.min.z, b.max.z, b.max.y));
            }
            return ground;
        }

        /// <summary>Every updraft of the scene into the simulation, each with its wind drawn.</summary>
        void ReadAir()
        {
            air.Enabled = updraftsOn;
            foreach (var zone in FindObjectsByType<UpdraftZone>())
            {
                air.Add(zone.ToDomain());
                drawing.DrawUpdraft(zone.name, zone.Base, zone.Size, zone.LiftSpeed, () => air.Enabled);
            }
        }

        void AddGlider(Transform character, GlideProfile profile)
        {
            // The simulation tracks the feet: the bottom of the capsule.
            var feet = character.position - Vector3.up * character.lossyScale.y;
            Glider glider = null;   // the model may need it, and it needs the model: filled right below
            glider = World.Add(character.name, CreateModel(profile, () => glider), feet.ToNumerics(), character.forward.ToNumerics());
            glider.Stamina = profile.CreateStamina();
            bound.Add((glider, profile, profile.Version));
            drawing.Draw(glider, character, profile.Color);
        }

        void Update()
        {
            if (DemoInput.LaunchPressed) Turns.Start();
            if (DemoInput.ResetPressed) World.ResetAll();
            if (DemoInput.TogglePausePressed) World.Paused = !World.Paused;

            float scale = DemoInput.TimeScalePressed;
            if (scale > 0f) World.TimeScale = scale;

            if (DemoInput.ToggleTrailsPressed) drawing.ShowTrails = !drawing.ShowTrails;
            if (DemoInput.ToggleVectorsPressed) drawing.ShowVectors = !drawing.ShowVectors;
            if (DemoInput.ToggleCameraPressed && cameras != null) cameras.Next();
            if (DemoInput.ToggleUpdraftsPressed) updraftsOn = !updraftsOn;

            ApplySettings();
            diveStick.Dive = DemoInput.Dive;
            World.Advance(Time.deltaTime);
            // The waits between turns are simulated time too: in slow motion they last longer.
            if (!World.Paused) Turns.Advance(Time.deltaTime * World.TimeScale);
        }

        /// <summary>The sliders, live: the jump from this component, each method from its profile.
        /// A changed profile also brings a new, full stamina bar.</summary>
        void ApplySettings()
        {
            World.Jump = Jump;
            air.Enabled = updraftsOn;
            Turns.AfterLanding = afterLanding;
            Turns.BeforeJump = beforeJump;
            for (int i = 0; i < bound.Count; i++)
            {
                var (glider, profile, version) = bound[i];
                if (profile.Version == version) continue;
                glider.Model = CreateModel(profile, () => glider);
                glider.Stamina = profile.CreateStamina();
                bound[i] = (glider, profile, profile.Version);
            }
        }
    }
}
