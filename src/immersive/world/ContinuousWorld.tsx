import { useLayoutEffect, useMemo, useRef, type ReactNode } from 'react';
import { Color, InstancedMesh, Object3D } from 'three';

import { STATIONS, type StationId } from './stations';

function StationMarker({ position, index }: { position: [number, number, number]; index: number }) {
  const colors = ['#e6a44f', '#5da5a9', '#5b769f', '#d27752', '#8eaa5b', '#8b6f9c'];
  return <group position={position}><mesh position={[0, 0.55, 0]}><cylinderGeometry args={[0.62, 0.82, 1.1, 8]} /><meshStandardMaterial color={colors[index]} roughness={0.9} /></mesh><mesh position={[0, 1.25, 0]} rotation={[0, Math.PI / 8, 0]}><boxGeometry args={[1.8, 0.5, 1.1]} /><meshStandardMaterial color="#f6eed8" roughness={0.95} /></mesh></group>;
}

function InstancedPlants({ count, color, offset }: { count: number; color: string; offset: [number, number] }) {
  const mesh = useRef<InstancedMesh>(null);
  const points = useMemo(() => Array.from({ length: count }, (_, index) => {
    const column = index % 15;
    const row = Math.floor(index / 15);
    return [offset[0] + column * 0.48 + (row % 2) * 0.12, offset[1] + row * 0.7, 0.35 + (index % 3) * 0.08] as const;
  }), [count, offset]);

  useLayoutEffect(() => {
    if (!mesh.current) return;
    const transform = new Object3D();
    const tint = new Color(color);
    points.forEach(([x, z, height], index) => {
      transform.position.set(x, height / 2, z);
      transform.rotation.set(0, (index % 5) * 0.35, 0);
      transform.scale.set(1, height, 1);
      transform.updateMatrix();
      mesh.current!.setMatrixAt(index, transform.matrix);
      mesh.current!.setColorAt(index, tint);
    });
    mesh.current.instanceMatrix.needsUpdate = true;
    if (mesh.current.instanceColor) mesh.current.instanceColor.needsUpdate = true;
  }, [color, points]);

  return <instancedMesh ref={mesh} args={[undefined, undefined, count]}><coneGeometry args={[0.1, 1, 4]} /><meshStandardMaterial vertexColors roughness={0.95} /></instancedMesh>;
}

function Palm({ position, lean = 0 }: { position: [number, number, number]; lean?: number }) {
  return <group position={position} rotation={[0, lean, lean * 0.08]}><mesh position={[0, 1.25, 0]}><cylinderGeometry args={[0.13, 0.24, 2.5, 6]} /><meshStandardMaterial color="#775238" roughness={1} /></mesh>{[0, 1.25, 2.5, 3.75, 5].map((rotation) => <mesh key={rotation} position={[0, 2.5, 0]} rotation={[0.2, rotation, 0.8]}><coneGeometry args={[0.2, 1.55, 4]} /><meshStandardMaterial color="#316f50" roughness={0.95} /></mesh>)}</group>;
}

function StiltHouse({ position }: { position: [number, number, number] }) {
  return <group position={position}><mesh position={[0, 1.1, 0]}><boxGeometry args={[2.8, 1.35, 1.9]} /><meshStandardMaterial color="#f0d9ac" roughness={0.95} /></mesh><mesh position={[0, 2.2, 0]} rotation={[0, Math.PI / 4, 0]}><coneGeometry args={[2.15, 1.2, 4]} /><meshStandardMaterial color="#b4543f" roughness={0.9} /></mesh>{[-1, 1].flatMap((x) => [-0.62, 0.62].map((z) => <mesh key={`${x}-${z}`} position={[x, 0.4, z]}><boxGeometry args={[0.16, 0.8, 0.16]} /><meshStandardMaterial color="#674d38" roughness={1} /></mesh>))}</group>;
}

function Sampan({ position }: { position: [number, number, number] }) {
  return <group position={position} rotation={[0, -0.3, 0.04]}><mesh position={[0, 0.24, 0]} scale={[1.55, 0.35, 0.52]}><sphereGeometry args={[1, 12, 6]} /><meshStandardMaterial color="#704b34" roughness={0.9} /></mesh><mesh position={[0.1, 0.62, 0]} rotation={[0, 0, Math.PI / 2]}><cylinderGeometry args={[0.035, 0.035, 2.6, 6]} /><meshStandardMaterial color="#d7b77a" roughness={1} /></mesh></group>;
}

function ActiveStationFeature({ stationId }: { stationId: StationId }) {
  const station = STATIONS.find((candidate) => candidate.id === stationId)!;
  return <group key={station.id} position={station.position}><mesh position={[0, 0.06, 0]} rotation={[-Math.PI / 2, 0, 0]}><circleGeometry args={[3.5, 16]} /><meshBasicMaterial color="#315e58" transparent opacity={0.2} /></mesh><StiltHouse position={[0, 0, 0]} /><Palm position={[-2.4, 0, 0.8]} lean={0.3} /><Palm position={[2.3, 0, -0.7]} lean={-0.25} />{stationId === 'field' || stationId === 'planning' ? <Sampan position={[3.2, 0, 2.8]} /> : null}</group>;
}

export function ContinuousWorld({ stationId, children }: { stationId: StationId; children?: ReactNode }) {
  return <><color attach="background" args={['#b9d8d6']} /><fog attach="fog" args={['#b9d8d6', 28, 68]} /><directionalLight position={[18, 24, 12]} intensity={3.1} color="#ffd98a" /><mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.1, 0]}><planeGeometry args={[54, 46]} /><meshStandardMaterial color="#77985d" roughness={1} /></mesh><mesh rotation={[-Math.PI / 2, 0, 0]} position={[2.5, 0.01, 4]}><planeGeometry args={[6.8, 43]} /><meshStandardMaterial color="#2d8290" roughness={0.42} /></mesh><mesh rotation={[-Math.PI / 2, 0, 0]} position={[2.5, 0.035, 4]}><planeGeometry args={[1.2, 42]} /><meshStandardMaterial color="#6fb8ad" transparent opacity={0.42} roughness={0.25} /></mesh>{[-11, -6, -1, 4, 9, 14].map((z) => <mesh key={z} position={[-8.8, 0.02, z]} rotation={[0, 0.25, 0]}><boxGeometry args={[8.2, 0.26, 2.6]} /><meshStandardMaterial color="#94aa54" roughness={1} /></mesh>)}{[-9, -3, 3, 9, 15].map((z) => <mesh key={z} position={[13.6, 0.02, z]} rotation={[0, -0.35, 0]}><boxGeometry args={[6.2, 0.26, 2.7]} /><meshStandardMaterial color="#94aa54" roughness={1} /></mesh>)}<InstancedPlants count={105} color="#c7b854" offset={[-12, -11]} /><InstancedPlants count={75} color="#a7c064" offset={[9, 5]} />{STATIONS.map((station, index) => <StationMarker key={station.id} position={station.position} index={index} />)}<ActiveStationFeature stationId={stationId} />{children}</>;
}
