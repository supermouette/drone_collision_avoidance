using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneBetterDetector : MonoBehaviour
{

    public GameObject src;
    public GameObject target;
    public int state;
    public float speed = 0.05F;
    public float minH = 2;
    public float minD = 2;
    private Vector3 direction;
    private float deltaD = 0;
    public GameObject led;
    public GameObject cone;
    private bool isTouched = false;
    public float touched_dist;
    

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
    {

        if (isTouched)
        {
            return touched_dist;
        }

        return float.PositiveInfinity ;
    }

    void OnTriggerEnter(Collider collision)
    {
        isTouched = true;
        touched_dist = Vector3.Distance(this.transform.position, collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
        Debug.DrawRay(transform.position, - this.transform.position + collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position), Color.red, 20, true);
    }

    void OnTriggerStay(Collider collision)
    {
        isTouched = true;
        touched_dist = Vector3.Distance(this.transform.position, collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
        Debug.DrawRay(transform.position, -this.transform.position + collision.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position), Color.red, 20, true);
        //Debug.Log(touched_dist);
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
