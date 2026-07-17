import { Component, type ErrorInfo, type ReactNode, Suspense, useMemo } from 'react';
import { useGLTF } from '@react-three/drei';

import { ENVIRONMENT_ASSETS } from '../assets/manifest';

type Transform = {
  position: [number, number, number];
  rotation?: [number, number, number];
  scale?: number | [number, number, number];
};

class AssetErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(_error: Error, _info: ErrorInfo) {
    // Decorative assets never prevent access to the station flow.
  }

  render() {
    return this.state.failed ? null : this.props.children;
  }
}

function EnvironmentAsset({ asset, position, rotation, scale }: Transform & { asset: keyof typeof ENVIRONMENT_ASSETS }) {
  const { scene } = useGLTF(ENVIRONMENT_ASSETS[asset]);
  const model = useMemo(() => scene.clone(true), [scene]);

  return <primitive object={model} position={position} rotation={rotation} scale={scale} />;
}

function DecorativeAsset(props: Transform & { asset: keyof typeof ENVIRONMENT_ASSETS }) {
  return <AssetErrorBoundary><Suspense fallback={null}><EnvironmentAsset {...props} /></Suspense></AssetErrorBoundary>;
}

const palms: Transform[] = [
  { position: [-14, 0, -10], rotation: [0, 0.8, 0], scale: 1.25 },
  { position: [15, 0, -11], rotation: [0, -1.8, 0], scale: 1.15 },
  { position: [17, 0, 9], rotation: [0, 2.6, 0], scale: 1.35 },
  { position: [-12, 0, 14], rotation: [0, -0.5, 0], scale: 1.05 },
];

const cropPlots: Transform[] = [
  { position: [-9, 0.12, -7], rotation: [0, 0.25, 0], scale: 1.2 },
  { position: [-9, 0.12, -3], rotation: [0, 0.25, 0], scale: 1.2 },
  { position: [-9, 0.12, 1], rotation: [0, 0.25, 0], scale: 1.2 },
  { position: [12, 0.12, 10], rotation: [0, -0.35, 0], scale: 1.15 },
  { position: [15, 0.12, 13], rotation: [0, -0.35, 0], scale: 1.15 },
];

export function EnvironmentAssets() {
  return (
    <group>
      {palms.map((transform, index) => <DecorativeAsset key={`palm-${index}`} asset={index % 2 ? 'palmBent' : 'palmTall'} {...transform} />)}
      {cropPlots.map((transform, index) => <DecorativeAsset key={`crop-${index}`} asset="crops" {...transform} />)}
      <DecorativeAsset asset="boat" position={[2.3, 0.16, 7]} rotation={[0, -0.3, 0.03]} scale={0.9} />
      <DecorativeAsset asset="bridge" position={[2.5, 0.08, -1.5]} rotation={[0, Math.PI / 2, 0]} scale={0.75} />
      <DecorativeAsset asset="bush" position={[-1.5, 0.12, 13]} rotation={[0, 0.4, 0]} scale={1.1} />
      <DecorativeAsset asset="bush" position={[7, 0.12, -10]} rotation={[0, -0.5, 0]} scale={0.9} />
    </group>
  );
}
