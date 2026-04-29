# Graphics Rendering Pipeline - Detailed Explanation

## Overview: What Happens When You Draw Something

Every single frame of your game, the following happens roughly 60 times per second. Understanding this flow is crucial for optimization and achieving specific visual effects.

---

## STAGE 1: Application Stage (CPU)

This is YOUR code running on the CPU before anything hits the GPU.

### 1.1 Game State Update
```
- Process input
- Run physics simulation  
- Update animations
- AI decisions
- Move objects in the world
```

### 1.2 Scene Traversal & Culling

**Frustum Culling**: Don't send objects outside the camera's view to the GPU.
```csharp
// Conceptually what happens:
foreach (var object in allObjects)
{
    if (camera.Frustum.Contains(object.BoundingBox) != ContainmentType.Disjoint)
    {
        visibleObjects.Add(object);
    }
}
```

**Why this matters**: The fastest triangle to render is one you never send to the GPU.

### 1.3 Sorting & Batching

Objects are sorted to minimize state changes:
```
1. Sort by shader/material (switching shaders is expensive)
2. Sort by texture (texture switches are moderately expensive)
3. Sort opaque objects front-to-back (early Z rejection)
4. Sort transparent objects back-to-front (correct blending)
```

### 1.4 Build Draw Commands

The CPU prepares "draw calls" - instructions for the GPU:
```
Draw Call = {
    Shader to use,
    Textures to bind,
    Vertex buffer,
    Index buffer,
    Transform matrices,
    How many triangles
}
```

**Critical concept**: Each draw call has overhead. 1000 objects with 1 triangle each is SLOWER than 1 object with 1000 triangles. This is why batching exists.

---

## STAGE 2: Geometry Stage (GPU - Vertex Processing)

### 2.1 Input Assembler (Fixed Function)

The GPU fetches raw vertex data from memory:
```
Vertex Buffer: [Position, Normal, UV, Color, ...]
Index Buffer:  [0, 1, 2, 2, 1, 3, ...]  // Which vertices form triangles

Triangle 1: vertices 0, 1, 2
Triangle 2: vertices 2, 1, 3
...
```

### 2.2 Vertex Shader (Programmable - YOU WRITE THIS)

Runs once PER VERTEX. This is where the magic coordinate transforms happen.

**The Transform Chain:**
```
Model Space (Local)     →  Where vertex is relative to object origin
    ↓ World Matrix
World Space             →  Where vertex is in the game world  
    ↓ View Matrix
View/Camera Space       →  Where vertex is relative to camera
    ↓ Projection Matrix
Clip Space              →  Homogeneous coordinates for clipping
    ↓ Perspective Divide
NDC (Normalized Device) →  -1 to +1 cube
    ↓ Viewport Transform  
Screen Space            →  Actual pixel coordinates
```

**In HLSL:**
```hlsl
VertexOutput VS(VertexInput input)
{
    VertexOutput output;
    
    // Model → World
    float4 worldPos = mul(input.Position, World);
    
    // World → View  
    float4 viewPos = mul(worldPos, View);
    
    // View → Clip (Projection)
    output.Position = mul(viewPos, Projection);
    
    // This output.Position is in CLIP SPACE
    // GPU automatically does perspective divide and viewport transform
    
    return output;
}
```

### 2.3 Why N64 Lighting is "Vertex Lit"

**Per-Vertex (N64 style):**
```
- Calculate lighting AT EACH VERTEX in vertex shader
- Interpolate the resulting COLOR across the triangle
- Fast, but lighting "swims" on low-poly models
- Distinct retro look
```

**Per-Pixel (Modern):**
```
- Pass normals to pixel shader
- Calculate lighting AT EACH PIXEL
- Smooth, accurate, but more expensive
- Modern realistic look
```

### 2.4 Clipping

Triangles partially outside the view frustum get clipped:
```
Before:  Triangle with 1 vertex behind camera
After:   Quad (2 triangles) with vertices on the near plane

The GPU generates NEW vertices where edges cross clip planes.
```

---

## STAGE 3: Rasterization (Fixed Function)

### 3.1 What Rasterization Actually Does

Converts vector triangles into discrete pixels (fragments).

```
Input:  3 vertices with screen coordinates
Output: List of all pixels covered by triangle, with interpolated attributes

    V0 (100, 50)
       /\
      /  \
     /    \
    /______\
V1 (50,150) V2 (150,150)

Pixels covered: (75,75), (76,75), (77,75)... hundreds of them
Each pixel gets interpolated UV, color, normal, etc.
```

### 3.2 Interpolation

For each pixel inside the triangle, attributes are interpolated using barycentric coordinates:
```
Pixel at center of triangle:
  UV = (V0.uv + V1.uv + V2.uv) / 3
  Color = (V0.color + V1.color + V2.color) / 3
  
Pixel near V0:
  UV ≈ V0.uv (weighted more heavily)
```

**Perspective-Correct Interpolation**: Modern GPUs correct for perspective distortion. N64 had issues with this (affine texture warping).

---

## STAGE 4: Pixel/Fragment Stage (GPU)

### 4.1 Pixel Shader (Programmable - YOU WRITE THIS)

Runs once PER PIXEL (fragment). This is typically the performance bottleneck.

