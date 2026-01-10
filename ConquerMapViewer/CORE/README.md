# Conquer Map Viewer

A map viewer for Conquer Online built with .NET 8, WPF, and MonoGame.

## Architecture

This project follows Clean Architecture principles with the following layers:

### Core Layer (`ConquerMapViewer.Core`)
- Domain entities and value objects
- Core interfaces
- Business logic services
- No dependencies on external frameworks

### Infrastructure Layer (`ConquerMapViewer.Infrastructure`)
- File system implementations (WDF packages, 7z archives)
- File loaders (Map, Puzzle, ANI)
- Repositories
- External library integrations

### Rendering Layer (`ConquerMapViewer.Rendering`)
- MonoGame-specific rendering logic
- Drawing components
- Coordinate system transformations
- Primitives and vertex builders

### Presentation Layer (`ConquerMapViewer.WPF`)
- WPF UI with MonoGame integration
- MVVM pattern
- Dependency injection
- View models and views

## Key Features

- **Isometric map rendering** with zoom and pan
- **Layer support** for puzzles, backdrops, terrain objects, portals
- **Cell visualization** with access type coloring
- **MonoGame + WPF integration** for modern UI
- **Clean separation of concerns** with DI container
- **Resource management** with proper disposal patterns

## Building

Requires:
- .NET 8 SDK
- Visual Studio 2022 or Rider

```bash
dotnet build
dotnet run --project src/ConquerMapViewer.WPF
```

## Configuration

Update the `ConquerDirectory` path in `ServiceConfiguration.cs`:
```csharp
const string conquerDirectory = @"C:\Path\To\Your\Conquer\Files\";
```
