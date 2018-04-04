using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Shepherd : MonoBehaviour {

    public Vector3 simPosition;
    public float rotation;

    bool simulating = true;
    Simulation simulation;
    GameObject barn;
    public int discretizationAngle = 15;
    int fieldOfView = 360;

    public float shepherdSpeed;
    public int angleTurn = 15;

    Sheep[] allSheep;
    public int[] sight;

    public int[] markovMovementOutput;
    public MarkovDNA dna;
    List<LogicGate> brain;

    public float totalMovement;
    public enum SightType {Normal, Distance, FurthestOnly};
    public SightType sightType = SightType.Normal;
    public LogicGate.LogicType logicType;
    public enum MovementType { Analog, Discrete };
    public MovementType movementType = MovementType.Discrete;


    // Needed because -1 % 5 = -1 instead of -1 % 5 = 1
    int Modulo(int x, int y)
    {
        return (Mathf.Abs(x) % y);
    }

    // Return array of 0s and 1s whether the shepherd sees anything in the respective circle wedge
    int[] GetSight(SightType _sightType)
    {
        allSheep = simulation.sheep;
        int[] sight = new int[2*360 / discretizationAngle];

        //First half is data for sheep
        switch (_sightType)
        {
            // Get position in 0s and 1s
            case SightType.Normal:
                foreach (Sheep s in allSheep)
                {
                    int sightIndex = Modulo((int)(rotation + 180 + (int) Mathf.Rad2Deg * Mathf.Atan2((s.simPosition.z - simPosition.z), (s.simPosition.x - simPosition.x))), 360) / discretizationAngle;
                    sight[sightIndex] = 1;
                }
                break;

            // Get position with distance instead of just 0s and 1s
            case SightType.Distance:
                foreach (Sheep s in allSheep)
                {
                    float furthestSheepInWedge = sight[(int)((180 + Mathf.Rad2Deg * Mathf.Atan2((s.simPosition.z - simPosition.z), (s.simPosition.x - simPosition.x))) / discretizationAngle)];
                    if (Vector3.Distance(s.simPosition, simPosition) > furthestSheepInWedge){
                        sight[(int)((180 + Mathf.Rad2Deg * Mathf.Atan2((s.simPosition.z - simPosition.z), (s.simPosition.x - simPosition.x))) / discretizationAngle)] = Mathf.RoundToInt(Vector3.Distance(s.simPosition, simPosition));
                    }
                }
                break;

            // Get distance and position of the furthest sheep from the centroid only
            case SightType.FurthestOnly:
                Vector3 centroid = allSheep[0].CenterOfSheep(allSheep);
                Sheep furthest = null;
                float furthestDistance = 0;
                foreach (Sheep s in allSheep)
                {
                    if(Vector3.Distance(s.simPosition, simPosition) > furthestDistance)
                    {
                        furthestDistance = Vector3.Distance(s.simPosition, centroid);
                        furthest = s;
                    }
                }
                sight[(int)((180 + Mathf.Rad2Deg * Mathf.Atan2((furthest.simPosition.z - simPosition.z), (furthest.simPosition.x - simPosition.x))) / discretizationAngle)] = Mathf.RoundToInt(furthestDistance);
                break;
        }

        //Second half is data for barn
        float angle = Modulo((int)(rotation + 180 + (int)Mathf.Rad2Deg * Mathf.Atan2((barn.transform.position.z - simPosition.z), (barn.transform.position.x - simPosition.x))), 360);
        int index = Mathf.Clamp(360 / discretizationAngle + (int)(angle / discretizationAngle), 0, sight.Length-1);
        sight[index] = 1;
        return sight;
    }

    // Move according to the MarkovBrain rules
    public void Move()
    {
        sight = GetSight(sightType);
        Vector3 nextPosition = MarkovNextPosition();
        nextPosition.y = 0;
        totalMovement += Vector3.Magnitude(nextPosition - simPosition);
        simPosition = nextPosition;
    }

    // Calculate next position according to the MarkovBrain
    public Vector3 MarkovNextPosition()
    {
        markovMovementOutput = new int[2];

        // Interpret the nodes of the MarkovBrain
        foreach(LogicGate lg in brain)
        {
            int[] output = lg.GetOutputNodes();
            //print(string.Join(",", new List<int>(output).ConvertAll(i => i.ToString()).ToArray()));
            //print(lg.ToString());

            for (int i = 0; i < sight.Length; i++)
            {
                sight[i] = output[i];
            }
            for (int i = 0; i < markovMovementOutput.Length; i++)
            {
                markovMovementOutput[i] = output[i + sight.Length];
            }
        }

        // Move the shepherd
        int movementIndex = markovMovementOutput[0] + 2 * markovMovementOutput[1];
        float speed = shepherdSpeed;
        switch (movementIndex)
        { 
            //Stop
            case 0:
                if(movementType == MovementType.Analog)
                {
                    speed = 0;
                } else
                {
                    rotation = 0;
                }
                break;
            //Forward
            case 1:
                if (movementType == MovementType.Analog)
                {
                    speed = shepherdSpeed;
                }
                else
                {
                    rotation = 90;
                }
                break;
            //Turn right
            case 2:
                if (movementType == MovementType.Analog)
                {
                    rotation += angleTurn;
                }
                else
                {
                    rotation = 180;
                }
                break;
            //Turn left
            case 3:
                if (movementType == MovementType.Analog)
                {
                    rotation -= angleTurn;
                }
                else
                {
                    rotation = 270;
                }
                break;
        }

        // Calculate and return shepherds next position
        Vector3 lDirection = new Vector3 (Mathf.Sin(Mathf.Deg2Rad * rotation), 0, Mathf.Cos(Mathf.Deg2Rad * rotation));
        Vector3 nextPosition = simPosition + lDirection * speed;
        return nextPosition;
    }

    // Set up environment and first shepherd
    void Awake () {
        simulation = GameObject.Find("Game Manager").GetComponent<Simulation>();
        simulating = simulation.simulatingAnimation;
        barn = GameObject.Find("Barn");
        sight = GetSight(sightType);
        
        dna = new MarkovDNA();
        markovMovementOutput = new int[2];
        brain = new List<LogicGate>();

        for (int i = 0; i < dna.strand.Length - 1; i++)
        {
            byte[] readCodons = { dna.strand[i], dna.strand[i + 1] };
            if (dna.EqualsStartCodon(readCodons))
            {
                LogicGate lg = new LogicGate(i + 2, logicType);
                //print(lg.ToString());
                brain.Add(lg);
            }
        }
	}

    // Reset dog by generating a random new one
    public void Reset()
    {
        sight = GetSight(sightType);
        simPosition = new Vector3(-60, 0, -60);
        totalMovement = 0;
        rotation = 0;

        dna = new MarkovDNA();
        markovMovementOutput = new int[2];
        brain = new List<LogicGate>();

        for (int i = 0; i < dna.strand.Length - 1; i++)
        {
            byte[] readCodons = { dna.strand[i], dna.strand[i + 1] };
            if (dna.EqualsStartCodon(readCodons))
            {
                LogicGate lg = new LogicGate(i + 2, logicType);
                //print(lg.ToString());
                brain.Add(lg);
            }
        }
    }

    // Reset dog using MarkovDNA as input
    public void Reset(MarkovDNA dna)
    {
        sight = GetSight(sightType);
        simPosition = new Vector3(-60, 0, -60);
        totalMovement = 0;
        rotation = 0;
        this.dna = dna;
        markovMovementOutput = new int[2];
        brain = new List<LogicGate>();

        for (int i = 0; i < dna.strand.Length - 1; i++)
        {
            byte[] readCodons = { dna.strand[i], dna.strand[i + 1] };
            if (dna.EqualsStartCodon(readCodons))
            {
                LogicGate lg = new LogicGate(i + 2, logicType);
                //print(lg.ToString());
                brain.Add(lg);
            }
        }
    }

    // Update is called once per frame
    void Update () {
        simulating = simulation.simulatingAnimation;    

        // Manual imput for testing purposes
        if (simulating)
        {
            //print(string.Join("", new List<int>(sight).ConvertAll(i => i.ToString()).ToArray()));

            Vector3 nextPosition = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                Vector3 lDirection = new Vector3(Mathf.Sin(Mathf.Deg2Rad * rotation), 0, Mathf.Cos(Mathf.Deg2Rad * rotation));
                nextPosition = simPosition + lDirection;
            }
            if (Input.GetKey(KeyCode.D))
            {
                rotation += angleTurn;
            }
            if (Input.GetKey(KeyCode.S))
            {
            }
            if (Input.GetKey(KeyCode.A))
            {
                rotation -= angleTurn;
            }
            if (nextPosition != Vector3.zero)
            {
                simPosition = nextPosition;
            }
            UpdateSimulatedPosition();
        }
    }

    // Animate the shepherd for simulation
    void UpdateSimulatedPosition()
    {
        transform.position = simPosition;
    }

    
}
