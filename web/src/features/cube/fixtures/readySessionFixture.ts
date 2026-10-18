import { renderHook, waitFor } from "@testing-library/react";
import { useCubeSession } from "../hooks/useCubeSession";
import { SOLVED_CUBE_ID } from "./solvedCubeFixture";

// Renders the session hook for the solved cube's id and waits until the first response is shown.
// The caller mocks fetch before calling it.
export async function renderReadySession() {
  const view = renderHook(() => useCubeSession(SOLVED_CUBE_ID));
  await waitFor(() => {
    expect(view.result.current.state.status).toBe("ready");
  });
  return view;
}
