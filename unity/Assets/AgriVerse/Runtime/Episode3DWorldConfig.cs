using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Scenario-specific spatial presentation data. Runtime systems consume this
    /// configuration without branching on country, scenario, or site IDs.
    /// </summary>
    [CreateAssetMenu(
        menuName = "AgriVerse/Episode 3D World Config",
        fileName = "Episode3DWorldConfig")]
    public sealed class Episode3DWorldConfig : ScriptableObject
    {
        [SerializeField] private string scenarioId = string.Empty;
        [SerializeField] private string arrivalSiteId = string.Empty;
        [SerializeField] private Vector3 maiPosition;
        [SerializeField] private Vector3 hotspotPosition;
        [SerializeField] private Vector3 approachPosition;
        [SerializeField] private float approachHeading;

        public string ScenarioId => scenarioId;
        public string ArrivalSiteId => arrivalSiteId;
        public Vector3 MaiPosition => maiPosition;
        public Vector3 HotspotPosition => hotspotPosition;
        public Vector3 ApproachPosition => approachPosition;
        public float ApproachHeading => approachHeading;

        public void Configure(
            string configuredScenarioId,
            string configuredArrivalSiteId,
            Vector3 configuredMaiPosition,
            Vector3 configuredHotspotPosition,
            Vector3 configuredApproachPosition,
            float configuredApproachHeading)
        {
            scenarioId = configuredScenarioId ?? string.Empty;
            arrivalSiteId =
                configuredArrivalSiteId ?? string.Empty;
            maiPosition = configuredMaiPosition;
            hotspotPosition = configuredHotspotPosition;
            approachPosition = configuredApproachPosition;
            approachHeading = configuredApproachHeading;
        }
    }
}
