import 'dotenv/config';

import { createApp } from './app.js';
import { initializeModelAccess } from './model-access.js';

const port = Number(process.env.PORT ?? 8787);
const host = process.env.HOST?.trim() || '0.0.0.0';

try {
  const budget = await initializeModelAccess();
  createApp().listen(port, host, () => {
    console.info(
      JSON.stringify({
        event: 'server_started',
        port,
        version:
          process.env.DEPLOYED_COMMIT?.trim() ||
          process.env.RENDER_GIT_COMMIT?.trim() ||
          'local',
        budget_remaining_micro_usd: budget?.remainingMicroUsd,
      }),
    );
  });
} catch {
  console.error(JSON.stringify({ event: 'server_start_failed', reason: 'secure_runtime_configuration' }));
  process.exitCode = 1;
}
