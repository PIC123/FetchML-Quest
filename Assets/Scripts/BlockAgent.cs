using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System;
using Random = UnityEngine.Random;

public class BlockAgent : Agent
{
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        Person = GameObject.FindGameObjectWithTag("Person");
        InitialStickPosition = Stick.transform.position;
        InitialAgentPosition = this.transform.position;
        StickRBody = Stick.GetComponent<Rigidbody>();
    }

    public GameObject Stick;
    public GameObject ground;
    public Rigidbody StickRBody;
    public Rigidbody rBody;
    public float ThrowForce = 2f;

    private GameObject Person;
    private GameObject Target;
    private Transform Arena;
    private Vector3 InitialStickPosition;
    private Vector3 InitialAgentPosition;
    private bool HasStick;
    private bool isWaitingForThrow;
    private float prevDist;
    public override void OnEpisodeBegin()
    {
        if (this.transform.localPosition.y < 0)
        {
            // If the Agent fell, zero its momentum
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
            this.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

        // Move the target to a new spot
        //Stick.transform.localPosition = new Vector3(Random.value * 8 - 4,
        //                                   0.5f,
        //                                   Random.value * 8 - 4);
        //Stick.transform.rotation = new Quaternion(0, 0, 0, 0);

        // Move the 2nd target to a new spot
        //Person.transform.localPosition = new Vector3(Random.value * 8 - 4,
        //                                   0.5f,
        //                                   Random.value * 8 - 4);
        Target = Stick;
        Arena = this.transform.parent;
        HasStick = false;
        isWaitingForThrow = true;
        prevDist = Mathf.Infinity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Target.transform.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    public float forceMultiplier = 10;
    public float speed = 10;
    public float rotSpeed = 1800;
    public override void OnActionReceived(float[] vectorAction)
    {
        if (!isWaitingForThrow)
        {
            // Actions, size = 2
            //Vector3 controlSignal = Vector3.zero;
            //controlSignal.x = vectorAction[0];
            //controlSignal.z = vectorAction[1];
            //rBody.AddForce(controlSignal * forceMultiplier);
            //MoveAgent(vectorAction);
            float h = vectorAction[0] * speed * Time.fixedDeltaTime;//((vectorAction[0] + 1f)/2f) * speed * Time.fixedDeltaTime;
            float v = vectorAction[1] * speed * Time.fixedDeltaTime;
            //float rot = vectorAction[2] * rotSpeed;
            transform.Translate(h, 0, v);
            //transform.Rotate(0, rot, 0);

            // Rewards
            float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.transform.localPosition);
            //AddReward(-0.01f);

            // Reached target
            if (distanceToTarget < 1f)
            {
                if (Target.tag != "Person")
                {
                    Debug.Log("Got Stick");
                    SetReward(0.5f);
                    Target = Person;
                    //Stick.GetComponent<Collider>().isTrigger = true;
                    Stick.GetComponent<Rigidbody>().isKinematic = true;
                    Stick.transform.Translate(this.transform.up * 1f);
                    Stick.transform.SetParent(this.transform);
                    HasStick = true;
                }
                else
                {
                    Debug.Log("Found Person");
                    SetReward(1f);
                    Stick.transform.SetParent(Arena);
                    Stick.GetComponent<Rigidbody>().isKinematic = false;
                    //Stick.GetComponent<Collider>().isTrigger = false;
                    EndEpisode();
                    //Target = Stick;
                }
            }

            if (HasStick)
            {
                if (distanceToTarget < prevDist)
                {
                    prevDist = distanceToTarget;
                    AddReward(0.01f);
                    Debug.Log("Getting Closer");
                }
            }

            // Fell off platform
            if (this.transform.localPosition.y < 0)
            {
                Debug.Log("Fell Off");
                SetReward(-1.0f);
                EndEpisode();
            }
        } else
        {
            Debug.Log("Idling");
        }
    }

    //public override void Heuristic(float[] actionsOut)
    //{
    //    actionsOut[0] = Input.GetAxis("Horizontal");
    //    actionsOut[1] = Input.GetAxis("Vertical");
    //}
    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = 0;
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            actionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            actionsOut[0] = 2;
        }
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = Mathf.FloorToInt(act[0]);

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        rBody.AddForce(dirToGo * 3f,
            ForceMode.VelocityChange);
    }

    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = 0;
        groundCollider.material.staticFriction = 0;
    }

    public void SetStickAsTarget()
    {
        Target = Stick;
        isWaitingForThrow = false;
        StickRBody.AddForce(Stick.transform.eulerAngles * ThrowForce);
    }

    public void ResetStick()
    {
        Stick.transform.position = InitialStickPosition;
        StickRBody.angularVelocity = Vector3.zero;
        StickRBody.velocity = Vector3.zero;
    }

    public void ResetScene()
    {
        ResetStick();
        this.transform.position = InitialAgentPosition;
        rBody.angularVelocity = Vector3.zero;
        rBody.velocity = Vector3.zero;
        EndEpisode();
    }

    public void RandomThrow()
    {
        Stick.transform.localPosition = new Vector3(Random.value * 8 - 4,
                                           0.5f,
                                           Random.value * 8 - 4);
        Stick.transform.rotation = new Quaternion(0, 0, 0, 0);
        isWaitingForThrow = false;
        Target = Stick;
    }
}
