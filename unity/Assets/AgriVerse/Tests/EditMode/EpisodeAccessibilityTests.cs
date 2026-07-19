using NUnit.Framework;

namespace AgriVerse.Client.Tests
{
    public sealed class EpisodeAccessibilityTests
    {
        [SetUp]
        public void ResetSettings()
        {
            EpisodeAccessibility.ResetForTesting();
        }

        [TearDown]
        public void RestoreSettings()
        {
            EpisodeAccessibility.ResetForTesting();
        }

        [Test]
        public void TextContrastAndMotionPreferencesStayBounded()
        {
            EpisodeAccessibility.SetTextScale(4f);
            Assert.That(
                EpisodeAccessibility.TextScale,
                Is.EqualTo(1.4f));

            EpisodeAccessibility.SetTextScale(.1f);
            Assert.That(
                EpisodeAccessibility.TextScale,
                Is.EqualTo(.9f));

            EpisodeAccessibility.ToggleHighContrast();
            EpisodeAccessibility.ToggleReducedMotion();
            Assert.That(EpisodeAccessibility.HighContrast, Is.True);
            Assert.That(EpisodeAccessibility.ReducedMotion, Is.True);
        }
    }
}
