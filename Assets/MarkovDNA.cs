using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MarkovDNA {

    public int Length = 10000;
    public readonly byte[] startCodon = { 42, 213 };
    public readonly byte maxInputs = 5;
    public readonly byte maxOutputs = 5;
    public int maxNucleotideValue = 255;
    float mutationRate = 0.025f;

    public byte[] strand;

    // Random DNA strand
    public MarkovDNA()
    {
        strand = RandomDNAstrand();
    }

    // Generate DNA from file
    public MarkovDNA(string input)
    {
        strand = new byte[Length];


        string[] s = input.Split(',');

        for (int i = 0; i < Length; i++)
        {
            strand[i] = byte.Parse(s[i]);
        }
    }

    // Generate DNA from two parent DNAs
    public MarkovDNA(MarkovDNA dna1, MarkovDNA dna2)
    {
        int swapLocation = Random.Range(0, Length);
        byte[] newDNAstrand;

        // Swap at dna halves
        if (Random.Range(0, 1f) > 0.5f)
        {
            newDNAstrand = dna1.strand.Take(swapLocation).Concat(dna2.strand.Skip(swapLocation).Take(Length - swapLocation)).ToArray();
        }
        else
        {
            newDNAstrand = (byte[])dna2.strand.Take(swapLocation).Concat(dna1.strand.Skip(swapLocation).Take(Length - swapLocation)).ToArray();
        }
        // Apply Mutation
        newDNAstrand = ApplyMutation(newDNAstrand);
        strand = newDNAstrand;
    }

    // Mutate nucleotides of DNA
    private byte[] ApplyMutation(byte[] strand)
    {
        for (int i = 0; i < strand.Length; i++)
        {
            if (Random.Range(0f, 1f) < mutationRate)
            {
                strand[i] = (byte)Random.Range(0, maxNucleotideValue);
            }
        }
        return strand;
    }

    // Random DNA for testing purposes
    private byte[] RandomDNAstrand()
    {
        bool hasAtLeastOneStartCodon = false;
        byte[] strand = new byte[Length];
        while (!hasAtLeastOneStartCodon)
        {
            strand = new byte[Length];
            for (int i = 0; i < Length; i++)
            {
                strand[i] = (byte)Random.Range(0, maxNucleotideValue);
            }
            //Check if it has start codons
            for (int i = 0; i < strand.Length - 1; i++)
            {
                byte[] readCodon = { strand[i], strand[i + 1] };
                if (EqualsStartCodon(readCodon))
                {
                    hasAtLeastOneStartCodon = true;
                }
            }
        }
        return strand;
    }

    // Check if chosen codon is equal to logic gate start codon
    public bool EqualsStartCodon(byte[] codon)
    {
        for (int i = 0; i < codon.Length; i++)
        {
            if (codon[i] != startCodon[i])
                return false;
        }
        return true;
    }

    public float difference(MarkovDNA otherDNA)
    {
        float diff = 0;
        for(int i = 0; i < Length; i++)
        {
            diff += Mathf.Abs(strand[i] - otherDNA.strand[i]);
        }
        return diff / Length;
    }
}
