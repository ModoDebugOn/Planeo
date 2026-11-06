using System;
using System.Numerics;
using Gliding.Application;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using Gliding.Domain.Terrain;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class DiveGlideTests
    {
        const float Gravity = 20f;
        const float Dt = 1f / 120f;

        static FlightState Fly(IGlideModel model, FlightState state, float seconds)
        {
            int steps = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++) state = model.Step(state, Gravity, Dt);
            return state;
        }

        static FlightState Moving(float speed) => new FlightState(new Vector3(0f, 100f, 0f), Vector3.UnitZ * speed, Vector3.UnitZ);

        /// <summary>The whole jump off a 40 m ledge, flown with the stick as <paramref name="pilot"/>
        /// says (seconds since the glide opened → stick). Returns where it ended.</summary>
        static float JumpDistance(Func<float, float> pilot)
        {
            var ground = new Ground();
            ground.Add(new Block(-10f, 10f, -24f, 0f, 40f));
            var world = new FlightWorld(ground, new JumpSettings(8f, 7f, 20f));
            var stick = new DiveStick();
            var glider = world.Add("Batman", new DiveGlide(stick), new Vector3(0f, 40f, -4f), Vector3.UnitZ);
            world.LaunchAll();

            float gliding = 0f;
            for (int i = 0; i < 120 * 120 && !glider.Finished; i++)
            {
                if (glider.Phase == FlightPhase.Gliding) gliding += FlightWorld.StepSeconds;
                stick.Dive = pilot(gliding);
                world.StepOnce();
            }
            return glider.End.Value.Z;
        }

        [Test]
        public void Diving_turns_height_into_speed()
        {
            var stick = new DiveStick { Dive = 1f };
            var model = new DiveGlide(stick);
            var start = Moving(10f);

            var later = Fly(model, start, 1f);

            Assert.That(later.Velocity.Length(), Is.GreaterThan(18f));
            Assert.That(later.Position.Y, Is.LessThan(start.Position.Y - 10f));
        }

        [Test]
        public void Pulling_up_turns_speed_into_height()
        {
            var stick = new DiveStick { Dive = -1f };
            var model = new DiveGlide(stick);
            var start = Moving(25f);

            var later = Fly(model, start, 0.5f);

            Assert.That(later.Velocity.Length(), Is.LessThan(20f));
            Assert.That(later.Position.Y, Is.GreaterThan(start.Position.Y));
        }

        [Test]
        public void Without_speed_the_nose_cannot_go_flatter_than_the_normal_glide()
        {
            var stick = new DiveStick { Dive = -1f };   // pulling up as hard as possible…
            var model = new DiveGlide(stick);

            var next = model.Step(Moving(5f), Gravity, Dt);   // …but far too slow

            float angle = (float)(Math.Atan2(-next.Velocity.Y, next.Velocity.Z) * 180.0 / Math.PI);
            Assert.That(angle, Is.EqualTo(model.NeutralPitch).Within(0.01f));
        }

        [Test]
        public void Only_the_stick_changes_the_flight()
        {
            var stick = new DiveStick();
            var model = new DiveGlide(stick);
            var start = Moving(15f);

            var handsOff = model.Step(start, Gravity, Dt);
            stick.Dive = 1f;
            var diving = model.Step(start, Gravity, Dt);

            Assert.That(diving.Velocity.Y, Is.LessThan(handsOff.Velocity.Y));
        }

        [Test]
        public void The_autopilot_dives_first_and_then_pulls_back()
        {
            float glideTime = 0f;
            var hand = new DiveStick();
            var pilot = new DiveAutopilot(hand, () => glideTime, diveSeconds: 1.5f, then: -1f);

            Assert.That(pilot.Dive, Is.EqualTo(1f));
            glideTime = 1.6f;
            Assert.That(pilot.Dive, Is.EqualTo(-1f));
        }

        [Test]
        public void Touching_the_stick_takes_over_from_the_autopilot()
        {
            var hand = new DiveStick();
            var pilot = new DiveAutopilot(hand, () => 0f, diveSeconds: 1.5f, then: -1f);

            hand.Dive = -0.5f;

            Assert.That(pilot.Dive, Is.EqualTo(-0.5f));
        }

        [Test]
        public void Diving_and_pulling_back_climbs_back_up_for_a_while()
        {
            var ground = new Ground();
            ground.Add(new Block(-10f, 10f, -24f, 0f, 40f));
            var world = new FlightWorld(ground, new JumpSettings(8f, 7f, 20f));
            Glider glider = null;
            var pilot = new DiveAutopilot(new DiveStick(), () => glider.GlideTime, diveSeconds: 1.5f, then: -1f);
            glider = world.Add("Batman", new DiveGlide(pilot), new Vector3(0f, 40f, -4f), Vector3.UnitZ);
            world.LaunchAll();

            float lowest = float.MaxValue, climbed = 0f;
            for (int i = 0; i < 120 * 120 && !glider.Finished; i++)
            {
                world.StepOnce();
                if (glider.Phase != FlightPhase.Gliding) continue;
                lowest = Math.Min(lowest, glider.State.Position.Y);
                climbed = Math.Max(climbed, glider.State.Position.Y - lowest);
            }

            Assert.That(climbed, Is.GreaterThan(5f), "the pull-up has to be visible");
            Assert.That(glider.End.Value.Z, Is.GreaterThan(169f), "and it goes further than Spyro");
        }

        [Test]
        public void The_same_jump_ends_very_far_or_very_short_depending_on_the_pilot()
        {
            float divingAllTheWay = JumpDistance(t => 1f);
            float handsOff = JumpDistance(t => 0f);
            float pullingBack = JumpDistance(t => -1f);

            Assert.That(divingAllTheWay, Is.LessThan(30f));
            Assert.That(handsOff, Is.GreaterThan(divingAllTheWay * 3f));
            Assert.That(pullingBack, Is.GreaterThan(handsOff * 1.5f));
        }
    }
}
