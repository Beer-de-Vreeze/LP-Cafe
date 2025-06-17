using UnityEngine;

[System.Serializable]
public class SaveData
{
    //input whatever you want
    [SerializeField]
    public int _succesfulDateCount;

    [SerializeField]
    public int _failedDateCount;

    // Public property to access the successful date count
    public int SuccessfulDateCount
    {
        get { return _succesfulDateCount; }
        set { _succesfulDateCount = value; }
    }

    // Public property to access the failed date count
    public int FailedDateCount
    {
        get { return _failedDateCount; }
        set { _failedDateCount = value; }
    }
}
