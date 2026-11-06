using System.Numerics;
using Gliding.Domain.Air;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class ParagliderTests
    {
        const float Gravity = 20f;
        const float Dt = 1f / 120f;

        static FlightState Fly(IGlideModel model, FlightState state, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / Dt);
            for (int i = 0; i < steps; i++) state = model.Step(state, Gravity, Dt);
            return state;
        }

        static readonly FlightState Start = new FlightState(new Vector3(0f, 40f, 0f), new Vector3(0f, 0f, 8f), Vector3.UnitZ);

        [Test]
        public void Outside_the_updrafts_it_is_spyros_fixed_descent()
        {
            var later = Fly(new Paraglider(10f, 2.5f, new AirCurrents()), Start, 2f);

            Assert.That(later.HorizontalSpeed, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(later.VerticalSpeed, Is.EqualTo(-2.5f).Within(1e-4f));
        }

        [Test]
        public void Inside_an_updraft_it_goes_up()
        {
            var air = new AirCurrents();
            air.Add(new Updraft(-5f, 5f, -5f, 50f, 0f, 60f, liftSpeed: 6f));

            var later = Fly(new Paraglider(10f, 2.5f, air), Start, 1f);

            Assert.That(later.VerticalSpeed, Is.EqualTo(6f).Within(1e-4f));
            Assert.That(later.Position.Y, Is.EqualTo(46f).Within(0.05f));
        }

        [Test]
        public void With_the_updrafts_off_it_sinks_as_usual()
        {
            var air = new AirCurrents { Enabled = false };
            air.Add(new Updraft(-5f, 5f, -5f, 50f, 0f, 60f, liftSpeed: 6f));

            var later = Fly(new Paraglider(10f, 2.5f, air), Start, 1f);

            Assert.That(later.VerticalSpeed, Is.EqualTo(-2.5f).Within(1e-4f));
        }

        [Test]
        public void Above_the_top_of_the_updraft_there_is_no_lift()
        {
            var air = new AirCurrents();
            air.Add(new Updraft(-5f, 5f, -5f, 50f, 0f, 30f, liftSpeed: 6f));

            Assert.That(air.LiftAt(new Vector3(0f, 40f, 10f)), Is.EqualTo(0f));
            Assert.That(air.LiftAt(new Vector3(0f, 20f, 10f)), Is.EqualTo(6f));
        }
    }
}
