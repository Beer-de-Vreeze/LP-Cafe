using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    //input whatever you wan

    [SerializeField]
    public List<string> DatedBachelors = new List<string>();

    [SerializeField]
    public List<string> RealDatedBachelors = new List<string>();

    /// <summary>
    /// Flag indicating that bachelor data should be reset to initial state
    /// Used in builds where we can't modify ScriptableObject assets directly
    /// </summary>
    [SerializeField]
    public bool ShouldResetBachelors = false;
}
