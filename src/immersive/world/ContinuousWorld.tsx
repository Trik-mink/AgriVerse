import type { ReactNode } from 'react';

import { STATIONS } from './stations';

function StationMarker({ position, index }: { position: [number, number, number]; index: number }) {
  const colors = ['#e6a44f', '#5da5a9', '#5b769f', '#d27752', '#8eaa5b', '#8b6f9c'];

  return (
    <group position={position}>
      <mesh position={[0, 0.55, 0]}>
        <cylinderGeometry args={[0.7, 0.9, 1.1, 8]} />
        <meshStandardMaterial color={colors[index]} roughness={0.82} />
      </mesh>
      <mesh position={[0, 1.25, 0]} rotation={[0, Math.PI / 8, 0]}>
        <boxGeometry args={[2.2, 0.7, 1.35]} />
        <meshStandardMaterial color="#f6eed8" roughness={0.9} />
      </mesh>
    </group>
  );
}

export function ContinuousWorld({ children }: { children?: ReactNode }) {
  return (
    <>
      <color attach="background" args={['#a9cfda']} />
      <fog attach="fog" args={['#a9cfda', 30, 70]} />
      <hemisphereLight args={['#f9e8bf', '#2e5d56', 2.2]} />
      <directionalLight position={[18, 24, 12]} intensity={2.7} castShadow />
      <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <planeGeometry args={[52, 44]} />
        <meshStandardMaterial color="#6e9e6a" roughness={1} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[2.5, 0.02, 4]}>
        <planeGeometry args={[7, 43]} />
        <meshStandardMaterial color="#4695aa" roughness={0.28} metalness={0.08} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[-5.8, 0.03, 5]}>
        <planeGeometry args={[1.2, 38]} />
        <meshStandardMaterial color="#d7b77a" roughness={0.98} />
      </mesh>
      {STATIONS.map((station, index) => <StationMarker key={station.id} position={station.position} index={index} />)}
      {children}
    </>
  );
}
