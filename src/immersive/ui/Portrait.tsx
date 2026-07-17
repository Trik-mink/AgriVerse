import { useEffect, useState } from 'react';

function initials(name: string) {
  return name.split(/\s+/).filter(Boolean).map((part) => part[0]).join('').slice(0, 2).toUpperCase();
}

export function Portrait({ src, name }: { src?: string; name: string }) {
  const [failed, setFailed] = useState(!src);

  useEffect(() => setFailed(!src), [src]);

  if (failed || !src) return <span className="immersive-portrait-badge" aria-label={`${name} portrait unavailable`}>{initials(name)}</span>;

  return <img className="immersive-portrait" src={src} alt={`${name} portrait`} onError={() => setFailed(true)} />;
}
