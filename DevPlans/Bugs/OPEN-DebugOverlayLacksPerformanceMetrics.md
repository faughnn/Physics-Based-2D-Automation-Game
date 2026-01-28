# Bug: Debug Overlay Lacks Performance Profiling Metrics

**Status:** OPEN
**Reported:** 2026-01-28

## Description

The debug overlay (F3) does not provide enough information to identify where performance bottlenecks are occurring. There is no breakdown of time spent in each simulation phase, rendering costs, or other key metrics needed to diagnose frame rate issues.

## Desired Metrics

- **Simulation timing:** Time spent per simulation pass (groups A/B/C/D), belt simulation, cluster simulation
- **Chunk activity:** Number of active/dirty chunks vs total chunks, active cell count
- **Rendering timing:** Time spent uploading chunk textures, number of chunks re-uploaded per frame
- **Job scheduling:** Time spent scheduling vs executing Burst jobs
- **Per-system breakdown:** Belts, lifts, clusters, terrain colliders, digging — time per system per frame
- **Frame budget:** How much of the 16.6ms (60fps) or 33.3ms (30fps) budget is consumed and by what

## Severity

Low — quality of life for development and optimization work. Not a runtime bug but a tooling gap that slows down performance investigation.
