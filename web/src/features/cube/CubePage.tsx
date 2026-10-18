import { useCallback, useState } from "react";
import { AlertBanner } from "./components/AlertBanner";
import { CubeView } from "./components/CubeView";
import { ExplodedView } from "./components/ExplodedView";
import { MoveHistory } from "./components/MoveHistory";
import { MovePad } from "./components/MovePad";
import { SessionControls } from "./components/SessionControls";
import { cubeIdFromPath } from "./utils/sessionUrl";
import { useCubeSession } from "./hooks/useCubeSession";
import { useMoveKeys } from "./hooks/useMoveKeys";
import "./CubePage.css";

export const LOADING_MESSAGE = "Loading the cube…";
export const START_NEW_CUBE_LABEL = "Start a new cube";

export function CubePage() {
  // The URL is read once: the hook itself moves it to the cube it shows.
  const [cubeId] = useState(() => cubeIdFromPath(window.location.pathname));
  const { state, busy, startNewCube, applyMove, undo, reset, applyTtcSequence } =
    useCubeSession(cubeId);
  useMoveKeys(applyMove);
  const [explodedViewOpen, setExplodedViewOpen] = useState(false);
  const openExplodedView = useCallback(() => {
    setExplodedViewOpen(true);
  }, []);
  const closeExplodedView = useCallback(() => {
    setExplodedViewOpen(false);
  }, []);

  switch (state.status) {
    case "loading":
      return <p role="status">{LOADING_MESSAGE}</p>;
    case "error":
      return (
        <>
          <AlertBanner message={state.message} />
          <button type="button" onClick={startNewCube}>
            {START_NEW_CUBE_LABEL}
          </button>
        </>
      );
    case "ready":
      return (
        <>
          {state.notice !== null && <AlertBanner message={state.notice} />}
          <div className="cube-page">
            <CubeView cube={state.cube} />
            <div className="cube-page-side">
              <SessionControls
                canUndo={state.cube.moves.length > 0}
                busy={busy}
                onUndo={undo}
                onReset={reset}
                onApplyTtc={applyTtcSequence}
                onShowExplodedView={openExplodedView}
              />
              <MovePad onMove={applyMove} busy={busy} />
              <MoveHistory moves={state.cube.moves} />
            </div>
          </div>
          <ExplodedView
            lines={state.cube.explodedView}
            open={explodedViewOpen}
            onClose={closeExplodedView}
          />
        </>
      );
  }
}
