import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import { Vector3 } from 'three';

import { STATIONS } from './world/stations';

const WAKE_DURATION_SECONDS = 3;

export function WakeUpDirector({ reducedMotion }: { reducedMotion: boolean }) {
  const { camera, clock } = useThree();
  const startedAt = useRef<number | undefined>(undefined);
  const completed = useRef(false);
  const startPosition = useRef(new Vector3(0, 1.25, 1.4));
  const targetPosition = useRef(new Vector3(...STATIONS[0].camera));
  const lookTarget = useRef(new Vector3(...STATIONS[0].lookAt));

  useEffect(() => {
    if (reducedMotion) {
      camera.position.copy(targetPosition.current);
      camera.lookAt(lookTarget.current);
      completed.current = true;
      return;
    }

    camera.position.copy(startPosition.current);
    camera.lookAt(lookTarget.current);
    startedAt.current = clock.getElapsedTime();
  }, [camera, clock, reducedMotion]);

  useFrame(({ clock: frameClock }) => {
    if (reducedMotion || completed.current || startedAt.current === undefined) return;

    const rawProgress = Math.min(1, (frameClock.getElapsedTime() - startedAt.current) / WAKE_DURATION_SECONDS);
    const easedProgress = rawProgress * rawProgress * (3 - 2 * rawProgress);
    camera.position.lerpVectors(startPosition.current, targetPosition.current, easedProgress);
    camera.lookAt(lookTarget.current);

    if (rawProgress === 1) {
      completed.current = true;
    }
  });

  return null;
}
