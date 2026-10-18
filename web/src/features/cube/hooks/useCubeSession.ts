import { useCallback, useEffect, useReducer, useRef, useState } from "react";
import type { ApiResult } from "../../../services/types/apiResult";
import { applyMoves, createCube, getCube, resetCube, undoMove } from "../../../services/client";
import type { CubeState } from "../../../services/types";
import { cubePagePath } from "../utils/sessionUrl";
import { TTC_TEST_SEQUENCE } from "../utils/ttcSequence";

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
  /** True from the start of a change until the server answered it (move, undo, reset, TTC sequence). */
  busy: boolean;
  startNewCube: () => void;
  applyMove: (token: string) => void;
  undo: () => void;
  reset: () => void;
  applyTtcSequence: () => void;
}

/**
 * What a change answered. `confirmed` is set when an earlier step of a two-request change already
 * succeeded, so a later failure still leaves a state the server confirmed to show.
 */
interface MutationOutcome {
  result: ApiResult<CubeState>;
  confirmed: CubeState | null;
}

type SendMutation = (cube: CubeState) => Promise<MutationOutcome>;

function singleRequest(result: ApiResult<CubeState>): MutationOutcome {
  return { result, confirmed: null };
}

// Reset, then the sequence on the revision the reset answered. The two requests are not atomic: if the
// second fails, the server is already at the Solved State, so that state is the one to show.
async function resetThenApplyTtc(cube: CubeState): Promise<MutationOutcome> {
  let current = cube;
  if (cube.moves.length > 0) {
    const reset = await resetCube(cube.cubeId, cube.revision);
    if (!reset.ok) {
      return singleRequest(reset);
    }
    current = reset.data;
  }
  const applied = await applyMoves(current.cubeId, current.revision, [...TTC_TEST_SEQUENCE]);
  return { result: applied, confirmed: current === cube ? null : current };
}

/**
 * The session of the cube in the URL (AD-9): loads `cubeId`, or creates a cube when there is none,
 * and keeps the URL on the cube it shows. The server is the source of truth.
 */
export function useCubeSession(cubeId: string | null): CubeSession {
  const [state, dispatch] = useReducer(sessionReducer, INITIAL_STATE);
  // A second move sent before the first answers would carry the same stale revision (a false 409).
  const moveInFlight = useRef(false);
  // The ref blocks the next click at once; this state only lets the page show that a change is running.
  const [busy, setBusy] = useState(false);

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
      const id = state.cube.cubeId;
      moveInFlight.current = true;
      setBusy(true);
      void send(state.cube).then(async ({ result, confirmed }) => {
        if (result.ok) {
          dispatch({ type: "loaded", cube: result.data, notice: null });
        } else if (result.status === HTTP_CONFLICT) {
          // The server is the source of truth: take its state and do not apply the action.
          const reloaded = await getCube(id);
          if (reloaded.ok) {
            dispatch({ type: "loaded", cube: reloaded.data, notice: CONFLICT_MESSAGE });
          } else if (confirmed !== null) {
            // The reset already worked, so the server is at the state it confirmed, not the old cube.
            dispatch({ type: "loaded", cube: confirmed, notice: reloaded.error.detail });
          } else {
            dispatch({ type: "moveFailed", message: reloaded.error.detail });
          }
        } else if (confirmed !== null) {
          dispatch({ type: "loaded", cube: confirmed, notice: result.error.detail });
        } else {
          dispatch({ type: "moveFailed", message: result.error.detail });
        }
        setBusy(false);
      });
    },
    [state],
  );

  const applyMove = useCallback(
    (token: string) => {
      mutate(async (cube) => singleRequest(await applyMoves(cube.cubeId, cube.revision, [token])));
    },
    [mutate],
  );
  const undo = useCallback(() => {
    mutate(async (cube) => singleRequest(await undoMove(cube.cubeId, cube.revision)));
  }, [mutate]);
  const reset = useCallback(() => {
    mutate(async (cube) => singleRequest(await resetCube(cube.cubeId, cube.revision)));
  }, [mutate]);
  const applyTtcSequence = useCallback(() => {
    mutate(resetThenApplyTtc);
  }, [mutate]);

  return { state, busy, startNewCube, applyMove, undo, reset, applyTtcSequence };
}
