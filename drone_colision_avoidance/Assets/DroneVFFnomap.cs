using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneVFFnomap : MonoBehaviour
{
    // algortihme d'évitement de collisions : VFF
    // capteur : lidar
    public GameObject src; // le point de départ du drone
    public GameObject target; // l'objectif du drone
    public int state; // l'état du drone
    public float speed = 0.05F; // la vitesse max du drone
    public float minH = 1.5f; // la hauteur minimale du drone (utilisé pour le décollage)
    public float minD = 3; // la distance minimale à un obstacle avant que celui ci soit considéré comme dangereux
    private Vector3 direction; // la direction du drone
    private float deltaD = 0; // utilisé lors d'une manoeuvre d'évitement pour mesurer le temps écouler depuis la rencontre d'un obstacle
    public GameObject led; // la led, qui permet de visualiser l'état du drone

    public float coef = 5; // coeficient propre à VFF


    // Use this for initialization
    void Start()
    {
        this.transform.LookAt(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z));
        setState(0);
    }

    float monopointCaptor(Vector3 position, Vector3 vec, float range)// simulation d'un capteur monopoint utilisant un raycast
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

    List<Vector3> lidar(int nbRayon = 64) // simulation d'un capteur de type lidar, en utilisant des raycast, on peu choisir le nombre de rayons
    {
        List<Vector3> pointList = new List<Vector3>();
        for (int i=0; i< nbRayon; i++) // pour chaque rayon
        {
            Vector3 vec = Quaternion.AngleAxis(i*360/ nbRayon, transform.up) * transform.forward;
            vec.Normalize();
            RaycastHit hit;
            Vector3 centerLidar = transform.position + transform.up * 0.33f; // le centre se trouve au dessus du drone (sufisament haut pour dépasser la led, mais le plus bas possible)
            Ray downRay = new Ray(centerLidar, vec * minD);
            if (Physics.Raycast(downRay, out hit)) // si détection
            {
                if (hit.distance < minD)
                {
                    Debug.DrawRay(centerLidar, vec * minD, Color.red, 20, true);
                    pointList.Add(hit.point);
                }
                else // utile, car il peut y avoir un hit, mais plus loin que minD
                {
                    Debug.DrawRay(centerLidar, vec * minD, Color.green, -1, true);
                }
            }
            else
            {
                Debug.DrawRay(centerLidar, vec * minD, Color.green, -1, true);
            }
        }
        return pointList; // on retourne le nuage de points détecté
    }

    private void setState(int s) // change la couleur de la led à chaque changement d'état
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

    }
}
