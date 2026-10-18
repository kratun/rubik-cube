import { createServer, type ViteDevServer } from "vite";
import { STATUS_OK } from "./features/cube/testing/apiResponseFixture";

const CUBE_PATH = "/cubes/0c635560-7b8e-4721-88bb-76d310403744";
const ROOT_ELEMENT = '<div id="root"></div>';
const ANY_FREE_PORT = 0;

let server: ViteDevServer;
let origin: string;

beforeAll(async () => {
  server = await createServer({ server: { port: ANY_FREE_PORT, strictPort: false } });
  await server.listen();
  origin = server.resolvedUrls?.local[0] ?? "";
});

afterAll(async () => {
  await server.close();
});

describe("Vite dev server", () => {
  it("serves index.html for a /cubes/<id> link so a shared URL opens the app (AD-8)", async () => {
    const response = await fetch(`${origin}${CUBE_PATH.slice(1)}`, {
      headers: { Accept: "text/html" },
    });

    expect(response.status).toBe(STATUS_OK);
    expect(await response.text()).toContain(ROOT_ELEMENT);
  });
});
