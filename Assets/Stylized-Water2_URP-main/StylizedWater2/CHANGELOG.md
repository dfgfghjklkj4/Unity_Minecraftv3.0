1.1.4

Added:
- WaterObject.Find function. Attempts to find the WaterObject above or below the given position.
    * Floating Transform component now has an "Auto-find" option
- Buoyancy.Raycast function. Given a position and direction, finds the intersection point with the water
- Planar Reflections, maximum LOD quality setting. Can be used to limit reflected objects to LOD1 or LOD2
- Caustics distortion parameter. Offsets the caustics based on the normal map.

Changed:
- Planar Reflections, standard fog no longer renders into to the reflection. This can cause artifacts where it "bleeds" over partially culled geometry

Fixed:
- Azure fog library file not being found (GUID was changed)

1.1.3

Added:
- Vertical depth parameter, controls the density of the water based on viewing angle
- Realistic water material (ocean-esque)

Changed:
- Water meshes created through the utility or Water Grid component now have some height. Avoids premature culling when using high waves.
- Water Mesh utility can now automatically apply changes when parameters are modified.
- Setup actions under the Window menu now support Undo/redo actions
- Improved blending of normal maps for the Advanced shading mode
- Wave normals now distort caustics, creating a more believable effect
- Translucency shading now takes the surface curvature into account (controllable through new parameter)
- UI: translucency moved to the Lighting/Shading tab

Fixed:
- Water depth not rendering correctly when using an orthographic camera and OpenGL
- WaterMesh.Create function not taking the chosen shape into account, always creating a rectangle
- Integration for Dynamic Water Physics 2 no longer working since v2.4.2 (now the minimum required version)
- Refraction and reflection distortion appearing much stronger for orthographic camera's

1.1.2
This version includes some changes required for the Underwater Rendering extension

Added:
- Tessellation support (preview feature). Can be enabled under the "Advanced" tab.
- Added menu item to auto-setup Planar Reflections or create a Water Grid
- Hard-coded file paths for third-party fog libraries are now automatically rewritten
- Fog can now be globally disabled for the water through script (see "Third party integrations" section in docs)
- Support for reflection probe blending and box projection. Requires 2021.2.0b1+, will break in an alpha version
- (Pre-emptive) Support for rendering debug window in 2021.2.0b1

Changed:
- Menu item "Help/Stylized Water 2" moved to "Window/Stylized Water 2/Hub"
- Improved refraction, this now takes the surface curvature into account. Maximum allowed strength increased to x3

Fixed:
- Fog integration for SC Post Effects and Atmospheric Height Fog (v2.0.0+)

1.1.1

Hotfix for build-blocking error in some cases. Shader variants for features not installed are now stripped during the build process

1.1.0

Added:
- Diorama example scene
- Exposed parameter to control the size of point/spot light specular reflections

Changed:
- Translucency shading will now also work if lighting is disabled
- Intersection- and surface foam and now appear correct when using the Gamma color-space
- Surface foam can now be painted using the Alpha channel of the painted color. Previously the Blue channel was used only if River Mode was enabled.
- Planar Reflection Renderer component now shows a warning if reflection were globally disabled by an external script

Fixed:
- Scripting build error on tvOS due to VR code being stripped for the platform
- When river mode is enabled, a second layer of foam on slopes wasn't visible.
- Planar Reflection Renderer component needing to be toggled when using multiple instances
- Buoyancy not being in sync when animation speed was higher than x1

1.0.9

Added:
- Support for SC Post Effects Pack fog rendering (activated through the Help window)

Changed:
- Material UI now has a help toggle button, with quick links to documentation
- Buoyancy.SampleWaves function now has a boolean argument, to indicate if the material's wave parameters are being changed at runtime. Version without this has been marked as obsolete.
- Floating Transform component now supports multi-selection

Fixed:
- Planar reflections failing to render if the water mesh was positioned further away than its size
- Warning about obsolete API in Unity 2021.2+ due to URP changes, package will now import without any interuptions
- Shader error when Enviro fog was enabled, due to a conflicting function name

1.0.8

Added:
- Distance normals feature, blends in a second normal map, based on a start/end distance.
- Distance fade parameter for waves. Waves can smoothly fade out between a start/end distance, to avoid tiling artifacts when viewed from afar.
- Planar Reflections, option to specify the renderer to be used for reflection. This allows a set up where certain render features aren't being applied to the reflection

