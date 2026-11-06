using Gliding.Domain.Flight;
using NUnit.Framework;

namespace Gliding.Tests
{
    public sealed class StaminaTests
    {
        [Test]
        public void It_starts_full()
        {
            var stamina = new Stamina(7f);

            Assert.That(stamina.Current, Is.EqualTo(7f));
            Assert.That(stamina.Fraction, Is.EqualTo(1f));
            Assert.That(stamina.IsEmpty, Is.False);
        }

        [Test]
        public void Spending_lowers_it_and_never_below_zero()
        {
            var stamina = new Stamina(7f);

            stamina.Spend(3.5f);
            Assert.That(stamina.Fraction, Is.EqualTo(0.5f).Within(1e-6f));

            stamina.Spend(10f);
            Assert.That(stamina.Current, Is.EqualTo(0f));
            Assert.That(stamina.IsEmpty, Is.True);
        }

        [Test]
        public void Refill_brings_it_back_to_full()
        {
            var stamina = new Stamina(7f);
            stamina.Spend(7f);

            stamina.Refill();

            Assert.That(stamina.Fraction, Is.EqualTo(1f));
        }
    }
}
