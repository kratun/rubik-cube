import { CUBES_ROUTE_PREFIX, ROOT_PATH, cubeIdFromPath, cubePagePath } from "./sessionUrl";

const CUBE_ID = "0c635560-7b8e-4721-88bb-76d310403744";
const ENCODED_ID = "a b/c";
const ENCODED_PATH = "/cubes/a%20b%2Fc";
const OTHER_PATH = "/about";
const BROKEN_ENCODING_ID = "%E0%A4%A";
const BROKEN_ENCODING_PATH = `${CUBES_ROUTE_PREFIX}${BROKEN_ENCODING_ID}`;

describe("cubeIdFromPath", () => {
  it("returns the id of a cube path", () => {
    expect(cubeIdFromPath(`${CUBES_ROUTE_PREFIX}${CUBE_ID}`)).toBe(CUBE_ID);
  });

  it("decodes an encoded id", () => {
    expect(cubeIdFromPath(ENCODED_PATH)).toBe(ENCODED_ID);
  });

  it("returns an id with broken percent-encoding as it is", () => {
    expect(cubeIdFromPath(BROKEN_ENCODING_PATH)).toBe(BROKEN_ENCODING_ID);
  });

  it.each([ROOT_PATH, OTHER_PATH, CUBES_ROUTE_PREFIX])("returns null for %s", (path) => {
    expect(cubeIdFromPath(path)).toBeNull();
  });
});

describe("cubePagePath", () => {
  it("builds the path of a cube", () => {
    expect(cubePagePath(CUBE_ID)).toBe(`${CUBES_ROUTE_PREFIX}${CUBE_ID}`);
  });

  it("encodes the id", () => {
    expect(cubePagePath(ENCODED_ID)).toBe(ENCODED_PATH);
  });
});
