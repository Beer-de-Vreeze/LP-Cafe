using UnityEngine;

/// <summary>
/// Test script to verify bachelor reset functionality works in both editor and builds
/// </summary>
public class BachelorResetTester : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField]
    private bool testOnStart = false;

    void Start()
    {
        if (testOnStart)
        {
            TestBachelorReset();
        }
    }

    [ContextMenu("Test Bachelor Reset")]
    public void TestBachelorReset()
    {
        Debug.Log("=== Testing Bachelor Reset Functionality ===");

        // Test save data reset flag
        SaveData testSaveData = new SaveData();
        testSaveData.ShouldResetBachelors = true;
        testSaveData.DatedBachelors.Add("TestBachelor");
        testSaveData.RealDatedBachelors.Add("TestBachelor");

        SaveSystem.SerializeData(testSaveData);
        Debug.Log("✓ Set reset flag in save data");

        // Test loading and checking flag
        SaveData loadedData = SaveSystem.Deserialize();
        if (loadedData != null && loadedData.ShouldResetBachelors)
        {
            Debug.Log("✓ Reset flag properly saved and loaded");
        }
        else
        {
            Debug.LogError("✗ Reset flag not properly saved/loaded");
        }

        // Test bachelor runtime reset
        NewBachelorSO[] allBachelors = Resources.FindObjectsOfTypeAll<NewBachelorSO>();
        if (allBachelors.Length > 0)
        {
            NewBachelorSO testBachelor = allBachelors[0];

            // Set some test state
            testBachelor._HasBeenSpeedDated = true;
            if (testBachelor._loveMeter != null)
            {
                testBachelor._loveMeter.IncreaseLove(10);
            }

            // Save the changes to the save file
            testBachelor.MarkAsDated(); // This properly saves the speed dated flag

            Debug.Log(
                $"Before reset - {testBachelor._name}: SpeedDated={testBachelor._HasBeenSpeedDated}, Love={testBachelor._loveMeter?.GetCurrentLove() ?? -1}"
            );

            // Test runtime reset
#if UNITY_EDITOR
            testBachelor.ResetToInitialState();
            Debug.Log("✓ Used editor reset method");
#else
            testBachelor.ResetRuntimeState();
            Debug.Log("✓ Used runtime reset method");
#endif
            Debug.Log(
                $"After reset - {testBachelor._name}: SpeedDated={testBachelor._HasBeenSpeedDated}, Love={testBachelor._loveMeter?.GetCurrentLove() ?? -1}"
            );

            if (
                !testBachelor._HasBeenSpeedDated
                && (
                    testBachelor._loveMeter == null || testBachelor._loveMeter.GetCurrentLove() == 3
                )
            )
            {
                Debug.Log("✓ Bachelor successfully reset");
            }
            else
            {
                Debug.LogError("✗ Bachelor reset failed");
            }
        }
        else
        {
            Debug.LogWarning("No bachelors found for testing");
        }

        // Clean up test data
        SaveData cleanData = new SaveData();
        SaveSystem.SerializeData(cleanData);
        Debug.Log("✓ Cleaned up test save data");

        Debug.Log("=== Bachelor Reset Test Complete ===");
    }
}
