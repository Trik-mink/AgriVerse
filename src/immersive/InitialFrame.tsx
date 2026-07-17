import { useEffect } from 'react';
import { useThree } from '@react-three/fiber';

export function InitialFrame() {
  const { camera, gl, invalidate, scene } = useThree();

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

  return null;
}
