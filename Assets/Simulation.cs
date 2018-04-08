using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;


public class Simulation : MonoBehaviour {

    public GameObject sheepPrefab;
    public GameObject barn;
    public Shepherd shepherd;
    public bool simulatingAnimation = true;
    public bool runBestDog = false;
    public int sheepInitial = 20;
    public int sheepLeft;
    public int stepsInitial = 1000;
    int stepsLeft;
    public int score;

    // game settings
    // hardcoded spawn boundaries
    private float minSpawnX = -40.0f;
    private float maxSpawnX = 40.0f;
    private float maxSpawnZ = 25.0f;
    private float minSpawnZ = -45.0f;

    public int noSimulations;
    public int noSpecimens = 500;
    public int noRandoms = 10;
    public int noElitists = 10;
    MarkovDNA[] specimens;
    MarkovDNA[] previousSpecimens;

    float[] specimenFitness;
    float[] previousSpecimenFitness;

    float bestFitness = -1;

    [HideInInspector]
    public Sheep[] sheep;
    Vector3[] initialSheepLocations;

    public enum SheepSpawn { Static, Random };
    public SheepSpawn sheepSpawn = SheepSpawn.Static;
    public bool sheepGrazing = false;

    StreamWriter fitnessLogger;
    StreamWriter bestFitnessLogger;

    private string fileName = "DNA";
    private string fileType = ".txt";
    private string bestDogFile = "BestDOG";
    private string fitnessLogFile = "fitProgress";
    float bestDogFitness = -100;

