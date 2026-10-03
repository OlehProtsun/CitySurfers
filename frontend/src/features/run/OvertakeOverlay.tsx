import { useEffect } from "react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { Zap } from "lucide-react";
import { useRun } from "./context";
export function OvertakeOverlay() {
  const { events, dismissEvent } = useRun();
  const event = events[0];
  const reduced = useReducedMotion();
  useEffect(() => {
    if (!event) return;
    const timer = setTimeout(dismissEvent, 1600);
    return () => clearTimeout(timer);
  }, [event, dismissEvent]);
  return (
    <AnimatePresence>
      {event && (
        <motion.div
          key={`${event.opponent}-${event.rankAfter}`}
          className="overtake"
          role="status"
          aria-live="polite"
          initial={{ opacity: 0, scale: reduced ? 1 : 0.92 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: reduced ? 0.1 : 0.18 }}
        >
          <Zap size={40} fill="currentColor" />
          <span className="eyebrow">ONE STEP CLOSER</span>
          <h2>OVERTAKE!</h2>
          <p>{event.opponent}</p>
          <motion.strong
            initial={{ opacity: 0, y: reduced ? 0 : 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.2 }}
          >
            #{event.rankBefore} → #{event.rankAfter}
          </motion.strong>
          <motion.b initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
            +{event.pointsAwarded} pts
          </motion.b>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
