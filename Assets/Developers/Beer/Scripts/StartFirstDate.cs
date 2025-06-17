using UnityEngine;

public class StartFirstDate : MonoBehaviour
{
    [SerializeField]
    private SetBachelor setBachelor;

    public void Start()
    {
        if (setBachelor != null)
        {
            setBachelor.SetBatchelor();
        }
        else
        {
            Debug.LogError("SetBachelor component not found in the scene.");
        }
    }
}
