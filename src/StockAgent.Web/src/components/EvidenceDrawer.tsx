import type { EvidenceCard } from '../models';

/**
 * Displays selected evidence cards for the active research task.
 */
export function EvidenceDrawer({ evidenceCards }: { evidenceCards: EvidenceCard[] }) {
  return (
    <aside className="evidencePanel" aria-label="证据">
      <h2>证据</h2>
      {evidenceCards.length === 0 ? (
        <p className="muted">暂无证据</p>
      ) : (
        <ul>
          {evidenceCards.map((card) => (
            <li key={card.id}>
              <strong>{card.claim}</strong>
              <span>{card.snippet}</span>
            </li>
          ))}
        </ul>
      )}
    </aside>
  );
}
