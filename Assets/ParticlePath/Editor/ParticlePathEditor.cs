using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(ParticleSystem))]
public class ParticlePathEditor : Editor
{
    private ParticleSystem _particleSystem;
    private Assembly _assembly;
    private Type _particleSystemInspector;
    private MethodInfo _onInspectorGUI;
    private Editor _particleSystemEditor;
    public ParticlePath _particlePath;
    private int _currentCheckedPoint;

    private GUIStyle disableStyle;

    private void OnEnable()
    {
        _particleSystem = target as ParticleSystem;
        //载入程序集
        _assembly = Assembly.GetAssembly(typeof(Editor));
        //获取ParticleSystemInspector类
        _particleSystemInspector = _assembly.GetTypes().Where(t => t.Name == "ParticleSystemInspector").FirstOrDefault();
        //获取OnInspectorGUI方法
        _onInspectorGUI = _particleSystemInspector.GetMethod("OnInspectorGUI", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        //创建ParticleSystemInspector的实例
        _particleSystemEditor = CreateEditor(target, _particleSystemInspector);
        _particlePath = _particleSystem.gameObject.GetComponent<ParticlePath>();
        _currentCheckedPoint = -1;

        if (!_particlePath)
        {
            _particlePath = _particleSystem.gameObject.AddComponent<ParticlePath>();
            _particlePath.IsApprove = true;
            _particlePath.IsBezier = false;
            _particlePath.hideFlags = HideFlags.HideInInspector;
            _particlePath.Waypoints = new List<Vector3>();
            _particlePath.IsHideInInspector = false;
            _particlePath.PS = _particleSystem;
        }
        if (!_particlePath.IsApprove)
        {
            DestroyImmediate(_particlePath);
            _particlePath = _particleSystem.gameObject.AddComponent<ParticlePath>();
            _particlePath.IsApprove = true;
            _particlePath.IsBezier = false;
            _particlePath.hideFlags = HideFlags.HideInInspector;
            _particlePath.Waypoints = new List<Vector3>();
            _particlePath.IsHideInInspector = false;
            _particlePath.PS = _particleSystem;
        }

    }

    public override void OnInspectorGUI()
    {
        bool isChange = false;

        if (disableStyle == null)
        {
            foreach (GUIStyle style in GUI.skin.customStyles)
            {
                if (style.name == "toolbarbutton")
                {
                    disableStyle = new GUIStyle(style);
                    disableStyle.normal.textColor = Color.gray;
                    break;
                }
            }
        }


        EditorGUILayout.BeginVertical("HelpBox");

        EditorGUILayout.BeginHorizontal();
        GUI.color = _particlePath.IsPath ? Color.white : Color.gray;
        _particlePath.IsPath = GUILayout.Toggle(_particlePath.IsPath, "", GUILayout.Width(25));
        if (GUILayout.Button("Path Mode", "label"))
        {
            _particlePath.IsHideInInspector = !_particlePath.IsHideInInspector;
        }
        GUI.enabled = _particlePath.IsPath;
        EditorGUILayout.EndHorizontal();

        if (!_particlePath.IsHideInInspector)
        {
            for (int i = 0; i < _particlePath.Waypoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = _currentCheckedPoint == i ? Color.cyan : Color.white;
                if (GUILayout.Button("Path " + (i + 1), "toolbarbutton"))
                {
                    _currentCheckedPoint = i;
                }

                Vector3 pos = _particlePath.Waypoints[i];

                pos.x = Convert.ToSingle(GUILayout.TextField(pos.x.ToString(), GUILayout.Width(50)));
                pos.y = Convert.ToSingle(GUILayout.TextField(pos.y.ToString(), GUILayout.Width(50)));
                pos.z = Convert.ToSingle(GUILayout.TextField(pos.z.ToString(), GUILayout.Width(50)));

                if (pos.x != _particlePath.Waypoints[i].x ||
                    pos.y != _particlePath.Waypoints[i].y ||
                    pos.z != _particlePath.Waypoints[i].z)
                {
                    _particlePath.Waypoints[i] = pos;
                }

                if (i != 0)
                {
                    if (GUILayout.Button("↑", "toolbarbutton", GUILayout.Width(20)))
                    {
                        Vector3 vec = _particlePath.Waypoints[i];
                        _particlePath.Waypoints[i] = _particlePath.Waypoints[i - 1];
                        _particlePath.Waypoints[i - 1] = vec;
                    }
                }
                else
                {
                    GUILayout.Box("↑", disableStyle, GUILayout.Width(20) );
                }
                if (i != _particlePath.Waypoints.Count - 1)
                {
                    if (GUILayout.Button("↓", "toolbarbutton", GUILayout.Width(20)))
                    {
                        Vector3 vec = _particlePath.Waypoints[i];
                        _particlePath.Waypoints[i] = _particlePath.Waypoints[i + 1];
                        _particlePath.Waypoints[i + 1] = vec;
                    }
                }
                else
                {
                    GUILayout.Box("↓", disableStyle, GUILayout.Width(20));
                }

                if (GUILayout.Button("-", "toolbarbutton", GUILayout.Width(20)))
                {
                    _particlePath.Waypoints.RemoveAt(i);
                    _currentCheckedPoint = -1;
                    break;
                }


                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("", "OL Plus"))
            {
                if (_currentCheckedPoint != -1)
                {
                    _particlePath.Waypoints.Add(_particlePath.Waypoints[_currentCheckedPoint]);
                }
                else
                {
                    _particlePath.Waypoints.Add(_particlePath.transform.position);
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Line segment:");
            _particlePath.IsBezier = !GUILayout.Toggle(!_particlePath.IsBezier, "Line");
            _particlePath.IsBezier = GUILayout.Toggle(_particlePath.IsBezier, "Bezier");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Move speed:");
            _particlePath.Speed = EditorGUILayout.Slider(_particlePath.Speed, 10f, 0.05f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Camera follow:");
            _particlePath.IsCameraFollow = GUILayout.Toggle(_particlePath.IsCameraFollow, "Start camera follow");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Camera speed:");
            _particlePath.CameraSpeed= EditorGUILayout.Slider(_particlePath.CameraSpeed, 100f, 0.05f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Camera wait time(second):");
            _particlePath.CameraWaitTime = EditorGUILayout.Slider(_particlePath.CameraWaitTime, 60f, 0f);
            EditorGUILayout.EndHorizontal();
        }



        EditorGUILayout.EndVertical();

        GUI.color = Color.white;
        GUI.enabled = true;

        _onInspectorGUI.Invoke(_particleSystemEditor, null);

#if UNITY_EDITOR
        if (GUI.changed)
        {
            //EditorUtility.SetDirty(_particlePath);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
#endif
    }

    private void OnSceneGUI()
    {
        if (_particlePath.IsPath)
        {
            Handles.color = Color.cyan;

            //直线模式
            if (_particlePath.IsBezier == false)
            {
                for (int i = 0; i < _particlePath.Waypoints.Count; i++)
                {
                    if (i < _particlePath.Waypoints.Count - 1)
                    {
                        Handles.DrawLine(_particlePath.Waypoints[i], _particlePath.Waypoints[i + 1]);
                    }
                }
            }

            //贝塞尔模式
            if (_particlePath.IsBezier)
            {
                Vector3[] path = _particlePath.Waypoints.ToArray();

                Vector3[] vector3s = PathControlPointGenerator(path);
                Vector3 prevPt = Interp(vector3s, 0);

                int SmoothAmount = _particlePath.Waypoints.Count * _particlePath.BezierSmoothSens;
                for (int i = 1; i <= SmoothAmount; i++)
                {
                    float pm = (float)i / SmoothAmount;
                    //Debug.Log(pm);
                    Vector3 currPt = Interp(vector3s, pm);

                    Handles.DrawLine(currPt, prevPt);

                    prevPt = currPt;
                }
            }

            if (_currentCheckedPoint != -1 && _currentCheckedPoint < _particlePath.Waypoints.Count)
            {
                Tools.current = Tool.None;
                Vector3 oldVec = _particlePath.Waypoints[_currentCheckedPoint];
                Vector3 newVec = Handles.PositionHandle(oldVec, Quaternion.identity);
                if (oldVec != newVec)
                {
                    _particlePath.Waypoints[_currentCheckedPoint] = newVec;
                }
                Handles.Label(newVec, "Path " + (_currentCheckedPoint + 1));
                EditorUtility.SetDirty(_particlePath);

            }
        }
    }


    //贝塞尔曲线函数
    public Vector3[] PathControlPointGenerator(Vector3[] path)
    {
        Vector3[] suppliedPath;
        Vector3[] vector3s;

        //create and store path points:
        suppliedPath = path;

        //populate calculate path;
        int offset = 2;
        vector3s = new Vector3[suppliedPath.Length + offset];
        Array.Copy(suppliedPath, 0, vector3s, 1, suppliedPath.Length);

        //populate start and end control points:
        //vector3s[0] = vector3s[1] - vector3s[2];
        vector3s[0] = vector3s[1] + (vector3s[1] - vector3s[2]);
        vector3s[vector3s.Length - 1] = vector3s[vector3s.Length - 2] + (vector3s[vector3s.Length - 2] - vector3s[vector3s.Length - 3]);

        //is this a closed, continuous loop? yes? well then so let's make a continuous Catmull-Rom spline!
        if (vector3s[1] == vector3s[vector3s.Length - 2])
        {
            Vector3[] tmpLoopSpline = new Vector3[vector3s.Length];
            Array.Copy(vector3s, tmpLoopSpline, vector3s.Length);
            tmpLoopSpline[0] = tmpLoopSpline[tmpLoopSpline.Length - 3];
            tmpLoopSpline[tmpLoopSpline.Length - 1] = tmpLoopSpline[2];
            vector3s = new Vector3[tmpLoopSpline.Length];
            Array.Copy(tmpLoopSpline, vector3s, tmpLoopSpline.Length);
        }
        return (vector3s);
    }


    public Vector3 Interp(Vector3[] pts, float t)
    {
        int numSections = pts.Length - 3;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
        float u = t * (float)numSections - (float)currPt;

        Vector3 a = pts[currPt];
        Vector3 b = pts[currPt + 1];
        Vector3 c = pts[currPt + 2];
        Vector3 d = pts[currPt + 3];

        return .5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u)
            + (2f * a - 5f * b + 4f * c - d) * (u * u)
            + (-a + c) * u
            + 2f * b
        );
    }
}
