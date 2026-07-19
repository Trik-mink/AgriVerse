using System;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class Episode3DSiteAnchor
    {
        [SerializeField] private string siteId = string.Empty;
        [SerializeField] private Vector3 hotspotPosition;
        [SerializeField] private Vector3 approachPosition;
        [SerializeField] private float approachHeading;

        public string SiteId => siteId;
        public Vector3 HotspotPosition => hotspotPosition;
        public Vector3 ApproachPosition => approachPosition;
        public float ApproachHeading => approachHeading;

        public Episode3DSiteAnchor(
            string configuredSiteId,
            Vector3 configuredHotspotPosition,
            Vector3 configuredApproachPosition,
            float configuredApproachHeading)
        {
            siteId = configuredSiteId ?? string.Empty;
            hotspotPosition = configuredHotspotPosition;
            approachPosition = configuredApproachPosition;
            approachHeading = configuredApproachHeading;
        }
    }

    [Serializable]
    public sealed class Episode3DStakeholderAnchor
    {
        [SerializeField] private string stakeholderId = string.Empty;
        [SerializeField] private Vector3 position;

        public string StakeholderId => stakeholderId;
        public Vector3 Position => position;

        public Episode3DStakeholderAnchor(
            string configuredStakeholderId,
            Vector3 configuredPosition)
        {
            stakeholderId =
                configuredStakeholderId ?? string.Empty;
            position = configuredPosition;
        }
    }

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
        [SerializeField] private Vector3 maiPosition;
        [SerializeField] private Episode3DSiteAnchor[] siteAnchors =
            Array.Empty<Episode3DSiteAnchor>();
        [SerializeField]
        private Episode3DStakeholderAnchor[] stakeholderAnchors =
            Array.Empty<Episode3DStakeholderAnchor>();

        public string ScenarioId => scenarioId;
        public Vector3 MaiPosition => maiPosition;
        public Episode3DSiteAnchor[] SiteAnchors =>
            siteAnchors ?? Array.Empty<Episode3DSiteAnchor>();
        public Episode3DStakeholderAnchor[] StakeholderAnchors =>
            stakeholderAnchors ??
            Array.Empty<Episode3DStakeholderAnchor>();

        public void Configure(
            string configuredScenarioId,
            Vector3 configuredMaiPosition,
            Episode3DSiteAnchor[] configuredSiteAnchors,
            Episode3DStakeholderAnchor[]
                configuredStakeholderAnchors)
        {
            scenarioId = configuredScenarioId ?? string.Empty;
            maiPosition = configuredMaiPosition;
            siteAnchors =
                configuredSiteAnchors ??
                Array.Empty<Episode3DSiteAnchor>();
            stakeholderAnchors =
                configuredStakeholderAnchors ??
                Array.Empty<Episode3DStakeholderAnchor>();
        }
    }
}
