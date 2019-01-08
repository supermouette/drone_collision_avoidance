using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneBehaviour : MonoBehaviour {
    /*
        Il s'agit de la classe qui gère le comportement d'un drone qui utilise l'algorithme avoidance shift (par la droite).
        Pour simuler un capteur monopoint type infrarougre/ultrason, un raycast a été utilisé.
    */
	public GameObject src; // le point de départ du drone
	public GameObject target; // l'objectif du drone
	public int state; // l'état du drone
    public float speed = 30F; // la vitesse max du drone
    public float minH = 2; // la hauteur minimale du drone (utilisé pour le décollage)
    public float minD = 2; // la distance minimale à un obstacle avant que celui ci soit considéré comme dangereux
    private Vector3 direction; // la direction du drone
    private float deltaD=0; // utilisé lors d'une manoeuvre d'évitement pour mesurer le temps écouler depuis la rencontre d'un obstacle
    public GameObject led; // la led, qui permet de visualiser l'état du drone

	// Use this for initialization
	void Start () {
        // le drone commence en regardant l'objectif
        this.transform.LookAt(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z));
        // l'état initial est 0, le décollage
        setState(0);
    }
	
    private void setState(int s)
        // change la couleur de la led lord d'un changement de statut
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
	void Update () {
        Debug.DrawRay(this.transform.position, this.transform.forward.normalized * minD, Color.green, -1, true); // représentation du capteur
        if (state == 0) { // décollage
			if (src.transform.position.y + minH > this.transform.position.y) // tant que le drone est trop bas, il monte
            {
                this.transform.Translate(this.transform.up.normalized * speed);
            }
            else // une fois la hauteur requise atteinte, passage à l'état 1
            {
                setState(1);
            }
		}
        else if (state == 1) // aller vers la cible
        {
            direction = (target.transform.position + new Vector3(0, minH + target.GetComponent<Collider>().bounds.size[1], 0) - this.transform.position);
            //Debug.DrawRay(this.transform.position, direction, Color.blue, 20);
            Vector3 norm = direction.normalized;
            
            if (Physics.Raycast(this.transform.position, this.transform.forward, minD)) // si obstacle, passage à l'état 2, manoeuvre d'esquive
            {
                //Debug.DrawRay(this.transform.position, direction.normalized * minD, Color.red, 20, true);
                setState(2);
            }
            else if (direction.magnitude < 0.5) // si on est arrivé, passage à l'état 3 : attérissage
            {
                setState(3);
            }
            else // sinon, on progresse vers la cible
            {
                //this.transform.Translate(this.transform.forward.normalized*speed, Space.World);
                this.transform.Translate(norm * speed, Space.World);
            }
            
        }
        else if (state == 2) // maneuvre d'évitement
        {
            if (Physics.Raycast(this.transform.position, this.transform.forward, minD)) // si obstacle, décallage vers la droite
            {
                Debug.DrawRay(this.transform.position, this.transform.forward.normalized * minD, Color.red, 20, true);
                deltaD = 0;
                this.transform.Translate(this.transform.right.normalized * speed, Space.World);
                
                deltaD += speed;
                
            }
            else if (deltaD < 4) // si il n'y a plus d'obstacle, on ne peut pas avancer directement : risque de collision sur les cotés du drone.
            {
                deltaD += speed;
                this.transform.Translate(this.transform.right.normalized * speed, Space.World);
            }
            else // fin de la manoeuvre, passage à la phase 4 : rotation en direction de l'objectif
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
        else if (state == 4) // rotation pour regarder l'objectif
        {
            deltaD +=1;

            var targetRotation = Quaternion.LookRotation(new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z) - transform.position);
            //var oldRotattion = transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed/2);

            if (deltaD>60)
            {
                setState(1);
                deltaD = 0;
            }
        }

    }
}
