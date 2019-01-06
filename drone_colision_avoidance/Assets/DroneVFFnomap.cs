using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneVFFnomap : MonoBehaviour
{

    public GameObject src;
    public GameObject target;
    public int state;
    public float speed = 0.05F;
    public float minH = 1.5f;
    public float minD = 3;
    private Vector3 direction;
    private float deltaD = 0;
    public GameObject led;
    public float coef = 5;


    // Use this for initialization
    void Start()
    {
        this.transform.LookAt(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z));
        //this.transform.Rotate(0, 90, 0);
        setState(0);
    }

    float monopointCaptor(Vector3 position, Vector3 vec, float range)
    {
        RaycastHit hit;
        Ray downRay = new Ray(position, vec.normalized * range);
        if (Physics.Raycast(downRay, out hit))
        {
            Debug.DrawRay(position, vec.normalized * range, Color.red, 20, true);
        }
        else
        {
            Debug.DrawRay(position, vec.normalized * range, Color.green, -1, true);
        }

        return hit.distance;
    }

    List<Vector3> lidar(int nbRayon = 64)
    {
        List<Vector3> pointList = new List<Vector3>();
        for (int i=0; i< nbRayon; i++)
        {
            Vector3 vec = Quaternion.AngleAxis(i*360/ nbRayon, transform.up) * transform.forward;
            vec.Normalize();
            RaycastHit hit;
            Vector3 centerLidar = transform.position + transform.up * 0.33f; // le centre se trouve au dessus du lidar (sufisament haut pour dépasser la led, mais le plus bas possible)
            Ray downRay = new Ray(centerLidar, vec * minD);
            if (Physics.Raycast(downRay, out hit))
            {
                if (hit.distance < minD)
                {
                    Debug.DrawRay(centerLidar, vec * minD, Color.red, 20, true);
                    pointList.Add(hit.point);
                }
                else
                {
                    Debug.DrawRay(centerLidar, vec * minD, Color.green, -1, true);
                }
            }
            else
            {
                Debug.DrawRay(centerLidar, vec * minD, Color.green, -1, true);
            }
        }
        return pointList;
    }

    private void setState(int s)
    {
        Debug.Log(state + " -> " + s);
        state = s;
        MeshRenderer meshRenderer = led.GetComponent<MeshRenderer>();
        switch (s)
        {
            case 0:
                meshRenderer.material = Resources.Load("start", typeof(Material)) as Material;
                break;
            case 1:
                meshRenderer.material = Resources.Load("state_ok", typeof(Material)) as Material;
                break;
            case 2:
                meshRenderer.material = Resources.Load("avoidance", typeof(Material)) as Material;
                break;
            case 3:
                meshRenderer.material = Resources.Load("end", typeof(Material)) as Material;
                break;
            case 4:
                meshRenderer.material = Resources.Load("orientation", typeof(Material)) as Material;
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawRay(this.transform.position, this.transform.forward.normalized * minD, Color.green, -1, true);
        
        if (state == 0)
        { // décollage
            if (src.transform.position.y + minH > this.transform.position.y)
            {
                this.transform.Translate(this.transform.up.normalized * speed);
            }
            else
            {
                setState(1);
            }
        }
        else if (state == 1) // aller vers la cible
        {
            direction = (target.transform.position + new Vector3(0, minH + target.GetComponent<Collider>().bounds.size[1], 0) - this.transform.position);
            
            if (direction.magnitude < 0.5) // si le drone est arrivé
            {
                setState(3);
            }
            else
            {
                List<Vector3> points = lidar();
                Vector3 dir_obstacle;
                for (int i = 0; i < points.Count; i++)
                {
                    dir_obstacle = transform.position - points[i];
                    direction += coef * dir_obstacle / (dir_obstacle.magnitude * dir_obstacle.magnitude);
                }
                this.transform.Translate(direction.normalized * speed, Space.World);
            }

        }
        else if (state == 3) // attérissage
        {
            if (this.transform.position.y > target.transform.position.y + target.GetComponent<Collider>().bounds.size[1])
            {
                this.transform.Translate(-this.transform.up.normalized * speed);
            }
            else
            {
                Debug.Log("succès");
                setState(5);
            }
        }
        else if (state == 4)
        {
            deltaD += 1;
            //Vector2 f = new Vector2(this.transform.right[0], this.transform.right[2]);
            //Vector2 d = new Vector2(direction[0], direction[2]);
            //float angle = Vector2.Angle(f, d);
            //Debug.Log(angle);
            var targetRotation = Quaternion.LookRotation(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z) - transform.position);
            //var oldRotattion = transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed / 2);

            if (deltaD > 60)
            {
                setState(1);
                deltaD = 0;
            }
        }

    }
}
