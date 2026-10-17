# rubik-cube
A Rubik’s Cube simulator with a .NET core and React UI. Rotates any face, shows the cube in an exploded view.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.302 or newer 10.0.x; pinned in `global.json`)

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
