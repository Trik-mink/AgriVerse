export function canOpenProposal(repliedStakeholderIds: string[], stakeholderIds: string[]) {
  return stakeholderIds.every((id) => repliedStakeholderIds.includes(id));
}

export function canRunSimulation(input: {
  testedSiteIds: string[];
  siteIds: string[];
  repliedStakeholderIds: string[];
  stakeholderIds: string[];
  interventionIds: string[];
  rationale: string;
}) {
  return (
    input.siteIds.every((id) => input.testedSiteIds.includes(id)) &&
    canOpenProposal(input.repliedStakeholderIds, input.stakeholderIds) &&
    input.interventionIds.length > 0 &&
    input.rationale.trim().length > 0
  );
}
