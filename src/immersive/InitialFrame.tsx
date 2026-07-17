import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';

export function InitialFrame({ onReady }: { onReady: () => void }) {
  const { camera, gl, invalidate, scene } = useThree();
  const renderedFrames = useRef(0);
  const hasReportedReady = useRef(false);

  useEffect(() => {
    gl.compile(scene, camera);
    invalidate();
    const firstFrame = requestAnimationFrame(() => invalidate());
    let settledFrame: number | undefined;
    const settleRequest = requestAnimationFrame(() => {
      settledFrame = requestAnimationFrame(() => invalidate());
    });
    return () => {
      cancelAnimationFrame(firstFrame);
      cancelAnimationFrame(settleRequest);
      if (settledFrame !== undefined) {
        cancelAnimationFrame(settledFrame);
      }
    };
  }, [camera, gl, invalidate, scene]);

  useFrame(({ gl: renderer }) => {
    if (hasReportedReady.current || renderer.info.render.frame === 0) return;

    // This callback runs before the current draw, so each count represents a completed prior frame.
    renderedFrames.current += 1;
    if (renderedFrames.current < 2) return;

    hasReportedReady.current = true;
    onReady();
  });

  return null;
}
