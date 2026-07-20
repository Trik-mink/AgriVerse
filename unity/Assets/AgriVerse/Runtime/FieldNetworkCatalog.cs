using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AgriVerse.Client
{
    public enum FieldNetworkStatus
    {
        Available,
        Incoming
    }

    public enum FieldNetworkConnectionState
    {
        Loading,
        Offline,
        Ready
    }

    [Serializable]
    public sealed class FieldNetworkLocation
    {
        public FieldNetworkLocation(
            string id,
            string country,
            string region,
            string episode,
            string teaser,
            FieldNetworkStatus status,
            double latitude,
            double longitude)
        {
            Id = id ?? string.Empty;
            Country = country ?? string.Empty;
            Region = region ?? string.Empty;
            Episode = episode ?? string.Empty;
            Teaser = teaser ?? string.Empty;
            Status = status;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Id { get; }
        public string Country { get; }
        public string Region { get; }
        public string Episode { get; }
        public string Teaser { get; }
        public FieldNetworkStatus Status { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public bool IsPlayable =>
            Status == FieldNetworkStatus.Available;
    }

    /// <summary>
    /// One presentation catalog for the global field network. The playable entry is
    /// derived from the loaded scenario; incoming locations are presentation-only and
    /// never invoke scenario or backend services.
    /// </summary>
    public sealed class FieldNetworkCatalog
    {
        private readonly FieldNetworkLocation[] locations;

        private FieldNetworkCatalog(
            FieldNetworkLocation activeLocation,
            IEnumerable<FieldNetworkLocation> futureLocations)
        {
            ActiveLocation = activeLocation;
            locations = new[] { activeLocation }
                .Concat(
                    futureLocations ??
                    Enumerable.Empty<FieldNetworkLocation>())
                .ToArray();
        }

        public IReadOnlyList<FieldNetworkLocation> Locations =>
            locations;
        public FieldNetworkLocation ActiveLocation { get; }

        public static FieldNetworkCatalog CreateForScenario(
            ScenarioDto scenario,
            string episode,
            string teaser,
            FieldNetworkCatalogAsset presentation = null)
        {
            if (scenario == null ||
                string.IsNullOrWhiteSpace(scenario.id))
            {
                throw new ArgumentException(
                    "A loaded scenario is required.",
                    nameof(scenario));
            }

            presentation ??=
                Resources.Load<FieldNetworkCatalogAsset>(
                    "FieldNetworkCatalog");
            IEnumerable<FieldNetworkLocation> futureLocations =
                presentation?.FutureLocations?
                    .Where(entry => entry != null)
                    .Select(entry =>
                        new FieldNetworkLocation(
                            entry.Id,
                            entry.Country,
                            entry.Region,
                            entry.EpisodeTitle,
                            entry.Teaser,
                            FieldNetworkStatus.Incoming,
                            entry.Latitude,
                            entry.Longitude)) ??
                Enumerable.Empty<FieldNetworkLocation>();

            return new FieldNetworkCatalog(
                new FieldNetworkLocation(
                    scenario.id,
                    scenario.location?.country,
                    scenario.location?.region,
                    episode,
                    teaser,
                    FieldNetworkStatus.Available,
                    presentation?.ActiveLatitude ?? 0.0,
                    presentation?.ActiveLongitude ?? 0.0),
                futureLocations);
        }

        public FieldNetworkLocation Find(string id) =>
            locations.FirstOrDefault(
                location =>
                    string.Equals(
                        location.Id,
                        id,
                        StringComparison.Ordinal));
    }

    public sealed class FieldNetworkLandingState
    {
        private readonly FieldNetworkCatalog catalog;

        public FieldNetworkLandingState(
            FieldNetworkCatalog sourceCatalog,
            FieldNetworkConnectionState connectionState =
                FieldNetworkConnectionState.Ready)
        {
            catalog = sourceCatalog ??
                throw new ArgumentNullException(nameof(sourceCatalog));
            ConnectionState = connectionState;
        }

        public FieldNetworkLocation SelectedLocation { get; private set; }
        public FieldNetworkConnectionState ConnectionState { get; private set; }
        public bool MissionServiceReady =>
            ConnectionState == FieldNetworkConnectionState.Ready;

        public bool Select(string id)
        {
            FieldNetworkLocation location = catalog.Find(id);
            if (location == null) return false;
            SelectedLocation = location;
            return true;
        }

        public void ClearSelection()
        {
            SelectedLocation = null;
        }

        public void SetConnectionState(
            FieldNetworkConnectionState value)
        {
            ConnectionState = value;
        }

        public bool CanBeginMission(string playerName) =>
            MissionServiceReady &&
            SelectedLocation != null &&
            SelectedLocation.IsPlayable &&
            !string.IsNullOrWhiteSpace(playerName) &&
            playerName.Trim().Length <= 40;
    }
}
