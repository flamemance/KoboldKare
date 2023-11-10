using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaturationReagentEffect : ModifyingReagentEffect
{
    public override void Apply(Kobold k, float usedAmount, ref KoboldGenes genes, ref ReagentContents addBack, ref float energy)
    {
        genes = genes.With(saturation: (byte)Mathf.Clamp(genes.saturation + (byte)(Mathf.CeilToInt(usedAmount * Multiplier)), 0, 255));
    }
}
