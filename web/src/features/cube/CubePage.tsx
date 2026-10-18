import { useState } from "react";
import { CubeView } from "./CubeView";
import { MovePad } from "./MovePad";
import { cubeIdFromPath } from "./sessionUrl";
import { useCubeSession } from "./useCubeSession";
import { useMoveKeys } from "./useMoveKeys";

export const LOADING_MESSAGE = "Loading the cube…";
export const START_NEW_CUBE_LABEL = "Start a new cube";

export function CubePage() {
  // The URL is read once: the hook itself moves it to the cube it shows.
  const [cubeId] = useState(() => cubeIdFromPath(window.location.pathname));
  const { state, startNewCube, applyMove } = useCubeSession(cubeId);
  useMoveKeys(applyMove);

  switch (state.status) {
    case "loading":
      return <p role="status">{LOADING_MESSAGE}</p>;
    case "error":
      return (
        <>
          <p role="alert">{state.message}</p>
          <button type="button" onClick={startNewCube}>
            {START_NEW_CUBE_LABEL}
          </button>
        </>
      );
    case "ready":
      return (
        <>
          {state.notice !== null && <p role="alert">{state.notice}</p>}
          <MovePad onMove={applyMove} />
          <CubeView cube={state.cube} />
        </>
      );
  }
}
