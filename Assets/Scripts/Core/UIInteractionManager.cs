using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIInteractionManager : MonoBehaviour
{
    [Header("UI Controls (Buttons, Toggles, Sliders, etc.)")]
    [SerializeField] private List<Selectable> uiControls;

    private static UIInteractionManager _instance;
    public static UIInteractionManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void RegisterControl(Selectable control)
    {
        if (!uiControls.Contains(control))
            uiControls.Add(control);
    }

    public void DisableAllExcept(Selectable activeControl)
    {
        foreach (var control in uiControls)
        {
            if (control != null)
                control.interactable = (control == activeControl);
        }
    }

    public void EnableAll()
    {
        foreach (var control in uiControls)
        {
            if (control != null)
                control.interactable = true;
        }
    }
}
