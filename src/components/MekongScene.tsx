import { Canvas } from '@react-three/fiber';
import { Html, OrbitControls } from '@react-three/drei';

import type { TestSite } from '../types';

type MekongSceneProps = {
  sites: TestSite[];
  selectedSiteId?: string;
  onSelectSite: (siteId: string) => void;
};

const positions: Array<[number, number, number]> = [
  [-2.3, 0.25, 0.5],
  [0, 0.25, 0],
  [2.1, 0.25, -0.55],
];

export function MekongScene({ sites, selectedSiteId, onSelectSite }: MekongSceneProps) {
  return (
    <div className="scene-frame" aria-label="Interactive agricultural landscape">
      <Canvas camera={{ position: [0, 5.4, 7.2], fov: 42 }}>
        <color attach="background" args={['#b8d9dd']} />
        <hemisphereLight intensity={1.4} groundColor="#315a47" />
        <directionalLight position={[4, 7, 2]} intensity={1.7} />
        <mesh rotation-x={-Math.PI / 2} position={[0, -0.05, 0]}>
          <planeGeometry args={[10, 7]} />
          <meshStandardMaterial color="#79ab66" roughness={0.95} />
        </mesh>
        <mesh rotation-x={-Math.PI / 2} position={[0, 0.01, 0]}>
          <planeGeometry args={[1.1, 7.4]} />
          <meshStandardMaterial color="#3c9cb5" roughness={0.25} metalness={0.15} />
        </mesh>
        <mesh rotation-x={-Math.PI / 2} position={[-2.8, 0.02, 0.9]} rotation-z={0.2}>
          <planeGeometry args={[1.25, 5.6]} />
          <meshStandardMaterial color="#8bbd65" roughness={1} />
        </mesh>
        <mesh rotation-x={-Math.PI / 2} position={[2.7, 0.02, -0.9]} rotation-z={-0.2}>
          <planeGeometry args={[1.25, 5.6]} />
          <meshStandardMaterial color="#a8b85e" roughness={1} />
        </mesh>
        {sites.map((site, index) => {
          const position = positions[index % positions.length];
          const isSelected = site.id === selectedSiteId;
          return (
            <group key={site.id} position={position}>
              <mesh onClick={() => onSelectSite(site.id)}>
                <cylinderGeometry args={[0.2, 0.28, 0.4, 24]} />
                <meshStandardMaterial color={isSelected ? '#e48a3b' : '#f1f0dd'} emissive={isSelected ? '#6f3210' : '#000000'} />
              </mesh>
              <Html center distanceFactor={8} position={[0, 0.55, 0]}>
                <button type="button" className="scene-label" onClick={() => onSelectSite(site.id)}>
                  {site.label}
                </button>
              </Html>
            </group>
          );
        })}
        <OrbitControls enablePan={false} minDistance={5.5} maxDistance={10} maxPolarAngle={1.35} minPolarAngle={0.75} />
      </Canvas>
    </div>
  );
}
