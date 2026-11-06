using System.Numerics;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class FixedDescentTests
    {
        const float Gravity = 20f;
        const float Dt = 1f / 120f;

        static readonly FixedDescent Spyro = new FixedDescent(forwardSpeed: 10f, sinkSpeed: 2.5f);

        [Test]
        public void The_fall_stops_dead_on_the_first_step()
        {
            // Coming in fast, falling and moving sideways: none of it survives the first step.
            var state = new FlightState(Vector3.Zero, new Vector3(3f, -15f, 8f), Vector3.UnitZ);

            var next = Spyro.Step(state, Gravity, Dt);

            Assert.That(next.VerticalSpeed, Is.EqualTo(-2.5f).Within(1e-5f));
            Assert.That(next.HorizontalSpeed, Is.EqualTo(10f).Within(1e-5f));
        }

        [Test]
        public void Gravity_does_not_matter()
        {
            var state = new FlightState(Vector3.Zero, Vector3.Zero, Vector3.UnitZ);

            var onEarth = Spyro.Step(state, 9.8f, Dt);
            var onJupiter = Spyro.Step(state, 25f, Dt);

            Assert.That(onJupiter.Velocity, Is.EqualTo(onEarth.Velocity));
        }

        [Test]
        public void The_path_is_a_straight_line_with_the_glide_ratio_as_slope()
        {
            var state = new FlightState(new Vector3(0f, 40f, 0f), Vector3.Zero, Vector3.UnitZ);
            for (int i = 0; i < 240; i++) state = Spyro.Step(state, Gravity, Dt);   // 2 s

            // 2 s: 20 m forward, 5 m down. Four metres forward per metre down.
            Assert.That(state.Position.Z, Is.EqualTo(20f).Within(1e-3f));
            Assert.That(state.Position.Y, Is.EqualTo(35f).Within(1e-3f));
            Assert.That(Spyro.GlideRatio, Is.EqualTo(4f));
        }

        [Test]
        public void It_keeps_going_the_way_it_faces()
        {
            var state = new FlightState(Vector3.Zero, Vector3.Zero, Vector3.UnitX);

            var next = Spyro.Step(state, Gravity, Dt);

            Assert.That(next.Velocity.X, Is.EqualTo(10f).Within(1e-5f));
            Assert.That(next.Velocity.Z, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(next.Facing, Is.EqualTo(Vector3.UnitX));
        }
    }
}
