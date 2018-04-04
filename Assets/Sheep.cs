using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sheep : MonoBehaviour {

    public Vector3 simPosition;
    public bool collected;
    Shepherd shepherd;
    Simulation simulation;
    bool simulating;

    // Sheep movement constants
    Vector3 nextLocation;
    float agentSpeed = 5;
    int numberOfNearestNeighbours;
    float shepherdRepulsionDistance = 20;
    float agentRepulsionDistance = 3;
    float agentRepulsionForce = 10;
    float agentAttractionForce = 5f;
    float shepherdRepulsionForce = 5;
    float randomMovementProbability = 0.05f;

    Vector3[] barnVertices; //topleft and botright
    public Sheep[] allSheep;
    bool sheepGrazing;

    // Move the sheep for one step
    public void Move()
    {
        Vector3 nextPosition = simPosition;
        // Sheep idling - grazing
        if (Vector3.Distance(shepherd.simPosition, simPosition) > shepherdRepulsionDistance)
        {
            if (sheepGrazing)
            {
                if (Random.Range(0, 1f) < randomMovementProbability)
                {
                    nextPosition = simPosition + Random.insideUnitSphere;
                    nextPosition.y = 0;
                }
            }
        }
        // Sheep running from predator
        else
        {
            Vector3 shepherdDirection = (simPosition - shepherd.simPosition).normalized * shepherdRepulsionForce;
            Vector3 LCMDirection = (CenterOfSheep(NClosestSheep()) - simPosition).normalized * agentAttractionForce;
            Vector3 TooCloseDirection = TooCloseRepulsion(TooCloseSheep()) * agentRepulsionForce;

            nextPosition = simPosition + (shepherdDirection + LCMDirection + TooCloseDirection).normalized * agentSpeed;
            nextPosition.y = 0;
            //print(mySheep.index + " " + nextPosition); */ 
        }
        // Collect sheep in barn
        if (CheckSheepPosition())
        {
            collected = true;
            //Destroy(sheepObjects[mySheep.index]);
            GetComponent<SpriteRenderer>().enabled = false;
            simulation.sheepLeft--;
            //print("BARNED " + gameObject.name);
        }
        simPosition = nextPosition;
        // Update simulated position if simulating
        if(simulating)
            UpdateSimulatedPosition();
    }

    // Return the centroid of a group of sheep
    public Vector3 CenterOfSheep(Sheep[] mySheep)
    {
        Vector3 center = Vector3.zero;
        foreach (Sheep s in mySheep)
        {
            //print(s.index);
            if (!s.collected)
                center += s.simPosition;
        }

        return center / mySheep.Length;
    }
    
    // Return the closest N sheep - used for grouping sheep
    Sheep[] NClosestSheep()
    {
        Sheep[] sortedSheep = allSheep;
        //Sort by distance
        for (int i = 0; i < sortedSheep.Length; i++)
        {
            for (int j = 0; j < sortedSheep.Length - 1; j++)
            {
                float dstToA = Vector3.Distance(simPosition, sortedSheep[j].simPosition);
                float dstToB = Vector3.Distance(simPosition, sortedSheep[j + 1].simPosition);
                if (dstToA > dstToB)
                {
                    Sheep temp = sortedSheep[j];
                    sortedSheep[j] = sortedSheep[j + 1];
                    sortedSheep[j + 1] = temp;
                }
            }
        }

        //Return n closest
        List<Sheep> NClosest = new List<Sheep>();
        int numSheepCount = 0;
        for (int i = 0; i < sortedSheep.Length; i++)
        {
            if (!sortedSheep[i].collected)
            {
                NClosest.Add(sortedSheep[i]);
                numSheepCount++;
            }
            if (numSheepCount == numberOfNearestNeighbours)
                break;
        }
        return NClosest.ToArray();
    }

    // Return the group of sheep too close to you
    Sheep[] TooCloseSheep()
    {
        List<Sheep> sheepToClose = new List<Sheep>();
        foreach (Sheep s in allSheep)
        {
            if (s != this && Vector3.Distance(s.simPosition, simPosition) < agentRepulsionDistance)
            {
                if (!s.collected)
                    sheepToClose.Add(s);
            }
        }
        return sheepToClose.ToArray();
    }
    
    // Return direction of repulsion because of the sheep too close to you
    Vector3 TooCloseRepulsion(Sheep[] sheepTooClose)
    {
        Vector3 repulsionVector = Vector3.zero;
        foreach (Sheep s in sheepTooClose)
        {
            repulsionVector += (simPosition - s.simPosition) / Vector3.Distance(simPosition, s.simPosition);
        }
        return repulsionVector;
    }

    // Return true if sheep barned
    bool CheckSheepPosition()
    {
        if (collected == true)
            return false;
        if (simPosition.x > barnVertices[0].x && simPosition.x < barnVertices[1].x)
        {
            if (simPosition.z < barnVertices[0].z && simPosition.z > barnVertices[1].z)
            {
                return true;
            }
        }
        return false;
    }

    //Used only for demonstration
    void UpdateSimulatedPosition()
    {
        transform.position = simPosition;
    }

    //Find edges of barn
    void SetBarn()
    {
        barnVertices = new Vector3[2];
        barnVertices[0] = GameObject.Find("Barn").transform.Find("TopLeft").transform.position;
        barnVertices[1] = GameObject.Find("Barn").transform.Find("BotRight").transform.position;
    }

    // Use this for initialization
    void Awake () {
        numberOfNearestNeighbours = allSheep.Length;
        simulation = GameObject.Find("Game Manager").GetComponent<Simulation>();
        shepherd = GameObject.Find("Shepherd").GetComponent<Shepherd>();
        simulating = simulation.simulatingAnimation;
        sheepGrazing = simulation.sheepGrazing;
        SetBarn();
    }
	
	// Update is called once per frame
	void Update () {
        simulating = simulation.simulatingAnimation;
	}
}
