using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "UnDFight/Stage Registry")]
public sealed class StageRegistry : ScriptableObject
{
    public List<StageDefinition> stages = new List<StageDefinition>();
    public IEnumerable<StageDefinition> Available => stages.Where(s => s != null && s.available).OrderBy(s => s.sortOrder).ThenBy(s => s.stableId);
    public bool Validate(out string error)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach(var s in stages)
        {
            if(s == null || string.IsNullOrWhiteSpace(s.stableId) || !ids.Add(s.stableId)) { error = "Arena ausente ou ID vazio/duplicado"; return false; }
            if(s.environmentPrefab == null || !s.environmentPrefab.Validate(out error)) { error = s.name + ": prefab/contrato inválido"; return false; }
        }
        error = null; return true;
    }
    public bool TryGet(string id, out StageDefinition stage)
    {
        stage = null;
        if(!Validate(out _)) return false;
        stage = Available.FirstOrDefault(s => s.stableId == id);
        return stage != null;
    }
}
