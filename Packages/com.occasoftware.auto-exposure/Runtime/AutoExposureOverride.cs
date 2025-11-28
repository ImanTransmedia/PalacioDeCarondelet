using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.AutoExposure.Runtime
{
    public enum AutoExposureMode
    {
        Off,
        On
    }

    public enum AutoExposureAdaptationMode
    {
        Progressive,
        Instant
    }

    public enum AutoExposureMeteringMaskMode
    {
        Procedural,
        Textural
    }

    public enum AutoExposureRenderingMode
    {
        Compute,
        Fragment
    }

    [Serializable]
    [VolumeComponentMenu("Post-processing/Auto Exposure")]
    public sealed class AutoExposureOverride : VolumeComponent, IPostProcessComponent
    {
        [Tooltip(
            "Set to On to enable auto exposure processing. This effect automatically overrides the Color Adjustments \"Post Exposure\" Setting."
        )]
        public AutoExposureModeParameter mode = new AutoExposureModeParameter(AutoExposureMode.Off);

        [Tooltip(
            "You can clamp the lower bound of exposure values. Clamping the lower bound to a high value such as 1 will cause darker scenes to be overexposed. Refer to documentation for more detail on understanding EV values."
        )]
        public FloatParameter evMin = new FloatParameter(0);

        [Tooltip(
            "You can clamp the upper bound of exposure values. Clamping the upper bound to a low value such as 5 will cause brighter scenes to be underexposed. Refer to documentation for more detail on understanding EV values."
        )]
        public FloatParameter evMax = new FloatParameter(12);

        [Tooltip(
            "You can use the Exposure Compensation setting to adjust the baseline exposure for your shots. Exposure Compensation follows a power curve. -2 is one-fourth as bright, -1 is one-half as bright, 0 is default, +1 is twice as bright, +2 is four times as bright, and so on."
        )]
        public FloatParameter evCompensation = new FloatParameter(0);

        [Tooltip(
            "You can configure an additional Exposure Compensation Curve that is applied on top of the fixed Exposure Compensation setting. Your curve should at least include the range of the EV Min to EV Max values. Exposure Compensation follows a power curve. -2 is one-fourth as bright, -1 is one-half as bright, 0 is default, +1 is twice as bright, +2 is four times as bright, and so on."
        )]
        public TextureCurveParameter compensationCurveParameter = new TextureCurveParameter(
            new TextureCurve(
                new[] { new Keyframe(0f, 0f), new Keyframe(1f, 0f) },
                0f,
                false,
                new Vector2(0f, 1f)
            )
        );

        [InspectorName("Mode")]
        [Tooltip(
            "The mode that will be used to adjust exposure over time. Progressive animates the exposure over time. Instant jumps the exposure to the target value instantly."
        )]
        public AutoExposureAdapationModeParameter adaptationMode =
            new AutoExposureAdapationModeParameter(AutoExposureAdaptationMode.Progressive);

        [Tooltip(
            "The rate (in f-stops) at which the Auto Exposure will adjust from dark to light scenes. Eyes are faster to adapt when moving from a dark to a light environment, so this value should generally be larger than the Light to Dark Speed."
        )]
        public FloatParameter darkToLightSpeed = new FloatParameter(3);

        [Tooltip(
            "The rate (in f-stops) at which the Auto Exposure will adjust from light to dark scenes. Eyes are slower to adapt when moving from a light to a dark environment, so this value should generally be smaller than the Dark to Light Speed."
        )]
        public FloatParameter lightToDarkSpeed = new FloatParameter(1);

        [Tooltip(
            "Set whether the metering mask should be sampled from a procedural radial falloff or a texture mask (to be provided by user). Textural mode with no texture results in no metering mask being applied."
        )]
        public AutoExposureMeteringMaskModeParameter meteringMaskMode =
            new AutoExposureMeteringMaskModeParameter(AutoExposureMeteringMaskMode.Procedural);

        [Tooltip(
            "A texture mask used to weight the relative importance of samples taken from various positions on screen. Automatically stretched to fit. Greyscale texture is expected and only the red channel is sampled."
        )]
        public TextureParameter meteringMaskTexture = new TextureParameter(null);

        [Tooltip(
            "Controls the radial importance falloff for procedural metering masks. A higher value means that the importance decreases more rapidly as you approach the screen edges."
        )]
        public MinFloatParameter meteringProceduralFalloff = new MinFloatParameter(2, 0);

        [Tooltip(
            "Set the method used to calculate and apply the auto exposure. Compute is faster and more accurate, but only works on Compute-capable systems (SM4.5+)."
        )]
        public AutoExposureRenderingModeParameter renderingMode =
            new AutoExposureRenderingModeParameter(AutoExposureRenderingMode.Compute);

        [Tooltip(
            "Controls the number of samples taken to estimate the screen brightness. Higher sample counts increase the performance cost."
        )]
        public IntParameter sampleCount = new IntParameter(30);

        [Tooltip(
            "Set to On to randomize the sample positions each frame. Creates jitter when enabled. Set to Off to use fixed sample positions. Creates sudden jumps in exposure when disabled."
        )]
        public AutoExposureModeParameter animateSamplePositions = new AutoExposureModeParameter(
            AutoExposureMode.Off
        );

        [Tooltip(
            "Sets the temporal filtering applied to the screen brightness estimate. A higher value means that more recent samples are weighted more strongly."
        )]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.03f, 0, 1);

        [Tooltip(
            "Set to On to enable sample clamping. Sample clamping reduces the variance of results in high-variance screen conditions at the cost of lower responsiveness and less accurate results."
        )]
        public AutoExposureModeParameter clampingEnabled = new AutoExposureModeParameter(
            AutoExposureMode.Off
        );

        [Tooltip(
            "Controls the size of the clamping bracket in luminance units. A value of 1 corresponds to a bracket equal to the trailing luminance +/- 1."
        )]
        public FloatParameter clampingBracket = new FloatParameter(2);

        public bool IsActive()
        {
            if (mode.value == AutoExposureMode.Off)
                return false;

            return true;
        }

        public bool IsTileCompatible() => false;
    }

    [Serializable]
    public sealed class AutoExposureModeParameter : VolumeParameter<AutoExposureMode>
    {
        public AutoExposureModeParameter(AutoExposureMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class AutoExposureAdapationModeParameter
        : VolumeParameter<AutoExposureAdaptationMode>
    {
        public AutoExposureAdapationModeParameter(
            AutoExposureAdaptationMode value,
            bool overrideState = false
        )
            : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class AutoExposureMeteringMaskModeParameter
        : VolumeParameter<AutoExposureMeteringMaskMode>
    {
        public AutoExposureMeteringMaskModeParameter(
            AutoExposureMeteringMaskMode value,
            bool overrideState = false
        )
            : base(value, overrideState) { }
    }

    [Serializable]
    public sealed class AutoExposureRenderingModeParameter
        : VolumeParameter<AutoExposureRenderingMode>
    {
        public AutoExposureRenderingModeParameter(
            AutoExposureRenderingMode value,
            bool overrideState = false
        )
            : base(value, overrideState) { }
    }
}
