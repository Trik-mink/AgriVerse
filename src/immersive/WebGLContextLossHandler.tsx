import { useEffect } from 'react';
import { useThree } from '@react-three/fiber';

export function WebGLContextLossHandler({ onContextLost }: { onContextLost: () => void }) {
  const { gl } = useThree();

  useEffect(() => {
    const canvas = gl.domElement;
    const handleContextLost = (event: Event) => {
      event.preventDefault();
      gl.setAnimationLoop(null);
      onContextLost();
    };

    canvas.addEventListener('webglcontextlost', handleContextLost, false);
    return () => canvas.removeEventListener('webglcontextlost', handleContextLost, false);
  }, [gl, onContextLost]);

  return null;
}