    bool firstSpawn = true;
    // Reset and spawn new sheep at start of simulation
    void SpawnSheep()
    {
        if (firstSpawn || sheepSpawn == SheepSpawn.Random)
        {
            //Cleanup
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Sheep"))
            {
                Destroy(go);
            }
            sheep = new Sheep[sheepInitial];

            // spawn
            Vector3 position;

            for (int i = 0; i < sheepInitial; i++)
            {
                position = new Vector3(UnityEngine.Random.Range(minSpawnX, maxSpawnX), .0f, UnityEngine.Random.Range(minSpawnZ, maxSpawnZ));
                GameObject go = GameObject.Instantiate(sheepPrefab, position, Quaternion.identity) as GameObject;
                go.transform.eulerAngles = new Vector3(90, 0, 0);
                go.GetComponent<Sheep>().simPosition = position;
                go.name = "S" + i;
                sheep[i] = go.GetComponent<Sheep>();
                initialSheepLocations[i] = sheep[i].simPosition;
            }
            firstSpawn = false;
        }
        else if (!firstSpawn)
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Sheep"))
            {
                go.GetComponent<SpriteRenderer>().enabled = true;
                go.GetComponent<Sheep>().collected = false;
            }
            for (int i = 0; i < sheepInitial; i++)
            {
                sheep[i].simPosition = initialSheepLocations[i];
            }
        }
    }

    // Set neighbours of all sheep
    void SetSheepNeighbours()
    {
        foreach(Sheep s in sheep)
        {
            s.allSheep = sheep;
        }
    }

    

    // Use this for initialization
    void Start () {
        Application.runInBackground = true;
        specimens = new MarkovDNA[noSpecimens];
        specimenFitness = new float[noSpecimens];
        sheep = new Sheep[sheepInitial];
        initialSheepLocations = new Vector3[sheepInitial];

        if (!simulatingAnimation)
        {
            runBestDog = false;
        }
        fitnessLogger = new StreamWriter(fitnessLogFile + fileType, true);
        bestFitnessLogger = new StreamWriter("bestFitness" + fileType, true);

        if (!runBestDog)
        {
            StartCoroutine(SimulateStepByStep(noSimulations));

        } else
        {
            StartCoroutine(SimulateBestDog());
    
        }

    }

    // Gets best dog from a file and simulates it
    IEnumerator SimulateBestDog()
    {
        sheepLeft = sheepInitial;
        stepsLeft = stepsInitial;
        SpawnSheep();
        SetSheepNeighbours();

        string s = new StreamReader("BestDOG.txt").ReadLine().ToString();
        shepherd.Reset(new MarkovDNA(s));
        for (int j = 0; j < 200000; j++)
        {
            for (int k = 0; k < sheep.Length; k++)
            {
                if (!sheep[k].collected)
                    sheep[k].Move();
            }
            shepherd.Move();
                yield return null;
            stepsLeft--;
        }
    }

    float initialDistanceFromBarn = 0;
    public float totalSheepBarned = 0;

    float VectorSum(float[] vec)
    {
        float sum = 0;
        for (int i = 0; i < vec.Length; i++)
        {
            sum += vec[i];
        }
        return sum;
    }
    // See simulation in runtime
    IEnumerator SimulateStepByStep(int noSimulations)
    {
        previousSpecimens = new MarkovDNA[noSpecimens];
        previousSpecimenFitness = new float[noSpecimens];
        print("STEP BY STEP SIM START " + System.DateTime.Now);

        for (int s = 0; s < noSimulations; s++)
        {
            totalSheepBarned = 0;
            for (int i = 0; i < noSpecimens; i++)
            {
                //Initialize 
                sheepLeft = sheepInitial;
                stepsLeft = stepsInitial;
                SpawnSheep();
                SetSheepNeighbours();
                
                initialDistanceFromBarn = SheepDistanceFromBarn();
                // Random shepherds for the first iteration
                if(s == 0)
                {
                    shepherd.Reset();
                }
                // Get top guys to the field again
                else if(i < noElitists)
                {
                    shepherd.Reset(previousSpecimens[i]);
                }
                // Insert randoms to help prevent local maxima
                else if(i > noSpecimens - noRandoms)
                {
                    shepherd.Reset();
                }
                // Get two DNAs from the previous simulation and merge them to create new shepherds
                else
                {
                    MarkovDNA dna1 = null;
                    MarkovDNA dna2 = null;
                    float cummulativeProbability = 0;
                    float random1 = Random.Range(0f, 1f);
                    float random2 = Random.Range(0f, 1f);
                    int index1 = -1;
                    int index2 = -1;
                    float fitnessSum = VectorSum(previousSpecimenFitness);
                    //The best shepherd has the highest chance of reproducing
                    for (int j = 0; j < previousSpecimenFitness.Length; j++)
                    {
                        cummulativeProbability += previousSpecimenFitness[j] / fitnessSum;
                        if (index1 == -1 && random1 < cummulativeProbability)
                        {
                            index1 = j;
                        }
                        if(index2 == -1 && random2 < cummulativeProbability)
                        {
                            index2 = j;
                        }
                    }

                    // Ensure indexes aren't the same
                    while (index1 == index2)
                        index2 = Random.Range(0, noSpecimens);

                    dna1 = previousSpecimens[index1];
                    dna2 = previousSpecimens[index2];
                    shepherd.Reset(new MarkovDNA(dna1, dna2));
                }

                specimens[i] = shepherd.dna;

                // Simulate 
                for (int j = 0; j < stepsInitial; j++)
                {
                    for (int k = 0; k < sheep.Length; k++)
                    {
                        if (!sheep[k].collected)
                            sheep[k].Move();
                    }
                    shepherd.Move();
                    if (simulatingAnimation)
                        yield return new WaitForSeconds(0.05f);
                    stepsLeft--;

                }
                specimenFitness[i] = FitnessFunction();
                if (!simulatingAnimation)
                    yield return null;
            }

            PrintWithTime("[" + s + "] OVER");
            //printWithTime("Sorting");
            SortSpecimens();
            //printWithTime("Sorted");
            
            //Save best dog
            if (specimenFitness.Last() > bestDogFitness)
            {
                bestDogFitness = specimenFitness.Last();
                using (StreamWriter dogWriter = new StreamWriter(bestDogFile + fileType))
                {
                    dogWriter.Write(ArrayToString(specimens.Last().strand));
                }
            }
            //Save the fitness value of the average dog
            float fitAvg = 0;
            for(int i = 0; i < specimenFitness.Length; i++)
            {
                //print(specimenFitness[i]);
                fitAvg += specimenFitness[i];
            }
            fitAvg /= specimenFitness.Length;

            print("Average fitness: " + fitAvg);
            print("Average gene difference: " + AverageDifferenceInGenes());
            print("Average sheep barned: " + (totalSheepBarned)/noSpecimens + " out of " + sheepInitial);
            fitnessLogger.Write(fitAvg + "\n");
            bestFitnessLogger.Write((totalSheepBarned) / noSpecimens + "\n");

            // Switch to a new file every 100 generations
            if (s > 0 && s % 100 == 0)
            {
                fitnessLogger.Close();
                fitnessLogger = new StreamWriter(fitnessLogFile + s + fileType);
                bestFitnessLogger.Close();
                bestFitnessLogger = new StreamWriter("averageSheep" + s + fileType);
            }
            specimenFitness.CopyTo(previousSpecimenFitness, 0);
            specimens.CopyTo(previousSpecimens, 0);

        }
        fitnessLogger.Close();
    }

    // Returns a number how well the shepherd did at his task
    float FitnessFunction()
    {
        if (shepherd.totalMovement == 0)
        { 
            return 1;
        }

        return 2 + ((sheepInitial - sheepLeft)*50 + (SheepDistanceFromBarn() - initialDistanceFromBarn) + ShepherdDistanceFromSheep())/TotalShepherdMovement();// + SheepGrouping(); // + ShepherdDistanceFromSheep() + stepsLeft;
    }
    
    // Return the max distance between any two sheep
    float SheepGrouping()
    {
        float maxDistance = 0;
        if (sheepLeft == 0)
            return 0;
        foreach (Sheep s in sheep)
        {
            foreach (Sheep s1 in sheep)
            {
                if(s != s1)
                {
                    if(!s1.collected && !s.collected)
                    {

                        if (Vector3.Distance(s.simPosition, s1.simPosition) > maxDistance)
                        {
                            maxDistance = Vector3.Distance(s.simPosition, s1.simPosition);
                        }
                    }
                }
            }
        }
        return Mathf.Max(0, 50 - maxDistance);
    }

    // Shepherd's distance from the average sheep
    float ShepherdDistanceFromSheep()
    {
        float avgDistance = 0;
        if(sheepLeft == 0)
        {
            return 0;
        }
        foreach(Sheep s in sheep)
        {
            if (!s.collected)
            {
                avgDistance += Vector3.Distance(shepherd.simPosition, s.simPosition);
            }
        }
        return Mathf.Max(0, 50 - avgDistance / sheepLeft);
    }

    float TotalShepherdMovement()
    {
        return 1 + shepherd.totalMovement/stepsInitial;
    }
    // Returns a number for how far the average sheep was from the barn
    // Closer -> bigger number
    float SheepDistanceFromBarn()
    {
        float totalDistance = 0;
        if (sheepLeft == 0)
            return 0;
        foreach (Sheep s in sheep)
        {
            totalDistance += Vector3.Distance(barn.transform.position, s.simPosition);
        }
        return Mathf.Max(0, 100 - totalDistance / sheepLeft);
    }

    //Sorts specimens according to their fitness functions
    private void SortSpecimens()
    {
        for (int i = 0; i < specimenFitness.Length; i++)
        {
            for (int j = 0; j < specimenFitness.Length-1; j++)
            {
                if (specimenFitness[j] > specimenFitness[j+1])
                {
                    float temp = specimenFitness[j];
                    specimenFitness[j] = specimenFitness[j + 1];
                    specimenFitness[j + 1] = temp;

                    MarkovDNA tempDNA = specimens[j];
                    specimens[j] = specimens[j+1];
                    specimens[j + 1] = tempDNA;
                }
            }
        }
    }

    void PrintWithTime(string s)
    {
        print(s + " @ " + System.DateTime.Now);
    }

    string ArrayToString<T>(T[] a)
    {
        string toPrint = "";
        foreach (T item in a)
        {
            toPrint += item + ",";
        }
        return toPrint;
    }

    float AverageDifferenceInGenes()
    {
        float avgDif = 0;
        for (int i = 0; i < specimens.Length; i++)
        {
            for(int j = 0; j < specimens.Length; j++)
            {
                avgDif += specimens[i].difference(specimens[j]);
            }
        }
        return avgDif/specimens.Length/specimens.Length;
    }
}
