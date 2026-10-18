# rubik-cube
A Rubik’s Cube simulator with a .NET core and React UI. Rotates any face, shows the cube in an exploded view.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.302 or newer 10.0.x; pinned in `global.json`)
- [Node.js](https://nodejs.org/) 24 or newer, for the web app in `web/`
- The local .NET tools (CSharpier, listed in `.config/dotnet-tools.json`): run `dotnet tool restore` once after cloning; without it `dotnet csharpier check .` fails

## Build

```bash
dotnet build
```

The build must finish with 0 warnings and 0 errors.

## Run the tests

```bash
dotnet test
```

The tests include the TTC Test Sequence. Applied to a solved cube, `F R' U B' L D'` must render as this exploded view (W white, Y yellow, G green, B blue, R red, O orange):

```
      R O G
      B W W
      B B B
G Y Y O R R Y B O Y B W
O O G O G W R R W O B Y
B G O W W W O Y R Y Y W
      G G B
      R Y R
      R G G
```

The same sequence and expected output are stored in `tests/Fixtures/` and reused by the later API and UI tests.

## Try the API

Commands are for bash (on Windows use Git Bash). Start the API (it listens on port 5080; Swagger UI is at <http://localhost:5080/swagger> in Development):

```bash
dotnet run --project src/Rubik.Api
```

In a second terminal, create a cube. The response holds the `cubeId` and the `revision` of the empty move list; use them in the commands below:

```bash
curl -X POST http://localhost:5080/api/cubes
```

Apply the TTC Test Sequence. `expectedRevision` is the `revision` you last received; a stale one gives `409`: read the cube again with `GET` and retry. Replace `<cubeId>` and `<revision>` by hand. The `explodedView` in the response is the exploded view shown under "Run the tests" above:

```bash
curl -X POST http://localhost:5080/api/cubes/<cubeId>/moves \
  -H "Content-Type: application/json" \
  --data-binary @- <<'JSON'
{"expectedRevision":"<revision>","moves":["F","R'","U","B'","L","D'"]}
JSON
```

Undo the last move, then reset the cube (use the `revision` from the previous response each time):

```bash
curl -X POST http://localhost:5080/api/cubes/<cubeId>/undo \
  -H "Content-Type: application/json" \
  -d '{"expectedRevision":"<revision>"}'

curl -X POST http://localhost:5080/api/cubes/<cubeId>/reset \
  -H "Content-Type: application/json" \
  -d '{"expectedRevision":"<revision>"}'
```

Read the current state at any time with `curl http://localhost:5080/api/cubes/<cubeId>`.

## Run the UI

The web app lives in `web/` (React, Vite, TypeScript). Commands are for bash; run them in `web/` unless stated otherwise.

```bash
npm install
npm run dev
```

| Part | Port | Started by |
| --- | --- | --- |
| API | 5080 | `dotnet run --project src/Rubik.Api` |
| Web app (Vite dev server) | 5173 | `npm run dev` |

The browser talks only to <http://localhost:5173>. Vite forwards every `/api` request to the API on port 5080, so the API must be running too.

### Run everything in two terminals

Terminal 1, in the repository root:

```bash
dotnet run --project src/Rubik.Api --launch-profile http
```

Expected output (among other lines):

```
Now listening on: http://localhost:5080
Application started. Press Ctrl+C to shut down.
```

Terminal 2, in `web/`:

```bash
npm install
npm run dev
```

Expected output (the timing differs):

```
VITE v8.3.4  ready in 771 ms

➜  Local:   http://localhost:5173/
```

Open <http://localhost:5173>. The page creates a cube and moves to `/cubes/<cubeId>`; open that link again to get the same cube. Press **Apply TTC test sequence**. The history reads `F R' U B' L D'`; press **Show exploded view** to see the nine lines shown under "Run the tests". The button first resets a cube that already has moves, so the result is always the same. Stop both servers with Ctrl+C.

### Test commands

| What | Command | Where |
| --- | --- | --- |
| Backend unit and integration tests | `dotnet test` | repository root |
| Frontend unit and component tests (Vitest) | `npm test` | `web/` |
| End-to-end test (Playwright) | `npm run e2e` | `web/` |
| Lint and production build | `npm run lint`, `npm run build` | `web/` |

`npm run e2e` starts the API (5080) and the web app (5173) itself, opens the app in Chromium, presses the TTC button, checks the exact text, and stops both servers. Ports 5080 and 5173 must be free, so stop any server from the steps above first. Once per machine, install the browser with `npx playwright install chromium`.

### Regenerate the API types

The types in `web/src/services/types/generated/` come from the API's OpenAPI document. After any change to the API contract, start the API in Development on port 5080 (`dotnet run --project src/Rubik.Api --launch-profile http`), then run `npm run api:generate` in `web/` and commit the result. The script reads <http://localhost:5080/swagger/v1/swagger.json>, so it fails if the API is not running.
