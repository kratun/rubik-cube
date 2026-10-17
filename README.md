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
