using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LogicGate {

    public int[] inputNodes;
    public int[] inputNodesIndexes;
    public int[] outputNodes;
    public int[] outputNodesIndexes;

    public float[,] probabilities;
    Shepherd shepherd;

    public enum LogicType { Probabilistic, Set }

    // Get the logic gate from DNA
    public LogicGate(int firstNucleotideIndex, LogicType logicType = LogicType.Probabilistic)
    {
        shepherd = GameObject.Find("Shepherd").GetComponent<Shepherd>();
        SetInputOutputNodes();
        MarkovDNA dna = shepherd.dna;

        int index = firstNucleotideIndex;
        index %= dna.Length;

        inputNodesIndexes = new int[dna.strand[index] % dna.maxInputs];
        index++;
        index %= dna.Length;

        outputNodesIndexes = new int[dna.strand[index] % dna.maxOutputs];
        index++;
        index %= dna.Length;

        // Read input nodes
        for (int i = 0; i < inputNodesIndexes.Length; i++)
        {
            inputNodesIndexes[i] = dna.strand[index] % inputNodes.Length;
            index++;
            index %= dna.Length;
        }

        // Read output nodes
        for (int i = 0; i < outputNodesIndexes.Length; i++)
        {
            outputNodesIndexes[i] = dna.strand[index] % outputNodes.Length;
            index++;
            index %= dna.Length;
        }

        // Read probabilities for output nodes and set values from 0 to 1
      
            probabilities = new float[(int)Mathf.Pow(2, inputNodesIndexes.Length), outputNodesIndexes.Length];
            for (int i = 0; i < Mathf.Pow(2, inputNodesIndexes.Length); i++)
            {
                for (int j = 0; j < outputNodesIndexes.Length; j++)
                {
                switch (logicType)
                {
                    case LogicType.Probabilistic:
                        probabilities[i, j] = (float)dna.strand[index] / (float)dna.maxNucleotideValue;
                        break;
                    case LogicType.Set:
                        probabilities[i, j] = Mathf.Round(dna.strand[index] / (float)dna.maxNucleotideValue);
                        break;
                }
                    index++;
                    index %= dna.Length;
                }
            }
        
    }
    
    // Reset the nodes according to what the shepherd sees currently or what the brain has changed so far
    public void SetInputOutputNodes()
    {
        inputNodes = shepherd.sight;
        outputNodes = shepherd.sight.Concat(shepherd.markovMovementOutput).ToArray();
    }

    // Calculate the output nodes and return
    public int[] GetOutputNodes()
    {
        SetInputOutputNodes();

        // Figure out which row of the probabilities table to take into account
        int index = 0;
        for(int i = 0; i < inputNodesIndexes.Length; i++)
        {
            index += (int)Mathf.Pow(2, i) * inputNodes[inputNodesIndexes[i]];
        }

        // Set output nodes according to the probabilities
        for (int i = 0; i < outputNodesIndexes.Length; i++)
        {
            float randomValue = Random.Range(0f, 1f);
            if (randomValue < probabilities[index, i])
            {
                outputNodes[outputNodesIndexes[i]] = 1;
            }
        }


        
        return outputNodes;
    }

    // Input, output and probabilities to string
    override public string ToString()
    {
        string gate = "PLG\nInput:\n";

        for (int i = 0; i < inputNodesIndexes.Length; i++)
        {
            gate += inputNodesIndexes[i].ToString() + " ";
        }
        gate += "\nOutput[" + outputNodes.Length + "]: \n";
        for (int i = 0; i < outputNodesIndexes.Length; i++)
        {
            gate += outputNodesIndexes[i].ToString() + " ";
        }
        gate += ("\n");
        for (int i = 0; i < outputNodes.Length; i++)
        {
            gate += outputNodes[i].ToString() + " ";
        }
        gate += "\nProbabilities:\n";
        for (int i = 0; i < probabilities.GetLength(0); i++)
        {
            gate += i + " | ";
            for (int j = 0; j < probabilities.GetLength(1); j++)
            {
                gate += probabilities[i, j].ToString() + " ";
            }
            gate += "\n";
        }
        return gate;
    }
}
