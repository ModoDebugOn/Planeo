using System.Numerics;
using Gliding.Application;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using Gliding.Domain.Terrain;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class TakeTurnsTests
    {
        const float Dt = FlightWorld.StepSeconds;

        FlightWorld world;
        TakeTurns turns;
        Glider spyro, mario;

        [SetUp]
        public void SetUp()
        {
            var ground = new Ground();
            ground.Add(new Block(-10f, 10f, -20f, 0f, 40f));
            world = new FlightWorld(ground, new JumpSettings(8f, 7f, 20f));
            spyro = world.Add("Spyro", new FixedDescent(10f, 2.5f), new Vector3(4.5f, 0f, -4f), Vector3.UnitZ);
            mario = world.Add("Mario", new ReducedGravity(0.15f), new Vector3(1.5f, 0f, -4f), Vector3.UnitZ);
            turns = new TakeTurns(world) { AfterLanding = 1f, BeforeJump = 1.5f };
        }

        void Step(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                world.StepOnce();
                turns.Advance(Dt);
            }
        }

        void StepUntil(System.Func<bool> condition)
        {
            for (int i = 0; i < 120 * 120 && !condition(); i++) Step();
        }

        [Test]
        public void Only_the_first_one_jumps_at_the_start()
        {
            turns.Start();
            Step(60);

            Assert.That(turns.Current, Is.SameAs(spyro));
            Assert.That(spyro.Phase, Is.Not.EqualTo(FlightPhase.Standing));
            Assert.That(mario.Phase, Is.EqualTo(FlightPhase.Standing));
        }

        [Test]
        public void The_turn_passes_a_moment_after_landing_and_the_next_one_jumps_later()
        {
            turns.Start();
            StepUntil(() => spyro.Finished);

            Step(119);   // just under a second watching the landing
            Assert.That(turns.Current, Is.SameAs(spyro));

            StepUntil(() => turns.Current == mario);
            Assert.That(mario.Phase, Is.EqualTo(FlightPhase.Standing), "the camera goes back first");

            StepUntil(() => mario.Phase != FlightPhase.Standing);
            Assert.That(mario.Phase, Is.EqualTo(FlightPhase.Running));
        }

        [Test]
        public void The_trajectories_already_flown_stay()
        {
            turns.Start();
            StepUntil(() => spyro.Finished);
            int points = spyro.Trajectory.Count;

            StepUntil(() => mario.Finished);

            Assert.That(spyro.Trajectory.Count, Is.EqualTo(points));
            Assert.That(spyro.Phase, Is.EqualTo(FlightPhase.Landed));
            Assert.That(turns.Running, Is.False);
        }

        [Test]
        public void Starting_again_puts_everybody_back_on_the_ledge()
        {
            turns.Start();
            StepUntil(() => mario.Finished);

            turns.Start();

            Assert.That(turns.Current, Is.SameAs(spyro));
            Assert.That(mario.Phase, Is.EqualTo(FlightPhase.Standing));
            Assert.That(mario.Trajectory.Count, Is.EqualTo(0));
        }
    }
}
