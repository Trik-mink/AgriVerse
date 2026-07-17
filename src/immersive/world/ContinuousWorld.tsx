import type { ReactNode } from 'react';

import { STATIONS } from './stations';
import { EnvironmentAssets } from './EnvironmentAssets';

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
      <color attach="background" args={['#b6d7da']} />
      <fog attach="fog" args={['#b6d7da', 30, 70]} />
      <hemisphereLight args={['#fff0c9', '#245f57', 2.5]} />
      <directionalLight position={[18, 24, 12]} intensity={2.8} castShadow shadow-mapSize={[1024, 1024]} />
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.1, 0]} receiveShadow>
        <planeGeometry args={[54, 46]} />
        <meshStandardMaterial color="#799e62" roughness={1} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[2.5, 0.01, 4]} receiveShadow>
        <planeGeometry args={[6.8, 43]} />
        <meshStandardMaterial color="#2d8290" roughness={0.38} metalness={0.06} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[2.5, 0.035, 4]}>
        <planeGeometry args={[1.2, 42]} />
        <meshStandardMaterial color="#62afad" transparent opacity={0.46} roughness={0.2} />
      </mesh>
      {[-11, -6, -1, 4, 9, 14].map((z) => <mesh key={z} position={[-8.8, 0.02, z]} rotation={[0, 0.25, 0]} receiveShadow><boxGeometry args={[8.2, 0.26, 2.6]} /><meshStandardMaterial color="#89a854" roughness={0.95} /></mesh>)}
      {[-9, -3, 3, 9, 15].map((z) => <mesh key={z} position={[13.6, 0.02, z]} rotation={[0, -0.35, 0]} receiveShadow><boxGeometry args={[6.2, 0.26, 2.7]} /><meshStandardMaterial color="#89a854" roughness={0.95} /></mesh>)}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[-4.8, 0.04, 5]} receiveShadow>
        <planeGeometry args={[1.35, 39]} />
        <meshStandardMaterial color="#d5ae6c" roughness={0.98} />
      </mesh>
      <mesh position={[-0.4, 0.7, -8]} castShadow><boxGeometry args={[3.7, 1.25, 2.6]} /><meshStandardMaterial color="#f3e8ca" roughness={0.9} /></mesh>
      <mesh position={[-0.4, 1.7, -8]} rotation={[0, Math.PI / 4, 0]} castShadow><coneGeometry args={[2.9, 1.4, 4]} /><meshStandardMaterial color="#b75d45" roughness={0.88} /></mesh>
      <EnvironmentAssets />
      {STATIONS.map((station, index) => <StationMarker key={station.id} position={station.position} index={index} />)}
      {children}
    </>
  );
}
