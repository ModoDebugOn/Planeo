using System.Numerics;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class ReducedGravityTests
    {
        const float Gravity = 20f;
        const float Dt = 1f / 120f;

        static readonly ReducedGravity Tanuki = new ReducedGravity(gravityScale: 0.15f);

        static FlightState Fly(IGlideModel model, FlightState state, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++) state = model.Step(state, Gravity, Dt);
            return state;
        }

        [Test]
        public void It_keeps_falling_faster_and_faster()
        {
            var start = new FlightState(new Vector3(0f, 40f, 0f), new Vector3(0f, 0f, 8f), Vector3.UnitZ);

            var after1 = Fly(Tanuki, start, 1f);
            var after2 = Fly(Tanuki, start, 2f);

            // 0.15 · 20 = 3 m/s² downwards: 3 m/s after one second, 6 after two.
            Assert.That(after1.VerticalSpeed, Is.EqualTo(-3f).Within(1e-3f));
            Assert.That(after2.VerticalSpeed, Is.EqualTo(-6f).Within(1e-3f));
        }

        [Test]
        public void It_keeps_the_horizontal_speed_it_came_with()
        {
            var start = new FlightState(new Vector3(0f, 40f, 0f), new Vector3(0f, 0f, 8f), Vector3.UnitZ);

            var later = Fly(Tanuki, start, 3f);

            Assert.That(later.HorizontalSpeed, Is.EqualTo(8f).Within(1e-4f));
        }

        [Test]
        public void Without_the_suit_it_is_plain_gravity()
        {
            var start = new FlightState(Vector3.Zero, new Vector3(0f, 0f, 8f), Vector3.UnitZ);

            var later = Fly(new ReducedGravity(1f), start, 1f);

            Assert.That(later.VerticalSpeed, Is.EqualTo(-Gravity).Within(1e-3f));
        }

        [Test]
        public void The_path_is_a_curve_not_a_line()
        {
            var start = new FlightState(new Vector3(0f, 40f, 0f), new Vector3(0f, 0f, 8f), Vector3.UnitZ);

            // Same horizontal distance each second, more height lost each second.
            var a = Fly(Tanuki, start, 1f);
            var b = Fly(Tanuki, start, 2f);
            float drop1 = start.Position.Y - a.Position.Y;
            float drop2 = a.Position.Y - b.Position.Y;

            Assert.That(drop2, Is.EqualTo(drop1 * 3f).Within(0.05f));   // 1.5 m, then 4.5 m
        }
    }
}
