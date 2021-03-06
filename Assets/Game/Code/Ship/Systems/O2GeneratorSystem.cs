﻿using UnityEngine;
using UnityTK;

public class O2GeneratorSystem : ShipSystem
{
    public override ShipSystemType shipSystemType
    {
        get
        {
            return ShipSystemType.O2_GENERATOR;
        }
    }

    /// <summary>
    /// The generation rate in units / s
    /// </summary>
    public float generationRate = 0.35f;

    protected override void UpdateSystem(float currentEfficiency)
    {
        Ship.instance.oxygen.value += this.generationRate * currentEfficiency * Time.fixedDeltaTime;
    }

    protected override float ComputeWorkLoad(float predictedEfficiency)
    {
        float wouldGenerate = this.generationRate * predictedEfficiency * Time.fixedDeltaTime;
        float willGenerate = Mathf.Min(wouldGenerate, Ship.instance.oxygen.maxDelta);

        if (Mathf.Approximately(wouldGenerate, 0))
            return 0;

        return willGenerate / wouldGenerate;
    }
}