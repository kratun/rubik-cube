import { useState } from "react";
import { CubeView } from "./CubeView";
import { cubeIdFromPath } from "./sessionUrl";
import { useCubeSession } from "./useCubeSession";

export const LOADING_MESSAGE = "Loading the cube…";
export const START_NEW_CUBE_LABEL = "Start a new cube";

export function CubePage() {
  // The URL is read once: the hook itself moves it to the cube it shows.
  const [cubeId] = useState(() => cubeIdFromPath(window.location.pathname));
  const { state, startNewCube } = useCubeSession(cubeId);

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
      return <CubeView cube={state.cube} />;
  }
}
