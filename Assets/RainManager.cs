using UnityEngine;
using UnityEngine.UI;

public class RainManager : MonoBehaviour
{
    public Toggle toggle;

    private GameObject[] objectsWithTag;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectsWithTag = GameObject.FindGameObjectsWithTag("Hat");
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        OnToggleValueChanged(toggle.isOn);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnToggleValueChanged(bool isOn)
    {
        foreach (GameObject obj in objectsWithTag)
        {
            // Check if the object has a Renderer component and enable/disable it
            Renderer objRenderer = obj.GetComponent<Renderer>();
            if (objRenderer != null)
            {
                objRenderer.enabled = isOn;  // Show if the toggle is on, hide if off
            }
        }
    }
}
