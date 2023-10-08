using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerController : Agent
{
    Rigidbody rb;

    [SerializeField]
    public bool inHumanControl;

    GameObject goal, trainingArea, environment;
    Vector3 startingPosition;
    float lastDist;

    float move = 0;
    float turn = 0;

    Stats stats;

    private float moveForce = 10.0f;
    private float turnForce = 0.01f;

    private float deathReward = 20.0f;
    private float goalReward = 12.0f;
    private float rCloser = 0.02f;
    private float rFurther = 0.05f;

    RaycastHit hit;
    LayerMask ground;

    int episodeNum = 0;

    void Start()
    {
        startingPosition = transform.localPosition;
        rb = GetComponent<Rigidbody>();

        stats = GameObject.FindGameObjectWithTag("Stats").GetComponent<Stats>();

        trainingArea = transform.parent.gameObject;
        if (trainingArea.name != "TrainingArea") trainingArea = trainingArea.transform.parent.gameObject;

        environment = trainingArea.transform.Find("Environment").gameObject;

        goal = trainingArea.transform.Find("Goal").gameObject;
        //idk if this is right
        //camera = trainingArea.transform.Find("PCam").gameObject;


        //ground = LayerMask.GetMask("Ground");
    }


    public Vector3 RandomizeGoalPosition(Vector3 pos)
    {
        pos.z = Random.Range(-29.0f, 29.0f);
        pos.x = Random.Range(-29.0f, 29.0f);
        return pos;
    }

    public void RandomizeGoals()
    {
        if (goal != null) goal.transform.localPosition = RandomizeGoalPosition(goal.transform.localPosition);
        
    }

    public override void OnEpisodeBegin()
    {
        episodeNum = stats.StartEpisode();

        transform.localPosition = startingPosition;
        lastDist = Mathf.Abs(goal.transform.localPosition.z - transform.localPosition.z);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        RandomizeGoals();

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (inHumanControl) return;
        
        var dir = goal.transform.localPosition - transform.localPosition;
        var angle = Vector3.Angle(transform.forward, dir);
        sensor.AddObservation(angle);

        var dist = Vector3.Distance(goal.transform.localPosition, transform.localPosition);
        sensor.AddObservation(dist);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (inHumanControl) return;

        move = actions.ContinuousActions[0];

        turn = actions.ContinuousActions[1];

       // Debug.Log(move + " " + turn);

    }

    void DoingTheThing()
    {
        
        rb.AddForce(0, 0, move * moveForce, ForceMode.Force);
        rb.AddTorque(0, turn * turnForce, 0, ForceMode.Force);

        var curdist = Mathf.Abs(goal.transform.localPosition.z - transform.localPosition.z);
        if (curdist < lastDist)
        {
            SetReward(rCloser);
        }
        else
        {
            SetReward(-rFurther);
        }

        lastDist = curdist;
    }

   
    
    void Update() {
        if (inHumanControl) {
            move = 0;

            if (Input.GetKey(KeyCode.W)){
                move = 1;
            }
            else if (Input.GetKey(KeyCode.S)){
                move = 2;
            }

            else if (Input.GetKey(KeyCode.A)){
                move = 3;
            }

            else if (Input.GetKey(KeyCode.D)){
                move = 4;
            }

        }
    }

    void FixedUpdate() {

        DoingTheThing();

        //not ready yet 
        //if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out hit, Mathf.Infinity, ground) || Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out hit, Mathf.Infinity, ground))
        //{
        //    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green);
        //    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green);

        //    //Debug.Log("<color=#ffffff>Did Hit</color>");

        //    //SetReward(0.001f);
        //}


        //else
        //{
        //    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 1000, Color.red);
        //    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 1000, Color.red);

        //    //Debug.Log("<color=#a86232>Did not Hit</color>");

        //    //SetReward(-0.03f);
        //}

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Goal"))
        {
            Debug.Log("<color=#00ff00>GOALLLLL</color>");
            SetReward(goalReward);
            EndEpisode();

            stats.AddGoal(episodeNum, 1);
        }

        else if (collision.gameObject.CompareTag("Death"))
        {
            Debug.Log("<color=#ff0000>OH NOOOO</color>");
            SetReward(-deathReward);
            EndEpisode();

            stats.AddGoal(episodeNum, 0);
        }

    }

    void OnTriggerEnter(Collider other)
    {
       

    }
}
