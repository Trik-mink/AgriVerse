export const PLAYER_PRESETS = [
  { id: 'channel', label: 'Channel', color: '#17675a' },
  { id: 'clay', label: 'Clay', color: '#b75d45' },
  { id: 'sun', label: 'Sun', color: '#d69c34' },
  { id: 'leaf', label: 'Leaf', color: '#5c8550' },
] as const;

export type PlayerPresetId = (typeof PLAYER_PRESETS)[number]['id'];

export type PlayerProfile = {
  displayName: string;
  presetId: PlayerPresetId;
};

export const DEFAULT_PLAYER: PlayerProfile = { displayName: 'Student', presetId: 'channel' };
