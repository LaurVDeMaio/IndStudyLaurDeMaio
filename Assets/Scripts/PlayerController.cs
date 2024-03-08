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
    GameObject[] obstacles;
    public int obsCount = 1;
    public GameObject obstacle;

    Vector3 startingPosition;
    float lastDist;

    float move = 0;
    float turn = 0;

    Stats stats;

    private float moveForce = 20.0f;
    private float turnForce = 0.01f;

    private float deathReward = -100.0f;
    private float goalReward = 20.0f;
    private float rCloser = 0.05f;
    //private float rFurther = 4.00f;

    RaycastHit hit;
    LayerMask collisions;
    
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



        obstacles = new GameObject[obsCount];
        for (int i = 0; i < obsCount; i++)
        {
            var pos = new Vector3(0, 10000, 0);
            obstacles[i] = Instantiate(obstacle, pos, Quaternion.identity, environment.transform);
        }

    }
    public Vector3 FindSafePos(Vector3 offset, float y)
    {
        while (true)
        {
            Vector3 pos = new Vector3(Random.Range(-29.0f, 29.0f), 0.5f, Random.Range(-29.0f, 29.0f));
            var colliders = Physics.OverlapBox(pos + offset, new Vector3(3.0f, 0.25f, 3.0f));
            if (colliders.Length == 0) return new Vector3(pos.x,y, pos.z);
        }
    }



    public void RandomizeGoals()
    {
        if (goal != null) goal.transform.localPosition = FindSafePos(trainingArea.transform.position, 0.74f);
        
    }

    public void RandomizePlayers()
    {
        if (rb != null) rb.transform.localPosition = FindSafePos(trainingArea.transform.position, 1.0f);

    }


    public void RandomizeObstaclesPosition()
    {
        //randomize position of each obstacle

        for(int i = 0; i < obsCount; i++)
        {
            obstacles[i].transform.localPosition = FindSafePos(trainingArea.transform.position, 0f);
        }
    }


   
    public override void OnEpisodeBegin()
    {
        episodeNum = stats.StartEpisode();

        transform.localPosition = startingPosition;
        lastDist = Vector3.Distance(goal.transform.localPosition, transform.localPosition);
        var dir = goal.transform.localPosition - transform.localPosition;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        RandomizeGoals();
        RandomizePlayers();

        //instantiate a random number of obstacles
        RandomizeObstaclesPosition();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (inHumanControl) return;

        sensor.AddObservation(transform.localPosition);
    
        var dist = Vector3.Distance(goal.transform.localPosition, transform.localPosition);
        sensor.AddObservation(dist);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (inHumanControl) return;

        move = actions.ContinuousActions[0];
        turn = actions.ContinuousActions[1];

    }

    void DoingTheThing()
    {
        rb.AddForce(transform.forward * move * moveForce, ForceMode.Force);
        rb.AddTorque(0, turn * turnForce, 0, ForceMode.Force);

        var curdist = Vector3.Distance(goal.transform.localPosition, transform.localPosition);
        if (curdist < lastDist)
        {
            SetReward(rCloser);
        }
        else
        {
            //SetReward(-rFurther);
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
            SetReward(deathReward);
            EndEpisode();

            stats.AddGoal(episodeNum, 0);
        }

    }

}
