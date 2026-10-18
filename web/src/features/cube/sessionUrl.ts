export const ROOT_PATH = "/";
export const CUBES_ROUTE_PREFIX = "/cubes/";

const NO_ID = "";

// An id that cannot be decoded is passed on as it is: the API rejects it and the page says "not found".
function decodeId(encodedId: string): string {
  try {
    return decodeURIComponent(encodedId);
  } catch {
    return encodedId;
  }
}

/** The cubeId in a `/cubes/<cubeId>` path, or null for any other path (the app has two routes). */
export function cubeIdFromPath(pathname: string): string | null {
  if (!pathname.startsWith(CUBES_ROUTE_PREFIX)) {
    return null;
  }
  const cubeId = decodeId(pathname.slice(CUBES_ROUTE_PREFIX.length));
  return cubeId === NO_ID ? null : cubeId;
}

export function cubePagePath(cubeId: string): string {
  return `${CUBES_ROUTE_PREFIX}${encodeURIComponent(cubeId)}`;
}
