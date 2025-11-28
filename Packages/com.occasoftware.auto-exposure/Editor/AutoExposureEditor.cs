using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using OccaSoftware.AutoExposure.Runtime;

namespace OccaSoftware.AutoExposure.Editor
{
    [CustomEditor(typeof(AutoExposureOverride))]
    public class AutoExposureEditor : VolumeComponentEditor
    {
        SerializedDataParameter mode;
        SerializedDataParameter evMin;
        SerializedDataParameter evMax;
        SerializedDataParameter evCompensation;
        SerializedDataParameter compensationCurveParameter;
        SerializedDataParameter adaptationMode;
        SerializedDataParameter darkToLightTime;
        SerializedDataParameter lightToDarkTime;
        SerializedDataParameter meteringMaskMode;
        SerializedDataParameter meteringMaskTexture;
        SerializedDataParameter meteringProceduralFalloff;

        SerializedDataParameter renderingMode;
        SerializedDataParameter sampleCount;
        SerializedDataParameter animateSamplePositions;
        SerializedDataParameter response;
        SerializedDataParameter clampingEnabled;
        SerializedDataParameter clampingBracket;

        SerializedProperty m_ExposureCurve;

        public override void OnEnable()
        {
            PropertyFetcher<AutoExposureOverride> o = new PropertyFetcher<AutoExposureOverride>(serializedObject);

            mode = Unpack(o.Find(x => x.mode));
            evMin = Unpack(o.Find(x => x.evMin));
            evMax = Unpack(o.Find(x => x.evMax));
            evCompensation = Unpack(o.Find(x => x.evCompensation));
            compensationCurveParameter = Unpack(o.Find(x => x.compensationCurveParameter));
            adaptationMode = Unpack(o.Find(x => x.adaptationMode));
            darkToLightTime = Unpack(o.Find(x => x.darkToLightSpeed));
            lightToDarkTime = Unpack(o.Find(x => x.lightToDarkSpeed));
            meteringMaskMode = Unpack(o.Find(x => x.meteringMaskMode));
            meteringMaskTexture = Unpack(o.Find(x => x.meteringMaskTexture));
            meteringProceduralFalloff = Unpack(o.Find(x => x.meteringProceduralFalloff));
            m_ExposureCurve = o.Find("compensationCurveParameter.m_Value.m_Curve");

            renderingMode = Unpack(o.Find(x => x.renderingMode));
            sampleCount = Unpack(o.Find(x => x.sampleCount));
            animateSamplePositions = Unpack(o.Find(x => x.animateSamplePositions));
            response = Unpack(o.Find(x => x.response));
            clampingEnabled = Unpack(o.Find(x => x.clampingEnabled));
            clampingBracket = Unpack(o.Find(x => x.clampingBracket));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(mode);

            if (mode.value.intValue == (int)AutoExposureMode.On)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Exposure Settings", EditorStyles.boldLabel);
                PropertyField(evMin, new GUIContent("Lower Bound"));
                PropertyField(evMax, new GUIContent("Upper Bound"));
                PropertyField(evCompensation, new GUIContent("Fixed Compensation"));

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                DrawOverrideCheckbox(compensationCurveParameter);
                using (new EditorGUI.DisabledScope(!compensationCurveParameter.overrideState.boolValue))
                {
                    EditorGUILayout.LabelField(
                        new GUIContent(
                            "Compensation Curve",
                            "You can configure this compensation curve to control the exposure compensation at various EV levels. This curve is combined additively with the Fixed Compensation setting. The curve range of [0, 1] is automatically remapped to [Lower Bound, Upper Bound]."
                        )
                    );
                    if (GUILayout.Button("Reset"))
                    {
                        m_ExposureCurve.animationCurveValue = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 0) });
                    }
                    EditorGUILayout.EndHorizontal();

                    using (new EditorGUI.IndentLevelScope(2))
                    {
                        EditorGUILayout.CurveField(m_ExposureCurve, Color.white, new Rect(0, -3, 1, 6), new GUIContent(""), GUILayout.MinHeight(50));
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    var t = target as AutoExposureOverride;

                    if (t == null)
                        return;

                    t.compensationCurveParameter.value.SetDirty();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Adaptation Settings", EditorStyles.boldLabel);
                PropertyField(adaptationMode);

                if (adaptationMode.value.intValue == (int)AutoExposureAdaptationMode.Progressive)
                {
                    PropertyField(darkToLightTime);
                    PropertyField(lightToDarkTime);
                }

                PropertyField(meteringMaskMode);
                if (meteringMaskMode.value.intValue == (int)AutoExposureMeteringMaskMode.Procedural)
                {
                    PropertyField(meteringProceduralFalloff);
                }
                else
                {
                    PropertyField(meteringMaskTexture);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
                PropertyField(renderingMode);
                if (renderingMode.value.intValue == (int)AutoExposureRenderingMode.Fragment)
                {
                    PropertyField(sampleCount);
                    PropertyField(animateSamplePositions);
                    PropertyField(response);
                    PropertyField(clampingEnabled);
                    if (clampingEnabled.value.boolValue)
                    {
                        PropertyField(clampingBracket);
                    }
                }
            }
        }
    }
}
