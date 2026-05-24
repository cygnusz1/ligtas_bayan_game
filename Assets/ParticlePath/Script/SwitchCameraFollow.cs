using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCameraFollow : MonoBehaviour {

    public ParticlePath particlePath;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 180, 30), "View Particle"))
        {
            particlePath.IsCameraFollow = false;
            Camera.main.transform.position = new Vector3(-36.4f, 34.1f, 8.5f);

            Camera.main.transform.rotation = Quaternion.Euler(35f, -264f, 0f);
        }

        if (GUI.Button(new Rect(0,40,180,30),"Follow Particle"))
        {
            particlePath.IsCameraFollow = true;
        }

        if (GUI.Button(new Rect(0, 80, 180, 30), "Follow and move to half"))
        {
            particlePath.IsCameraFollow = true;
            particlePath.SetCameraPosition(0.5f);
        }


        if (GUI.Button(new Rect(Screen.width - 200, 0, 150, 30), "Change To Line"))
        {
            particlePath.ChangeSegmentToLine();
            particlePath.Speed = 5;
            particlePath.CameraSpeed = 40;
        }

        if (GUI.Button(new Rect(Screen.width - 200, 40, 150, 30), "Change To Bezier"))
        {
            particlePath.ChangeSegmentToBezier();
            particlePath.Speed = 0.55f;
            particlePath.CameraSpeed = 30;
        }

    }
}