```hlsl
float4 PS(VertexOutput input) : COLOR0
{
    // Sample texture at interpolated UV
    float4 texColor = tex2D(diffuseSampler, input.TexCoord);
    
    // Multiply by interpolated vertex color (contains lighting for N64 style)
    float4 finalColor = texColor * input.Color;
    
    // Apply fog based on depth
    finalColor.rgb = lerp(finalColor.rgb, fogColor, input.FogFactor);
    
    return finalColor;
}
```

### 4.2 Texture Sampling

When you call `tex2D()`, the GPU:
```
1. Takes UV coordinates (0-1 range typically)
2. Multiplies by texture dimensions to get texel coordinates
3. Applies filtering:
   - Point/Nearest: Grab closest texel (chunky N64 look)
   - Bilinear: Blend 4 nearest texels (smooth but blurry)
   - Trilinear: Bilinear + blend between mip levels
   - Anisotropic: Fancy filtering for angled surfaces
```

**For N64 aesthetic: USE POINT FILTERING**

### 4.3 Per-Fragment Tests

After the pixel shader, several tests determine if the pixel actually gets written:

```
Scissor Test  → Is pixel inside scissor rectangle?
Alpha Test    → Is alpha above threshold? (discard if not)
Stencil Test  → Does stencil buffer allow this write?
Depth Test    → Is this pixel closer than what's already there?
```

**Early-Z Optimization**: Modern GPUs can do depth test BEFORE pixel shader if shader doesn't modify depth. This is why front-to-back sorting helps - rejected pixels skip the expensive pixel shader.

### 4.4 Output Merger / Blending

Final combination with the framebuffer:

```
For Opaque:
  Framebuffer[x,y] = ShaderOutput  // Just overwrite

For Transparent (Alpha Blending):
  Framebuffer[x,y] = ShaderOutput * Alpha + Framebuffer[x,y] * (1 - Alpha)

For Additive (fire, glow):
  Framebuffer[x,y] = ShaderOutput + Framebuffer[x,y]
```

---

## N64 Specifics: How OoT Did It

### The RCP (Reality Coprocessor)

```
┌─────────────────────────────────────────┐
│               N64 RCP                    │
│  ┌─────────────┐  ┌─────────────────┐   │
│  │     RSP     │  │       RDP       │   │
│  │  (Vector    │  │   (Rasterizer)  │   │
│  │  Processor) │  │                 │   │
│  │             │  │ - Texturing     │   │
│  │ - Transform │→→│ - Color Combine │   │
│  │ - Lighting  │  │ - Blending      │   │
│  │ - Clipping  │  │ - Z-Buffer      │   │
│  └─────────────┘  └─────────────────┘   │
└─────────────────────────────────────────┘
```

### Display Lists

OoT didn't make "draw calls" like we do. It built DISPLAY LISTS:
```c
// This is actual OoT decompiled code pattern
Gfx* gfx = POLY_OPA_DISP;

gSPMatrix(gfx++, modelMatrix, G_MTX_MODELVIEW | G_MTX_LOAD);
gSPVertex(gfx++, vertices, 32, 0);
gSP2Triangles(gfx++, 0, 1, 2, 0, 3, 4, 5, 0);
gDPSetPrimColor(gfx++, 0, 0, 255, 255, 255, 255);
gDPLoadTextureBlock(gfx++, texture, G_IM_FMT_RGBA, G_IM_SIZ_16b, 32, 32, ...);

POLY_OPA_DISP = gfx;
```

These commands were DMA'd to the RCP and executed.

### Color Combiner

The RDP had a "color combiner" - a fixed-function unit that combined colors:
```
Output = (A - B) * C + D

Where A, B, C, D could be:
- Texture color
- Vertex color (lighting)
- Primitive color (set by CPU)
- Environment color
- Shade (interpolated vertex colors)
- Constants (0, 1)
```

This is like a VERY LIMITED pixel shader. To replicate in MonoGame:
```hlsl
// Simulating N64 color combiner
float4 combined = (texColor - envColor) * vertexColor + envColor;
```

---

## Performance Implications

### What's Expensive:

| Operation | Cost | Why |
|-----------|------|-----|
| State Changes | HIGH | GPU pipeline stall |
| Texture Switches | MEDIUM | Cache miss |
| Draw Calls | MEDIUM | CPU overhead |
| Pixel Shader | MEDIUM | Runs per pixel |
| Vertex Shader | LOW | Runs per vertex (fewer) |
| Triangle Count | LOW | GPU is built for this |

### Optimization Priorities:

1. **Batch draw calls** - Combine meshes using same material
2. **Sort by state** - Minimize shader/texture switches
3. **Frustum cull** - Don't draw what's not visible
4. **LOD** - Fewer triangles at distance
5. **Occlusion cull** - Don't draw what's hidden
6. **Early-Z** - Sort opaque front-to-back

---

## Your MonoGame Retro Pipeline

For your N64-style game:

```
Frame Start
    │
    ├─► Clear low-res render target (320x240)
    │
    ├─► For each visible object:
    │       - Set World matrix
    │       - Bind vertex lit shader
    │       - Bind texture (point filtered)  
    │       - Draw mesh
    │
    ├─► Switch to backbuffer (1920x1080)
    │
    ├─► Draw low-res target as fullscreen quad
    │       - Point filter upscale (crispy pixels)
    │       - Optional: dither shader
    │       - Optional: scanlines
    │
    └─► Present()
```

This two-pass approach gives you:
- Authentic low-resolution rendering
- Chunky pixels when upscaled
- Room for post-processing effects
- Modern display compatibility
