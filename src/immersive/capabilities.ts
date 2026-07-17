export type PresentationMode = 'landing' | 'immersive' | 'classic';

export function supportsWebGL() {
  if (typeof document === 'undefined') return false;

  try {
    const canvas = document.createElement('canvas');
    return Boolean(canvas.getContext('webgl2') ?? canvas.getContext('webgl'));
  } catch {
    return false;
  }
}

export function shouldUseClassic(mode: Exclude<PresentationMode, 'landing'>, hasWebGL: boolean) {
  return mode === 'classic' || !hasWebGL;
}

export function prefersReducedMotion() {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}
