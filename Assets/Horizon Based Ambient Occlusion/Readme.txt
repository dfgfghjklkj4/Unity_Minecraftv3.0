
Horizon Based Ambient Occlusion (HBAO) is a post processing image effect to use
in order to add realism to your scenes. It helps accentuating small surface
details and reproduce light attenuation due to occlusion.

HBAO delivers more accurate AO compared to the other SSAO solutions
available on the asset store, and this without any compromise on performances.

This algorithm is highly optimized to use minimal GPU time and offers the best
quality to performance ratio.


-----------------------------------------------------------------------------
Usage
-----------------------------------------------------------------------------

In order to use this image effect, just add HBAO compoment to your main camera.
For this you can drag the HBAO.cs script on your camera or select it from the
Add Component menu (Image Effects/HBAO).

In most scenarios, using medium quality with a medium blur will yield very good
results.

Depending if you are targetting high performances vs beautiful AO you can use
the following settings:
  - high performances: low quality with wide blur
  - normal : medium quality with medium blur
  - fine ao: high quality with narrow blur  

For even more performances you can use half resolution AO, but it generally
introduces some objectionable flickering when the camera is in movement.

Using color bleeding is heavier on performances as it requires additional color
sampling. The normal color bleeding saturation to use is 1.

Important notice:
The placement of the HBAO component in your post FX stack is really important!
It should theoretically be the first effect in the chain meaning it should be
placed on top.

Shader Model 3.0 compatible hardware is required.


-----------------------------------------------------------------------------
Forum thread & support
-----------------------------------------------------------------------------

Link to the forum thread: 
http://forum.unity3d.com/threads/horizon-based-ambient-occlusion-hbao-image-effect.387374/ 

For any support request, please drop me an email at jimmikaelkael@gmail.com


-----------------------------------------------------------------------------
Changelog
-----------------------------------------------------------------------------

3.4
  - Added LitAO on URP (requires URP 10.0.0 or newer)

3.3.4
  - Fixed VR Single Pass on URP

3.3.3
  - Fixed VR on Standard Render Pipeline

3.3.2
  - Added support for URP 12.0.0

3.3.1
  - Added support for upcoming URP 10.0.0 (support view normals)

3.3
  - Added VR support to Standard Render Pipeline HBAO

3.2.1
  - Added assembly definitions for editor scripts

3.2
  - Fixed colorbleeding white color bleed beyond max distance
  - Switched HBAO for URP to Post Process VolumeComponent

3.1
  - Added HDRP support
  - Added more settings helper methods
  - Added namespaces for each render pipelines variants
  - Use 16bit floating point texture format for noise when platform supports it
  - Fixed wrong albedoMultipier setter method

3.0
  - Rewritten scripts/shaders code for Unity 2019.1+
  - Added scene view effect
  - Added temporal filtering
  - Added interleaved gradient and spatial distribution noises
  - Added possibility to stack AO components (both Standard and Universal Render Pipeline)
  - Added UI for Universal Render Pipeline AO setting assets
  - Fixed wrong RenderTextureFormat for color bleeding
  - Fixed memory leaks
  - Improved number of compiled shader variants

2.9
  - Fixed URP version shader keywords problem resulting in blackscreen in builds
  - Fixed URP support on OpenGLES2 graphic API
  - Fixed PS4 compilation error
  - Fixed orthographic camera support on OpenGL, OpenGLES2 and OpenGLES3 graphic APIs
  - Fixed AO consistency on various resolutions (render scale, dynamic resolution)
  - Fixed incorrect view normals sampling at half resolution
  - Allowed use of local shader keywords instead of global keywords
  - Removed obsolete random noise, downsampled blur and quarter AO resolution

2.8
  - Added URP support

2.7.2
  - Fixed deprecated editor API

2.7.1
  - Fixed obsolete scripting API
  - Added view normals debug display

2.7
  - Added multibounce approximation feature to replace older luminance influence setting
  - Improved AO intensity response
  - Improved blur
  - Fixed RGB colormask on composite passes

2.6
  - Fixed emission not cancelling AO in deferred occlusion
  - Added color bleeding emissive masking
  - Fixed VR Single Pass support in Unity 2017.2

2.5
  - Added offscreen samples contribution setting
  - Improved color bleeding performance

2.4
  - Added support for Single Pass Stereo Rendering

2.3
  - Fixed Camera.hdr obsolete warning in Unity 5.6
  - Fixed inconsistent line endings warning in HBAO.shader

2.2
  - HBAO_Integrated: adjusted to avoid any per frame GC allocation
  - Increased Max Radius Pixels limits to be compliant with 4K resolution
  - Prefixed radius Slider control variable type with namespace to avoid potential ambiguity

2.1
  - Fixed orthographic camera support with Deinterleaving in Unity 5.5+

2.0
  - Added a rendering pipeline integrated HBAO component (HBAO_Integrated.cs)
  - Fixed orthographic camera support in Unity 5.5+

1.8
  - Fixed bad rendering path detection in builds
  - Explicitely declared _NoiseTex as shader property

1.7
  - Added downsampled blur setting
  - Added quarter resolution setting
  - Improved samples distribution (Mersenne Twister)
  - Fixed black line artifacts in half resolution AO using reconstructed normals
  - Fixed SV_Target semantics to the proper case
  - Fixed ambiguous lerp in HBAO fragment

1.6
  - Added Reconstruct as a per pixel normals option
  - Simplified a few lerps in HBAO shader
  - Removed sliders for max distance and falloff distance so as to remove bounds

1.5
  - Added deinterleaving which gives performances gain for large radiuses and HD/UltraHD resolutions
  - Fixed luminance influence not handled correctly in debug views

1.4.3
  - Improved overall performances
  - Added support for orthographic camera projection

1.4.2
  - Added per pixel normals setting (GBuffer or Camera)
  - Added Max Distance and Distance Falloff settings
  - Fixed AO step size, allowing to get more interesting contact occlusion
  - Moved initialization to OnEnable instead of Start
  - Avoid to modify GUI.Label style directly as to not mess up the stats window
  - Renamed "Show Type" setting to "Display Mode"

1.4.1
  - Fix editor error when applying a preset in play mode

1.4
  - Added new user friendly UI & some presets
  - Fixed luminance influence not showing in AO only views
  - Fixed vanishing AO bug in while in editor

1.3.4
  - Integrate with Gaia as an extension

1.3.3
  - Added AO base color setting
  - Fix leaked noise texture
  - Fix demo scene

1.3.2:
  - Fix bad rendering path detection in unity editor with camera on "Use Player Settings"
  - Limit the maximum radius in pixels to address the close-up objects performance issue
  - Added an albedo contribution and multiplier for the color bleeding feature in deferred shading
  - Increased intensity upper bound (useful for dark environment)

1.3.1:
  - Improved color bleeding performances
  - Fix NPE when adding the HBAO component to a camera for the 1st time

1.3:
  - Improved performances
  - Improved compatibility (now compiles on every platforms, targetting shader model 3.0)
  - Improved settings (there are less settings, but they are more user-friendly)

1.2:
  - Added Color Beeding feature
  - Fixed artifacts on cutout materials in forward rendering

1.1:
  - Improved blur
  - Added another noise type
