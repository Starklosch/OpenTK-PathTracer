# C# OpenGL OpenTK Path Tracer

---

This is a working fork of [this repo](https://github.com/JulianStambuk/OpenTK-PathTracer/tree/master) with some differences:
- Doesn't depend on ARB_direct_state_access
- Compatible with OpenGL 4.3 and newer
- Minor changes

---

[Path Traced](https://en.wikipedia.org/wiki/Path_Tracing) renderer written in C#.

The calculations and rendering are done in real time using OpenGL. 
The whole Scene (only consisting out of Cuboids and Spheres for now) is loaded to a UBO which is then accessed in a Compute Shader where the Path Tracing is done.
Due to the realistic nature of Path Tracers various effects like Soft Shadows, Reflections or Ambient Occlusion emerge automatically without explicitly adding code for any of these effects like you would have to do in a traditional rasterizer.

The renderer also features [Depth of Field](https://en.wikipedia.org/wiki/Depth_of_field), which can be controlled with two variables at runtime through [ImGui](https://github.com/ocornut/imgui).
`FocalLength` is the distance an object appears in focus.
`ApertureDiamter` controlls how strongly objects out of focus are blured.

If a ray does not hit any object the color is retrieved from a cubemap which can either be 6 images inside the `Res` folder or a precomputed skybox. The atmospheric scattering in this skybox gets calculated in yet an other Compute Shader at startup.

Screenshots taken via the screenshot feature are saved in the local execution folder `Screenshots`.

Also see https://youtu.be/XcIToi0fh5c.

---

## **Controls**

### **KeyBoard:**
* E => Toggle cursor visibility.
* F11 => Toggle fullscreen.
* V => Toggle VSync.
* Esc => Close.

* W, A, S, D => Movement.
* LShift => Faster movement speed
* LControl => Slower movement speed

---

## **Render Samples**

![img1](https://github.com/Starklosch/OpenTK-PathTracer/blob/main/Screenshots/img1.png?raw=true)

![img2](https://github.com/Starklosch/OpenTK-PathTracer/blob/main/Screenshots/img2.png?raw=true)

![img3](https://github.com/Starklosch/OpenTK-PathTracer/blob/main/Screenshots/img3.png?raw=true)
