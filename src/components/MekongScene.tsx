import type { TestSite } from '../types';

type MekongSceneProps = {
  sites: TestSite[];
  selectedSiteId?: string;
  onSelectSite: (siteId: string) => void;
};

const markerPositions = [
  { left: '20%', top: '58%' },
  { left: '51%', top: '42%' },
  { left: '76%', top: '64%' },
];

export function MekongScene({ sites, selectedSiteId, onSelectSite }: MekongSceneProps) {
  return (
    <div className="scene-frame classic-scene" aria-label="Interactive agricultural landscape">
      <div className="classic-channel" aria-hidden="true" />
      {sites.map((site, index) => {
        const isSelected = site.id === selectedSiteId;
        return (
          <button key={site.id} type="button" className={`scene-label classic-site-marker ${isSelected ? 'is-selected' : ''}`} style={markerPositions[index % markerPositions.length]} onClick={() => onSelectSite(site.id)}>
            {site.label}
          </button>
        );
      })}
    </div>
  );
}
