import { Component, type ErrorInfo, type ReactNode, Suspense, useMemo, useRef } from 'react';
import { useGLTF } from '@react-three/drei';
import { useFrame } from '@react-three/fiber';
import { Color, Group, Mesh, MeshStandardMaterial, Vector3 } from 'three';

import { CHARACTER_ASSET } from '../assets/manifest';

type CharacterProps = {
  position: [number, number, number];
  color: string;
  gesture?: 'idle' | 'lead' | 'react';
};

class CharacterErrorBoundary extends Component<{ position: [number, number, number]; children: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(_error: Error, _info: ErrorInfo) {
    // Character presentation may fail without affecting the investigation.
  }

  render() {
    if (!this.state.failed) return this.props.children;
    return <group position={this.props.position}><mesh position={[0, 0.9, 0]}><cylinderGeometry args={[0.25, 0.36, 1.35, 8]} /><meshStandardMaterial color="#f6eed8" /></mesh><mesh position={[0, 1.8, 0]}><sphereGeometry args={[0.28, 12, 10]} /><meshStandardMaterial color="#d9a47e" /></mesh></group>;
  }
}

function CharacterModel({ position, color, gesture = 'idle' }: CharacterProps) {
  const { scene } = useGLTF(CHARACTER_ASSET);
  const group = useRef<Group>(null);
  const destination = useMemo(() => new Vector3(...position), [position]);
  const model = useMemo(() => {
    const clone = scene.clone(true);
    const palette = new Color(color);
    clone.traverse((object) => {
      if (!(object instanceof Mesh)) return;
      object.castShadow = true;
      const materials = Array.isArray(object.material) ? object.material : [object.material];
      object.material = materials.map((material) => {
        if (!(material instanceof MeshStandardMaterial)) return material;
        const next = material.clone();
        next.color.multiply(palette);
        return next;
      });
    });
    return clone;
  }, [color, scene]);

  useFrame(({ clock }) => {
    if (!group.current) return;
    const phase = clock.getElapsedTime() * (gesture === 'lead' ? 1.8 : 1.2);
    group.current.position.lerp(destination, 0.05);
    group.current.position.y += Math.sin(phase) * 0.0018;
    group.current.rotation.y = Math.sin(phase * 0.5) * (gesture === 'react' ? 0.16 : 0.07);
    group.current.rotation.z = gesture === 'lead' ? Math.sin(phase) * 0.025 : 0;
  });

  return <group ref={group} position={position} scale={1.12}><primitive object={model} /></group>;
}

export function Character(props: CharacterProps) {
  return <CharacterErrorBoundary position={props.position}><Suspense fallback={null}><CharacterModel {...props} /></Suspense></CharacterErrorBoundary>;
}
