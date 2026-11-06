using System.Numerics;
using Gliding.Application;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using Gliding.Domain.Terrain;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class GliderTests
    {
        static readonly JumpSettings Jump = new JumpSettings(runSpeed: 8f, jumpSpeed: 7f, gravity: 20f);

        /// <summary>A 40 m ledge ending at z = 0, over a floor at 0.</summary>
        static Ground Ledge()
        {
            var ground = new Ground();
            ground.Add(new Block(-10f, 10f, -20f, 0f, 40f));
            return ground;
        }

        static FlightWorld WorldWith(Ground ground, IGlideModel model, out Glider glider)
        {
            var world = new FlightWorld(ground, Jump);
            glider = world.Add("Test", model, new Vector3(0f, 0f, -4f), Vector3.UnitZ);
            return world;
        }

        static void RunUntilFinished(FlightWorld world, Glider glider)
        {
            for (int i = 0; i < 120 * 60 && !glider.Finished; i++) world.StepOnce();
        }

        [Test]
        public void Starts_standing_on_the_ground_under_its_feet()
        {
            WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);

            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Standing));
            Assert.That(glider.State.Position.Y, Is.EqualTo(40f));
        }

        [Test]
        public void Runs_to_the_edge_jumps_and_opens_the_glide_at_the_top_of_the_jump()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();

            while (glider.Phase == FlightPhase.Running) world.StepOnce();
            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Jumping));
            Assert.That(glider.State.Position.Z, Is.GreaterThan(0f).And.LessThan(0.2f));

            while (glider.Phase == FlightPhase.Jumping) world.StepOnce();
            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Gliding));
            // Top of a 7 m/s jump with g = 20: 7² / (2·20) ≈ 1.2 m above the ledge.
            Assert.That(glider.GlideStart.Value.Y, Is.EqualTo(41.2f).Within(0.1f));
        }

        [Test]
        public void Fixed_descent_lands_where_the_glide_ratio_says()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Landed));
            Assert.That(glider.State.Position.Y, Is.EqualTo(0f));
            // From the top of the jump: 4 m forward per metre down.
            var top = glider.GlideStart.Value;
            Assert.That(glider.End.Value.Z, Is.EqualTo(top.Z + top.Y * 4f).Within(0.2f));
        }

        [Test]
        public void The_same_jump_always_ends_in_the_same_place()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);

            world.LaunchAll();
            RunUntilFinished(world, glider);
            var first = glider.End.Value;

            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.End.Value, Is.EqualTo(first));
        }

        [Test]
        public void Hitting_the_side_of_a_platform_is_a_crash()
        {
            var ground = Ledge();
            ground.Add(new Block(-10f, 10f, 100f, 110f, 30f));   // too high to land on, in the way
            var world = WorldWith(ground, new FixedDescent(10f, 2.5f), out var glider);

            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Crashed));
            Assert.That(glider.End.Value.Z, Is.LessThan(100f));
        }

        [Test]
        public void Landing_on_a_platform_counts_its_height()
        {
            var ground = Ledge();
            ground.Add(new Block(-10f, 10f, 100f, 200f, 10f));   // a wide, low one where it comes down
            var world = WorldWith(ground, new FixedDescent(10f, 2.5f), out var glider);

            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Landed));
            Assert.That(glider.State.Position.Y, Is.EqualTo(10f));
        }

        [Test]
        public void The_method_can_be_swapped_between_jumps()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();
            RunUntilFinished(world, glider);
            float slow = glider.End.Value.Z;

            glider.Model = new FixedDescent(10f, 1.25f);   // half the sink: twice as far
            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.End.Value.Z, Is.GreaterThan(slow * 1.8f));
        }

        [Test]
        public void Same_character_same_jump_another_method()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();
            RunUntilFinished(world, glider);
            var spyroTop = glider.GlideStart.Value;
            float spyro = glider.End.Value.Z;

            glider.Model = new ReducedGravity(0.15f);
            world.LaunchAll();
            RunUntilFinished(world, glider);

            // Up to the top of the jump nothing changes; after it, the tanuki falls well short.
            Assert.That(glider.GlideStart.Value, Is.EqualTo(spyroTop));
            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Landed));
            Assert.That(glider.End.Value.Z, Is.LessThan(spyro / 2f));
        }

        [Test]
        public void The_gravity_it_feels_is_the_worlds_in_the_jump_and_the_methods_while_gliding()
        {
            var world = WorldWith(Ledge(), new ReducedGravity(0.15f), out var glider);
            world.LaunchAll();

            while (glider.Phase != FlightPhase.Jumping) world.StepOnce();
            world.StepOnce();
            Assert.That(glider.Acceleration.Y, Is.EqualTo(-20f).Within(1e-3f));

            while (glider.Phase != FlightPhase.Gliding) world.StepOnce();
            world.StepOnce();
            Assert.That(glider.Acceleration.Y, Is.EqualTo(-3f).Within(1e-3f));   // 0.15 · 20
        }

        [Test]
        public void Gliding_at_a_fixed_speed_feels_no_gravity_at_all()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();

            while (glider.Phase != FlightPhase.Gliding) world.StepOnce();
            for (int i = 0; i < 120; i++) world.StepOnce();

            Assert.That(glider.Acceleration.Length(), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Without_stamina_it_glides_until_it_lands()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(glider.Trajectory.Points, Has.None.Matches<TrajectoryPoint>(p => p.Phase == FlightPhase.Falling));
        }

        [Test]
        public void Out_of_stamina_it_lets_go_and_falls_with_plain_gravity()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            glider.Stamina = new Stamina(2f);   // two seconds of glide
            world.LaunchAll();

            while (glider.Phase != FlightPhase.Gliding) world.StepOnce();
            while (glider.Phase == FlightPhase.Gliding) world.StepOnce();

            Assert.That(glider.Phase, Is.EqualTo(FlightPhase.Falling));
            Assert.That(glider.GlideTime, Is.EqualTo(2f).Within(0.02f));
            world.StepOnce();
            Assert.That(glider.Acceleration.Y, Is.EqualTo(-20f).Within(1e-3f));
            Assert.That(glider.State.HorizontalSpeed, Is.EqualTo(10f).Within(1e-4f));   // keeps its speed
        }

        [Test]
        public void Tiring_shortens_the_same_jump_and_the_next_jump_starts_rested()
        {
            var world = WorldWith(Ledge(), new FixedDescent(10f, 2.5f), out var glider);
            world.LaunchAll();
            RunUntilFinished(world, glider);
            float rested = glider.End.Value.Z;

            glider.Stamina = new Stamina(3f);
            world.LaunchAll();
            RunUntilFinished(world, glider);
            float tired = glider.End.Value.Z;

            world.LaunchAll();
            RunUntilFinished(world, glider);

            Assert.That(tired, Is.LessThan(rested / 2f));
            Assert.That(glider.End.Value.Z, Is.EqualTo(tired));   // refilled on reset: the same jump again
        }
    }
}
