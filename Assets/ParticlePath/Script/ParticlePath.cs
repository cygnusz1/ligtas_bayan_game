using UnityEngine;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
public class ParticlePath : MonoBehaviour 
{
    //Winspy
    [HideInInspector]
    public bool IsBezier = false;
    [HideInInspector]
    public int BezierSmoothSens = 20;
    [HideInInspector]
    public float CameraWaitTime = 0;
    float cameraWaitTick = 0;

    private List<Vector3> BezierPoints = new List<Vector3>();

    float cameraMoveTick = 0;
    Vector3[] path; 
    Vector3[] vector3s;


    [HideInInspector]
    public bool IsCameraFollow = false;
    [HideInInspector]
    public float CameraSpeed = 10f;


    [HideInInspector]
    public bool IsApprove = false;
    [HideInInspector]
    public bool IsPath = false;
    [HideInInspector]
    public List<Vector3> Waypoints;

    [HideInInspector]
    public bool IsHideInInspector = false;
    [HideInInspector]
    public ParticleSystem PS;
    [HideInInspector]
    public float Speed = 2;

    private ParticleSystem.MainModule _psmm;
    private float _oldSpeed;

    private void Awake()
    {
        if (!PS || Waypoints.Count <= 0)
        {
            IsApprove = false;
            IsPath = false;
            return;
        }

        //创建贝塞尔曲线的路径点（而不是实时计算）
        //if (IsBezier)
            MakeBezierPoints();

        _psmm = PS.main;
        _oldSpeed = Speed;
        if (IsBezier)
            _psmm.startLifetime = BezierPoints.Count * Speed;
        else
            _psmm.startLifetime = Waypoints.Count * Speed;

        _psmm.simulationSpace = ParticleSystemSimulationSpace.Custom;
        _psmm.customSimulationSpace = transform;

        transform.localRotation = Quaternion.identity;

        //Camera.main.transform.position = new Vector3(0, 0, 0);
        //if (IsCameraFollow)
        //    Camera.main.transform.position = transform.position;

        cameraWaitTick = 0;
    }

    private void Update()
    {
        if (IsApprove && IsPath)
        {
            //直线模式
            if (IsBezier == false)
            {
                MoveParticles(Waypoints);

                if (IsCameraFollow)
                    MoveLineCamera();
            }
            else
            {

                MoveParticles(BezierPoints);

                if (IsCameraFollow)
                    MoveBezierCamera();
            }


        }
    }

    void MoveParticles(List<Vector3> movePoints)
    {
        ParticleSystem.Particle[] ps = new ParticleSystem.Particle[PS.particleCount];
        int pCount = PS.GetParticles(ps);

        if (_oldSpeed != Speed)
        {
            _oldSpeed = Speed;
            _psmm.startLifetime = movePoints.Count * Speed;
        }

        for (int i = 0; i < pCount; i++)
        {
            float proportion = (ps[i].startLifetime - ps[i].remainingLifetime) / ps[i].startLifetime;
            int index = Mathf.FloorToInt(proportion * movePoints.Count);
            if (index >= 0 && index < movePoints.Count - 1)
            {
                Vector3 direction = movePoints[index + 1] - movePoints[index];
                ps[i].velocity = direction * (1.0f / Speed) * (1.0f / transform.localScale.x);
            }
            else
            {
                ps[i].remainingLifetime = 0;
            }
        }

        PS.SetParticles(ps, pCount);

    }

    public void ChangeSegmentToLine()
    {
        IsBezier = false;
        _psmm.startLifetime = Waypoints.Count * Speed;
    }

    public void ChangeSegmentToBezier()
    {
        IsBezier = true;
        _psmm.startLifetime = BezierPoints.Count * Speed;
    }

    void MoveBezierCamera()
    {
        if (cameraWaitTick<CameraWaitTime )
        {
            cameraWaitTick += Time.deltaTime;
            return;
        }

        cameraMoveTick = cameraMoveTick + Time.deltaTime / CameraSpeed;

        Vector3 currPt = Interp(vector3s, cameraMoveTick);
            Vector3 nextPt = Interp(vector3s, cameraMoveTick + 0.03f);
            Camera.main.transform.position = currPt;
            Camera.main.transform.LookAt(nextPt);

            if (cameraMoveTick > 1)
                cameraMoveTick = 0;
    }

    void MoveLineCamera()
    {
        if (cameraWaitTick < CameraWaitTime)
        {
            cameraWaitTick += Time.deltaTime;
            return;
        }

        cameraMoveTick = cameraMoveTick + Time.deltaTime / CameraSpeed;

        Vector3 currPt = LineInterp(path, cameraMoveTick);
        Vector3 nextPt = LineInterp(path, cameraMoveTick + 0.03f);
        Camera.main.transform.position = currPt;
        Camera.main.transform.LookAt(nextPt);

        if (cameraMoveTick > 1)
            cameraMoveTick = 0;
    }


    void MakeBezierPoints()
    {
        path = Waypoints.ToArray();
        vector3s = PathControlPointGenerator(path);

        BezierPoints.Clear();

        int SmoothAmount = Waypoints.Count * BezierSmoothSens;
        for (int i = 1; i <= SmoothAmount; i++)
        {
            float pm = (float)i / SmoothAmount;
            //Debug.Log(pm);
            Vector3 currPt = Interp(vector3s, pm);

            BezierPoints.Add(currPt);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    public void SetCameraPosition(float offset)
    {
        cameraMoveTick = offset;
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

    public Vector3 LineInterp(Vector3[] pts, float t)
    {
        int numSections = pts.Length - 1;// - 3;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * (float)numSections), numSections - 1);
        float u = t * (float)numSections - (float)currPt;

        currPt += 1;

        if (currPt > 0)
            return Vector3.Lerp(pts[currPt - 1], pts[currPt], u);
        else
            return Vector3.Lerp(transform.position, pts[currPt], u);

    }
}
