# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.2.2] - 2025-02-13

- Fixed a bug with RTHandles not getting disposed on a scene change.

## [3.2.1] - 2025-01-23

- Fixed a bug with Fragment not working with VR Single Pass Instanced

## [3.2.0] - 2024-12-16

- Updated to Unity 6

## [3.1.3] - 2023-10-04

This version of Auto Exposure is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue causing a buffer to leak when disposing of buffers during domain reloads.
- Fixed an issue causing a null reference exception during domain reloads.

## [3.1.2] - 2023-10-04

This version of Auto Exposure is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue when disposing of buffers and handles during dispose step.

## [3.1.1] - 2023-10-04

This version of Auto Exposure is compatible with Unity 2022.3.0f1.

### Fixed

- Fixed an issue with ps5 renderer support.

## [3.1.0] - 2023-09-15

This version of Auto Exposure is compatible with Unity 2022.3.0f1.

### Added

- Added option to configure Auto Exposure Render Pass Event from Universal Renderer Data.

## [3.0.0]

This version of Auto Exposure is compatible with Unity 2022.3.0f1.

### Changed

- Switched to RTHandles and Blitter APIs
- Changed Assembly Definition from OccaSoftware.Exposure to OccaSoftware.AutoExposure
- Renamed the override from "AutoExposure" to "AutoExposureOverride"

### Fixed

- The Fragment mode now correctly samples the compensation curve.

## [2.0.0] - 2023-05-31

### Changed

- Switched to Package type

- Changed file structure and hierarchy

### Fixed

- Suppressed CS0618 warnings in editor (relates to gratuitous deprecation warnings for new RTHandles and Blitter APIs)

- Added CustomEditor attribute for 2022.2+ to support deprecation of VolumeComponentEditor attribute in Core RP 14.0+.

## [1.5.1] - 2023-03-29

- Fixed an issue causing transparent materials to render incorrectly with Auto Exposure.
- Changed readme docs to .pdf format to help with readability

## [1.5.0] - 2023-03-28

- Added support for VR Single-Pass Instanced rendering in Fragment mode rendering.
- Changed the Fragment mode rendering downscale steps so that the source texture is downsampled three times before being sampled in the Auto Exposure calculation step. This gives more even and consistent results.

## [1.4.0] - 2023-01-23

- Auto Exposure now supports a Fragment Shader rendering mode. This rendering mode makes the asset compatible with platforms that do not support Compute Shaders.

## [1.3.0] - 2022-08-08

- Added the option to use an Exposure Compensation Curve.
- The curve is combined additively with the fixed exposure compensation.
- The curve is limited to a range of 0 -> 1 on the x-axis. This range is then remapped to the Lower Bound and Upper Bound exposure ranges.
- The curve is limited to a range of -3 -> +3 on the y-axis, representing a total exposure variance range of -0.125x -> 8x brightness. Use the fixed compensation or the Volume system for a larger dynamic range within a single Volume.
- Updated some tooltips and labels to improve clarity.
- Fixed a bug causing the scene luminance to be miscalculated when using Dynamic Render Scaling.

## [1.2.0] - 2022-06-17

- Users can now select between procedural and textural metering mask modes.
- For the procedural metering mask mode, users can now customize the falloff rate.
- For the textural metering mask mode, users can point to a custom falloff texture.
- Slightly updated the included demo scene.

## [1.1.0] - 2022-06-11

- Improved performance
- Exposure adjustment calculations now take place entirely in EV units, resulting in more responsive and consistent exposure adjustments regardless of the baseline EV of your scene.
- Converted Dark to Light and Light to Dark Time parameters to Speed parameters that reflect the speed (in F-Stops) at which the Auto Exposure can adjust. A higher value means that the effect can adjust more quickly.

## [1.0.0]

First release
