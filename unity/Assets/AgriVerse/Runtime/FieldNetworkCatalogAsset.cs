using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class FieldNetworkPreviewEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string country = string.Empty;
        [SerializeField] private string region = string.Empty;
        [SerializeField] private string episodeTitle = string.Empty;
        [SerializeField] private string teaser = string.Empty;
        [SerializeField] private float latitude;
        [SerializeField] private float longitude;
        [SerializeField] private FieldNetworkStatus status =
            FieldNetworkStatus.Incoming;

        public string Id => id;
        public string Country => country;
        public string Region => region;
        public string EpisodeTitle => episodeTitle;
        public string Teaser => teaser;
        public float Latitude => latitude;
        public float Longitude => longitude;
        public FieldNetworkStatus Status => status;
    }

    [CreateAssetMenu(
        fileName = "FieldNetworkCatalog",
        menuName = "AgriVerse/Field Network Catalog")]
    public sealed class FieldNetworkCatalogAsset : ScriptableObject
    {
        [SerializeField] private float activeLatitude = 10.03f;
        [SerializeField] private float activeLongitude = 105.78f;
        [SerializeField] private FieldNetworkPreviewEntry[] futureLocations =
            Array.Empty<FieldNetworkPreviewEntry>();

        public float ActiveLatitude => activeLatitude;
        public float ActiveLongitude => activeLongitude;
        public IReadOnlyList<FieldNetworkPreviewEntry> FutureLocations =>
            futureLocations;
    }
}
