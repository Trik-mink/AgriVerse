import { describe, expect, it } from 'vitest';

import { ApiError } from '../server/api-error.js';
import { simulate } from '../server/runtime.js';

describe('runtime input boundary', () => {
  it('returns a deliberate client error for an unknown scenario site before calling the model', async () => {
    await expect(
      simulate({
        target_site_id: 'unknown-site',
        proposal: {
          intervention_ids: ['salt_tolerant_rice'],
          parameters: {},
          support_measures: [],
          rationale: 'A test rationale.',
        },
      }),
    ).rejects.toMatchObject<ApiError>({ status: 404, code: 'UNKNOWN_TEST_SITE' });
  });
});
