using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class FieldNetworkCatalogTests
    {
        private static readonly ScenarioDto Scenario = new ScenarioDto
        {
            id = "vietnam-mekong-salinity",
            title = "Saving a Mekong Delta farming community",
            location = new LocationDto
            {
                country = "Vietnam",
                region = "Mekong Delta"
            }
        };

        [Test]
        public void CatalogDerivesThePlayableEpisodeFromTheLoadedScenario()
        {
            FieldNetworkCatalog catalog =
                FieldNetworkCatalog.CreateForScenario(
                    Scenario,
                    "Episode 1: The Salt Line",
                    "Test the water. Hear the people. Revise the future.");

            Assert.That(catalog.Locations.Count(), Is.EqualTo(5));
            FieldNetworkLocation active = catalog.ActiveLocation;
            Assert.That(active.Id, Is.EqualTo(Scenario.id));
            Assert.That(active.Country, Is.EqualTo("Vietnam"));
            Assert.That(active.Region, Is.EqualTo("Mekong Delta"));
            Assert.That(
                active.Episode,
                Is.EqualTo("Episode 1: The Salt Line"));
            Assert.That(active.Status, Is.EqualTo(FieldNetworkStatus.Available));
            Assert.That(active.IsPlayable, Is.True);
        }

        [Test]
        public void CatalogKeepsAllFutureLocationsIncomingAndNonPlayable()
        {
            FieldNetworkCatalog catalog =
                FieldNetworkCatalog.CreateForScenario(
                    Scenario,
                    "Episode 1: The Salt Line",
                    "Test the water. Hear the people. Revise the future.");

            FieldNetworkLocation[] future = catalog.Locations
                .Where(location => !location.IsPlayable)
                .ToArray();
            Assert.That(
                future.Select(location => location.Country),
                Is.EquivalentTo(
                    new[]
                    {
                        "India",
                        "Kenya",
                        "Brazil",
                        "Netherlands"
                    }));
            Assert.That(
                future.All(
                    location =>
                        location.Status == FieldNetworkStatus.Incoming),
                Is.True);

            Assert.That(
                future.Single(location => location.Id == "india").Region,
                Is.EqualTo("Punjab"));
            Assert.That(
                future.Single(location => location.Id == "india").Episode,
                Is.EqualTo("The Falling Water Table"));
            Assert.That(
                future.Single(location => location.Id == "kenya").Episode,
                Is.EqualTo("When the Rains Fail"));
            Assert.That(
                future.Single(location => location.Id == "brazil").Region,
                Is.EqualTo("Cerrado"));
            Assert.That(
                future.Single(location => location.Id == "netherlands").Teaser,
                Does.Contain("workable for farmers"));
        }

        [Test]
        public void FutureLocationsComeFromTheSerializedPresentationCatalog()
        {
            FieldNetworkCatalogAsset asset =
                Resources.Load<FieldNetworkCatalogAsset>(
                    "FieldNetworkCatalog");

            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.FutureLocations.Count, Is.EqualTo(4));
            Assert.That(
                asset.FutureLocations.Select(entry => entry.Id),
                Is.EqualTo(
                    new[]
                    {
                        "india",
                        "kenya",
                        "brazil",
                        "netherlands"
                    }));
            Assert.That(
                asset.FutureLocations.All(
                    entry =>
                        !string.IsNullOrWhiteSpace(entry.Country) &&
                        !string.IsNullOrWhiteSpace(entry.Region) &&
                        !string.IsNullOrWhiteSpace(entry.EpisodeTitle) &&
                        !string.IsNullOrWhiteSpace(entry.Teaser) &&
                        entry.Status == FieldNetworkStatus.Incoming),
                Is.True);
        }

        [Test]
        public void SelectionOnlyUnlocksMissionStartForVietnamAndAValidName()
        {
            FieldNetworkLandingState state =
                new FieldNetworkLandingState(
                    FieldNetworkCatalog.CreateForScenario(
                        Scenario,
                        "Episode 1: The Salt Line",
                        "Test the water. Hear the people. Revise the future."));

            Assert.That(state.SelectedLocation, Is.Null);
            Assert.That(state.Select("india"), Is.True);
            Assert.That(state.SelectedLocation.Status, Is.EqualTo(FieldNetworkStatus.Incoming));
            Assert.That(state.CanBeginMission("Lan"), Is.False);

            Assert.That(state.Select(Scenario.id), Is.True);
            Assert.That(state.CanBeginMission(""), Is.False);
            Assert.That(state.CanBeginMission("Lan"), Is.True);

            state.ClearSelection();
            Assert.That(state.SelectedLocation, Is.Null);
            Assert.That(state.CanBeginMission("Lan"), Is.False);
        }

        [Test]
        public void GlobePinUsesACompactSurveyLabelWithALeaderLine()
        {
            GameObject root = new GameObject("AtlasPinTest");
            GameObject pinObject = new GameObject(
                "Pin",
                typeof(RectTransform));
            try
            {
                FieldNetworkPinView pin =
                    pinObject.AddComponent<FieldNetworkPinView>();
                pin.Build(
                    root.transform,
                    new FieldNetworkLocation(
                        "india",
                        "India",
                        "Punjab",
                        "The Falling Water Table",
                        "Protect the aquifer.",
                        FieldNetworkStatus.Incoming,
                        30.9,
                        75.8),
                    _ => { });

                Transform label = pin.transform.Find("PinLabel");
                Assert.That(
                    label.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
                Assert.That(
                    pin.transform.Find("PinLeaderLine")
                        ?.GetComponent<AtlasRouteGraphic>(),
                    Is.Not.Null);
                Assert.That(
                    (label as RectTransform).sizeDelta.x,
                    Is.LessThanOrEqualTo(190f));
                Assert.That(
                    label.GetComponentInChildren<Text>().text,
                    Does.Contain("INCOMING"));
            }
            finally
            {
                Object.DestroyImmediate(pinObject);
                Object.DestroyImmediate(root);
            }
        }
    }
}
