using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEditor.Build.Reporting;

public class comboBuild
{
    //This creates a menu item to trigger the dual builds https://docs.unity3d.com/ScriptReference/MenuItem.html 

    [MenuItem("Tools/Game Build/Dual Build")]
    public static void BuildGame()
    {
        //This builds the player twice: a build with desktop-specific texture settings (WebGL_Build)
        //as well as mobile-specific texture settings (WebGL_Mobile),
        //and combines the necessary files into one directory (WebGL_Build)

        string dualBuildPath = "WebGLBuilds";
        string desktopBuildName = "WebGL_Build";
        string mobileBuildName = "WebGL_Mobile";

        string desktopPath = Path.Combine(dualBuildPath, desktopBuildName);
        string mobilePath = Path.Combine(dualBuildPath, mobileBuildName);
        string[] scenes = new string[] { "Assets/scene.unity" };

        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
        BuildPipeline.BuildPlayer(scenes, desktopPath, BuildTarget.WebGL, BuildOptions.Development);

        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
        BuildPipeline.BuildPlayer(scenes, mobilePath, BuildTarget.WebGL, BuildOptions.Development);

        // Copy the mobile.data file to the desktop build directory to consolidate them both
        FileUtil.CopyFileOrDirectory(Path.Combine(mobilePath, "Build", mobileBuildName + ".data"), Path.Combine(desktopPath, "Build", mobileBuildName + ".data"));
    }
}