# Noita's Pixel Physics: Solving Chunk Boundary Synchronization

**Source:** Reference document on Noita's "Falling Everything" engine
**Relevance:** Implementation reference for our chunk boundary issues

---

## Overview

Noita's "Falling Everything" engine solves the exact problem you're experiencing through a **4-phase checkerboard update pattern** with **32-pixel overlap zones** between 64×64 chunks. This lock-free approach eliminates chunk edge stuttering by ensuring pixels crossing boundaries are processed by exactly one thread per frame. The technique requires no atomics, no mutexes on individual pixels, and allows particles to seamlessly transition between chunks.

---

## The Core Problem and Noita's Solution

Your issue—pixels briefly stopping at chunk edges—stems from race conditions and timing gaps when adjacent chunks are processed by different threads. Noita developer Petri Purho explained the challenge in his GDC 2019 talk: "If you were to just naively multi-thread those chunks, some problems would occur when there's a pixel that leaves its 64×64 area and wanders into the range of another chunk."

The naive fix of adding per-pixel "updated this frame" counters fails because it requires atomics or locks, destroying performance. Noita's elegant solution uses **spatial partitioning** instead of synchronization primitives.

### The 4-Phase Checkerboard Pattern

The system works as follows: In each frame, the world is updated four times. Each pass selects chunks in a checkerboard pattern (every other chunk in both dimensions). Critically, each chunk's update zone extends beyond its 64×64 boundary—pixels can move within the core area **plus 32 pixels in each cardinal direction**, creating cross-shaped processing regions. By offsetting which chunks are processed in each of the four passes, every pixel gets updated exactly once per frame, and cross-chunk movement happens atomically within a single thread's scope.

Petri Purho's exact description: "We pick every other 64×64 chunk, and the pixels inside the chunks are allowed to move within that 64×64 area plus 32 pixels in each cardinal direction. That creates little crosses. This ensures that every pixel inside that area is only updated by that thread."

---

## The 32-Pixel Movement Constraint is Essential

The entire system depends on a hard guarantee: **no pixel can move more than 32 pixels in a single frame**. This constraint is what makes the overlap zones sufficient—a particle starting anywhere in a 64×64 chunk can only reach into the adjacent 32-pixel buffer zone, never beyond into territory being processed by another thread during the same pass.

For your engine, you must either:
- Enforce a maximum movement speed of 32 pixels per frame (or whatever your overlap size is)
- Clamp pixel velocity before simulation
- Split high-velocity movements across multiple simulation substeps

If particles could teleport 40 pixels, they'd escape the buffer zone and potentially be missed or double-processed by adjacent chunk threads.

---

## Why Noita Uses Single-Buffering with Dirty Rectangles

Noita deliberately avoids double-buffering, which is unusual for cellular automata. Petri Purho explained: "When you do double buffering, you have to update everything even when applying multithreading." The single-buffer approach enables **dirty rectangle optimization**—each chunk tracks which pixels actually need updates, and static regions are skipped entirely.

Each 64×64 chunk maintains its own dirty rect indicating active pixel regions. When something moves within a chunk, it marks that area for update. This dramatically reduces iteration: instead of processing millions of pixels per frame, only chunks with activity are touched. Static stone walls, settled sand, and calm liquids cost essentially nothing to simulate.

The tradeoff is that you need the checkerboard update pattern to prevent race conditions when writing to the shared buffer.

---

## Implementing Chunk Boundary Wake-Up Systems

Beyond the 4-phase pattern, you likely need a **chunk wake-up mechanism**. When particles approach chunk boundaries, adjacent chunks must be activated even if they were previously dormant. Without this, a particle reaching a sleeping chunk's edge has nowhere to go.

A practical implementation from the falling sand community uses boundary ping detection:

```cpp
void MoveCell(int x, int y, int xto, int yto) {
    int pingX = 0, pingY = 0;
    if (x == m_chunk->m_x)                         pingX = -1;
    if (x == m_chunk->m_x + m_chunk->m_width - 1)  pingX = 1;
    if (y == m_chunk->m_y)                         pingY = -1;
    if (y == m_chunk->m_y + m_chunk->m_height - 1) pingY = 1;

    if (pingX != 0) m_world.KeepAlive(x + pingX, y);
    if (pingY != 0) m_world.KeepAlive(x, y + pingY);
}
```