Changed:
- Planar Reflections inspector now shows a warning and quick-fix button if the bounds need to be recalculated.
- Material section headers now also have tooltips
- Water Grid improvements, now only recreates tiles if the rows/colums value was changed

Fixed:
- Issues with DWP2 integration since its latest update

1.0.7

Added:
- Planar Reflections, option to include skybox.

Changed:
- Greatly improved caustics shading. No longer depends on the normal map for distortion.
- Normal map now has a speed multiplier, rather than being bound to the wave animation speed. If any custom materials were made, these likely have to be adjusted
- Updated default filepath for Boxophobic's Atmospheric Height Fog. This requires to reconfigure the fog integration through the Help window.
- Removed "MainColor" attribute from shader's deep color, to avoid Unity giving it a white color value for new materials
- Floating Transform, if roll strength is 0, the component will no longer modify the transform's orientation

Fixed:
- Objects above/in front of the water are no longer being refracted. Requires some additional legwork, so is limited to the Advanced shading mode.
- (Preliminary) Error about experimental API in Unity 2021.2+

1.0.6

Added:
- River mode, forces animations to flow in the vertical UV direction and draws surface foam on slopes (with configurable stretching and speed).
- Waterfall splash particle effect
- Data provider script for Dynamic Water Physics 2 is now part of the package (can be unlocked through the Help window if installed)
- Curvature mask parameter for environment reflections, allows for more true-to-nature reflections

Changed:
- Water hole shader can now also be used with Curved World 2020

Fixed:
- Planar reflections, reflected objects being sorting in reverse in some cases
- Mobile, animations appearing stepped on some devices when using Vulkan rendering
- (Preliminary) Error in URP 11, due to changes in shader code

1.0.5

Added:
- Particle effects, composed out of flipbooks with normal maps:
    * Big splash
    * Stationary ripples
    * Trail
    * Collision effect (eg. boat bows)
    * Splash ring (eg. footsteps)
    * Waterfall mist
    * Splash upwards
- Water Grid component, can create a grid of water objects and follow a specific transform.

Fixed:
- Material turning black if normal map was enabled, yet no normal map was assigned.
- Intersection texture was using mesh UV, even when UV source was set to "World XZ Projected".

Changed:
- Waves no longer displace along the mesh's normal when "World XZ projected" UV is used, which was incorrect behaviour
- Sparkles are no longer based on sun direction, this way they stay a consistent size in dynamically lit scenes. Instead they fade out when the sun approaches the horizon.
- When using Flat Shading, refraction and caustics still use the normal map, instead of the flat face normals.
- Planar Reflections render scale now takes render scale configured in URP settings into account
- Improved Rough Waves and Sea Foam textures

1.0.4

Added:
- CanTouchWater function to Buoyancy class: Checks if a position is below the maximum possible wave height. Can be used as a fast broad-phase check, before actually using the more expensive SampleWaves function
- Context menu option to Transform component to add Floating Transform component

Fixed:
- Error in material UI when no pipeline asset was assigned in Graphics Settings
- Floating Transform, sample points not being saved if object was a prefab

Changed:
- Revised demo content
- Floating Transform no longer animates when editing a prefab using the component
- Minor UI improvements

1.0.3

Added:
- Water Object script, as a general means of identifying and finding water meshes between systems, this is now attached to all prefabs
- Planar reflections renderer, enables mirror-like reflections from specific layers

Changed:
- Buoyancy sample positions of Floating Transform can now be manipulated in the scene view

Fixed:
- Error when assigning material to Floating Transform component, or not being able to

1.0.2
Verified compatibility with Wavemaker

Added:
- Support for Boxophobic's Atmospheric Height Fog (can be enabled through the Help window)

Changed:
- Shader now correctly takes the mesh's vertex normal into account, making it suitable for use with spheres and rivers

1.0.1
Verified compatibility with Oculus Quest (see compatibility section of documentation, some caveats apply due to an OpenGLES bug)

Fixed:
- Hard visible transition in translucency effect when exponent was set to 1
- Back faces not visible when culling was set to "Double-sided" and depth texture was enabled

1.0.0
Initial release (Nov 3 2020)