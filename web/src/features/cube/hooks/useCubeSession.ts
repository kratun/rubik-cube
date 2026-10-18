import { useCallback, useEffect, useReducer, useRef } from "react";
import type { ApiResult } from "../../../services/types/apiResult";
import { applyMoves, createCube, getCube, resetCube, undoMove } from "../../../services/client";
import type { CubeState } from "../../../services/types";
import { cubePagePath } from "../utils/sessionUrl";

export const CUBE_NOT_FOUND_MESSAGE = "Cube not found";
export const CONFLICT_MESSAGE = "The cube was changed by someone else";

const HTTP_NOT_FOUND = 404;
const HTTP_BAD_REQUEST = 400;
const HTTP_CONFLICT = 409;
// A malformed id in the URL (400) is as unusable as an unknown one (404).
const UNKNOWN_CUBE_STATUSES: readonly number[] = [HTTP_NOT_FOUND, HTTP_BAD_REQUEST];

export type SessionState =
  | { status: "loading" }
  | { status: "ready"; cube: CubeState; notice: string | null }
  | { status: "error"; message: string };

type SessionAction =
  | { type: "loading" }
  | { type: "loaded"; cube: CubeState; notice: string | null }
  | { type: "failed"; message: string }
  | { type: "moveFailed"; message: string };

const INITIAL_STATE: SessionState = { status: "loading" };

function sessionReducer(state: SessionState, action: SessionAction): SessionState {
  switch (action.type) {
    case "loading":
      return INITIAL_STATE;
    case "loaded":
      return { status: "ready", cube: action.cube, notice: action.notice };
    case "failed":
      return { status: "error", message: action.message };
    case "moveFailed":
      // The last good cube stays visible; only a ready session can have a failed move.
      return state.status === "ready" ? { ...state, notice: action.message } : state;
  }
}

function actionFor(result: ApiResult<CubeState>, isLoad: boolean): SessionAction {
  if (result.ok) {
    return { type: "loaded", cube: result.data, notice: null };
  }
  const unknownCube = isLoad && UNKNOWN_CUBE_STATUSES.includes(result.status);
  return { type: "failed", message: unknownCube ? CUBE_NOT_FOUND_MESSAGE : result.error.detail };
}

export interface CubeSession {
  state: SessionState;
  startNewCube: () => void;
  applyMove: (token: string) => void;
  undo: () => void;
  reset: () => void;
}

type SendMutation = (cubeId: string, expectedRevision: string) => Promise<ApiResult<CubeState>>;

/**
 * The session of the cube in the URL (AD-9): loads `cubeId`, or creates a cube when there is none,
 * and keeps the URL on the cube it shows. The server is the source of truth.
 */
export function useCubeSession(cubeId: string | null): CubeSession {
  const [state, dispatch] = useReducer(sessionReducer, INITIAL_STATE);
  // A second move sent before the first answers would carry the same stale revision (a false 409).
  const moveInFlight = useRef(false);

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

  // The guard opens only after the new state has rendered, so the next move reads the new revision.
  useEffect(() => {
    moveInFlight.current = false;
  }, [state]);

  const startNewCube = useCallback(() => {
    dispatch({ type: "loading" });
    void createCube().then((result) => {
      dispatch(actionFor(result, false));
    });
  }, []);

  // The one path for every change of the cube (move, undo, reset): the same revision check,
  // in-flight guard, last-good-state notice and 409 reload (AD-9).
  const mutate = useCallback(
    (send: SendMutation) => {
      if (state.status !== "ready" || moveInFlight.current) {
        return;
      }
      const { cubeId: id, revision } = state.cube;
      moveInFlight.current = true;
      void send(id, revision).then(async (result) => {
        if (result.ok) {
          dispatch({ type: "loaded", cube: result.data, notice: null });
        } else if (result.status === HTTP_CONFLICT) {
          // The server is the source of truth: take its state and do not apply the action.
          const reloaded = await getCube(id);
          dispatch(
            reloaded.ok
              ? { type: "loaded", cube: reloaded.data, notice: CONFLICT_MESSAGE }
              : { type: "moveFailed", message: reloaded.error.detail },
          );
        } else {
          dispatch({ type: "moveFailed", message: result.error.detail });
        }
      });
    },
    [state],
  );

  const applyMove = useCallback(
    (token: string) => {
      mutate((id, revision) => applyMoves(id, revision, [token]));
    },
    [mutate],
  );
  const undo = useCallback(() => {
    mutate(undoMove);
  }, [mutate]);
  const reset = useCallback(() => {
    mutate(resetCube);
  }, [mutate]);

  return { state, startNewCube, applyMove, undo, reset };
}
