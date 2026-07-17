import { useState } from 'react';

import type { Scenario } from '../../types';
import { DEFAULT_PLAYER, PLAYER_PRESETS, type PlayerProfile } from './player';

export function ImmersiveLanding({ scenario, onStartImmersive, onUseClassic }: { scenario: Scenario; onStartImmersive: (player: PlayerProfile) => void; onUseClassic: () => void }) {
  const [displayName, setDisplayName] = useState(DEFAULT_PLAYER.displayName);
  const [presetId, setPresetId] = useState<PlayerProfile['presetId']>(DEFAULT_PLAYER.presetId);

  return (
    <main className="immersive-landing">
      <div className="immersive-landing-art" aria-hidden="true"><span /><span /><span /></div>
      <section className="immersive-landing-copy" aria-labelledby="immersive-title">
        <p className="eyebrow">Field investigation</p>
        <h1 id="immersive-title">{scenario.title}</h1>
        <p>Trace one guided journey from field observation to an evidence-based policy brief.</p>
        <label className="immersive-name-field" htmlFor="player-name">Your name
          <input id="player-name" value={displayName} maxLength={48} onChange={(event) => setDisplayName(event.target.value)} />
        </label>
        <fieldset className="immersive-preset-picker"><legend>Journey marker</legend><div>
          {PLAYER_PRESETS.map((preset) => <label key={preset.id}><input type="radio" name="player-preset" value={preset.id} checked={presetId === preset.id} onChange={() => setPresetId(preset.id)} /><span style={{ backgroundColor: preset.color }} aria-hidden="true" /><b>{preset.label}</b></label>)}
        </div></fieldset>
        <div className="immersive-landing-actions">
          <button type="button" className="immersive-primary" onClick={() => onStartImmersive({ displayName: displayName.trim() || DEFAULT_PLAYER.displayName, presetId })}>Enter the field journey</button>
          <button type="button" className="immersive-secondary" onClick={onUseClassic}>Use classic view</button>
        </div>
      </section>
    </main>
  );
}
