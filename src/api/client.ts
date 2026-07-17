import type { GraderResult, PolicyBriefResult, Proposal, Scenario, SimulatorResult } from '../types';

type ApiErrorBody = { error?: { message?: string } };

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(options?.headers ?? {}) },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as ApiErrorBody;
    throw new Error(body.error?.message ?? 'The request could not be completed.');
  }

  return response.json() as Promise<T>;
}

export const api = {
  getScenario: () => request<Scenario>('/api/scenario'),
  askStakeholder: (stakeholderId: string, body: { message: string; conversation: Array<{ role: 'student' | 'stakeholder'; text: string }> }) =>
    request<{ message: string }>(`/api/stakeholders/${stakeholderId}/messages`, { method: 'POST', body: JSON.stringify(body) }),
  simulate: (body: { target_site_id: string; proposal: Proposal }) =>
    request<SimulatorResult>('/api/simulations', { method: 'POST', body: JSON.stringify(body) }),
  grade: (body: { target_site_id: string; proposal: Proposal; simulation: SimulatorResult }) =>
    request<GraderResult>('/api/feedback', { method: 'POST', body: JSON.stringify(body) }),
  createBrief: (body: { target_site_id: string; proposal: Proposal; simulation: SimulatorResult; stakeholder_concerns: Array<{ stakeholder_id: string; concern: string }> }) =>
    request<PolicyBriefResult>('/api/policy-briefs', { method: 'POST', body: JSON.stringify(body) }),
};
