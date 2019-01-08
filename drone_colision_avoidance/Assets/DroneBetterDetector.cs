using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneBetterDetector : MonoBehaviour
{
    /*
        Il s'agit de la classe qui gère le comportement d'un drone qui utilise l'algorithme avoidance shift (par la droite).
        Pour simuler un capteur monopoint type infrarougre/ultrason, on étudie la collision entre un cone et les obstacles (mime les cones de détection).
    */
    public GameObject src; // le point de départ du drone
    public GameObject target; // l'objectif du drone
    public int state; // l'état du drone
    public float speed = 0.05F; // la vitesse max du drone
    public float minH = 2; // la hauteur minimale du drone (utilisé pour le décollage)
    public float minD = 2; // la distance minimale à un obstacle avant que celui ci soit considéré comme dangereux
    private Vector3 direction; // la direction du drone
    private float deltaD = 0; // utilisé lors d'une manoeuvre d'évitement pour mesurer le temps écouler depuis la rencontre d'un obstacle
    public GameObject led; // la led, qui permet de visualiser l'état du drone
    public GameObject cone; // le cone (de détection)

    // lorsque collision avec le cone, une interruptuion est déclanché, mettant à jour ces deux variables
    private bool isTouched = false; // à vrai lorsque collision
    public float touched_dist; // la distance à l'object touché
    

    // Use this for initialization
    void Start()
    {
        // le drone commence en regardant l'objectif
        this.transform.LookAt(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z));
        // l'état initial est 0, le décollage
        setState(0);
    }

    float monopointCaptor(Vector3 position, Vector3 vec, float range)
        // simulation d'un capteur monopoint utilisant un raycast (non utilisé ici)
    {
        RaycastHit hit;
        Ray downRay = new Ray(position, vec.normalized*range);
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

    float realMonopointCaptor()
        // simulation d'un capteur monopoint en utilisant un cône de détection
    {

        if (isTouched)
        {
            return touched_dist;
        }

        return float.PositiveInfinity ;
    }

    void OnTriggerEnter(Collider collision) // fonction appélé lors d'une collision
    {
        isTouched = true;
        touched_dist = Vector3.Distance(this.transform.position, collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
        Debug.DrawRay(transform.position, - this.transform.position + collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position), Color.red, 20, true);
    }

    void OnTriggerStay(Collider collision) // fonction appélé lors d'une collision
    {
        isTouched = true;
        touched_dist = Vector3.Distance(this.transform.position, collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
        Debug.DrawRay(transform.position, -this.transform.position + collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position), Color.red, 20, true);
    }

    private void setState(int s)  // change la couleur de la led lord d'un changement de statut
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
    void Update() // similaire à la fonction update de DroneBehaviour
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
            //Debug.DrawRay(this.transform.position, direction, Color.blue, 20);
            Vector3 norm = direction.normalized;

            if (realMonopointCaptor() < minD )
            {
                //Debug.DrawRay(this.transform.position, direction.normalized * minD, Color.red, 20, true);
                setState(2);
            }
            else if (direction.magnitude < 0.5)
            {
                setState(3);
            }
            else
            {
                //this.transform.Translate(this.transform.forward.normalized*speed, Space.World);
                this.transform.Translate(norm * speed, Space.World);
            }

        }
        else if (state == 2) // maneuvre d'évitement
        {
            if (realMonopointCaptor() < minD)
            {
                
                deltaD = 0;
                this.transform.Translate(this.transform.right.normalized * speed, Space.World);

                deltaD += speed;

            }
            else if (deltaD < 4)
            {
                deltaD += speed;
                this.transform.Translate(this.transform.right.normalized * speed, Space.World);
            }
            else
            {
                setState(4);
                //this.transform.LookAt(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z));
                deltaD = 0;
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
        isTouched = false;

    }
}
