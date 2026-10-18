import { useCallback, useEffect, useReducer } from "react";
import type { ApiResult } from "../../api/apiResult";
import { createCube, getCube } from "../../api/client";
import type { CubeState } from "../../api/types";
import { cubePagePath } from "./sessionUrl";

export const CUBE_NOT_FOUND_MESSAGE = "Cube not found";

const HTTP_NOT_FOUND = 404;
const HTTP_BAD_REQUEST = 400;
// A malformed id in the URL (400) is as unusable as an unknown one (404).
const UNKNOWN_CUBE_STATUSES: readonly number[] = [HTTP_NOT_FOUND, HTTP_BAD_REQUEST];

export type SessionState =
  | { status: "loading" }
  | { status: "ready"; cube: CubeState }
  | { status: "error"; message: string };

type SessionAction =
  { type: "loading" } | { type: "loaded"; cube: CubeState } | { type: "failed"; message: string };

const INITIAL_STATE: SessionState = { status: "loading" };

function sessionReducer(_state: SessionState, action: SessionAction): SessionState {
  switch (action.type) {
    case "loading":
      return INITIAL_STATE;
    case "loaded":
      return { status: "ready", cube: action.cube };
    case "failed":
      return { status: "error", message: action.message };
  }
}

function actionFor(result: ApiResult<CubeState>, isLoad: boolean): SessionAction {
  if (result.ok) {
    return { type: "loaded", cube: result.data };
  }
  const unknownCube = isLoad && UNKNOWN_CUBE_STATUSES.includes(result.status);
  return { type: "failed", message: unknownCube ? CUBE_NOT_FOUND_MESSAGE : result.error.detail };
}

export interface CubeSession {
  state: SessionState;
  startNewCube: () => void;
}

/**
 * The session of the cube in the URL (AD-9): loads `cubeId`, or creates a cube when there is none,
 * and keeps the URL on the cube it shows. The server is the source of truth.
 */
export function useCubeSession(cubeId: string | null): CubeSession {
  const [state, dispatch] = useReducer(sessionReducer, INITIAL_STATE);

  useEffect(() => {
    let cancelled = false;
    const request = cubeId === null ? createCube() : getCube(cubeId);
    void request.then((result) => {
      if (!cancelled) {
        dispatch(actionFor(result, cubeId !== null));
      }
    });
    return () => {
      cancelled = true;
    };
  }, [cubeId]);

  useEffect(() => {
    if (state.status === "ready") {
      window.history.replaceState(null, "", cubePagePath(state.cube.cubeId));
    }
  }, [state]);

  const startNewCube = useCallback(() => {
    dispatch({ type: "loading" });
    void createCube().then((result) => {
      dispatch(actionFor(result, false));
    });
  }, []);

  return { state, startNewCube };
}