This ensures that whenever a particle moves near a chunk edge, the neighboring chunk wakes up and participates in the next frame's simulation. Your stuttering issue may partly stem from chunks not being ready to receive incoming particles.

---

## Cross-Chunk Movement Buffering

For moves that genuinely cross chunk boundaries, buffer them rather than applying immediately. Store pending cross-chunk moves as tuples: `(source_chunk, source_index, destination_index)`. After all parallel chunk processing completes, a synchronization step commits these buffered moves atomically.

```cpp
std::vector<std::tuple<SandChunk*, size_t, size_t>> m_changes;

void CommitCells() {
    // Filter invalid moves (destination already filled)
    // Resolve conflicts (multiple particles targeting same cell)
    // Apply moves atomically
    m_changes.clear();
}
```

This commit step runs single-threaded after parallel processing, but it handles only boundary-crossing particles—a tiny fraction of total movement—so the performance cost is minimal.

---

## Update Order Prevents Visual Artifacts

Noita processes the world **bottom-up** for falling materials. If you process top-to-bottom, only the bottom-most pixel in a column falls each frame; pixels above wait until the next frame, creating slow, frame-by-frame cascading. Bottom-up processing ensures entire columns can fall simultaneously.

Petri Purho: "In order to make the falling sand simulation work, you have to update the world from the bottom up. The reason is that if you don't do that, only the pixels at the very bottom will fall down. The next frame, the pixels above those will move down and so forth."

Additionally, randomize left-right processing direction per row to prevent directional bias. Without this, sand consistently piles toward one side:

```javascript
const direction = Math.random() > 0.5 ? 'ltr' : 'rtl';
```

---

## Alternative Parallelization: Margolus Neighborhood

If the 4-phase checkerboard is too complex, consider the **Margolus neighborhood** approach used in GPU implementations. The world divides into 2×2 non-overlapping blocks. Each block is processed independently (easily parallelizable), then the grid shifts diagonally by one cell for the next iteration. This creates a Z-pattern of four phases that covers all cells without overlap.

The downside: particles become visually separated by blank spaces at small scales, though this becomes less noticeable with larger particle counts or higher resolutions.

---

## Technical Specifications Summary

| Parameter | Noita's Value | Your Consideration |
|-----------|---------------|-------------------|
| Simulation chunk size | 64×64 pixels | Choose based on cache line size and thread count |
| Streaming chunk size | 512×512 pixels | For disk persistence, independent of sim chunks |
| Overlap buffer | 32 pixels per cardinal direction | Must be ≥ max pixel velocity |
| Update phases | 4 per frame | Creates complete coverage of checkerboard |
| Max pixel velocity | 32 pixels/frame | Enforced hard limit |
| Active chunks | ~12 loaded | Memory vs. off-screen update tradeoff |
| Buffer strategy | Single-buffered | Enables dirty rect optimization |
| Physics backend | Box2D | For rigid body interaction |

---

## Conclusion

Your chunk boundary stuttering almost certainly stems from one of three issues: insufficient overlap zones between chunks (anything less than your maximum pixel velocity will cause gaps), missing chunk wake-up logic (dormant chunks don't accept incoming particles), or processing order problems (all four checkerboard phases must complete before moving to the next frame). The 4-phase checkerboard with 32-pixel cardinal extensions is the proven solution—it's been shipping in Noita since 2019 and handles millions of simulated pixels. Implement the overlap zones first, add the wake-up ping system, and ensure your commit step for cross-chunk moves runs after all parallel work completes.

---

## Key Takeaways for Implementation

1. **Hard movement constraint**: ALL movement types (velocity, spreading, pathfinding) must respect the buffer limit
2. **Extended regions**: Each chunk processes core + buffer zones in all cardinal directions
3. **4-phase pattern**: Sequential group processing (A, B, C, D) with parallel chunks within each group
4. **Spatial isolation**: Same-group chunks must NEVER access overlapping cells
5. **Single-buffering**: Enables dirty rect optimization but requires careful spatial partitioning
6. **Chunk wake-up**: Adjacent chunks must be activated when particles approach boundaries
7. **Bottom-up processing**: Essential for proper falling behavior
8. **Randomized X direction**: Prevents directional bias in settling

The core insight: **Use spatial partitioning to eliminate the need for synchronization primitives.** No locks, no atomics, no per-pixel counters—just careful mathematical guarantees that threads never access the same memory.
