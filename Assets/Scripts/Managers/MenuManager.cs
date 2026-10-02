using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PanelPrincipal;
    [SerializeField] private GameObject nivelPanel;
    [SerializeField] private GameObject PanelPersonajes;

    private Stack<GameObject> HistorialPaneles = new Stack<GameObject>();

    private GameObject PanelActual;

    private void Start()
    {
        ShowInitialPanel(PanelPrincipal);
    }

    private void ShowInitialPanel(GameObject panel)
    {
        PanelPrincipal.SetActive(false);
        nivelPanel.SetActive(false);
        PanelPersonajes.SetActive(false);

        panel.SetActive(true);
        PanelActual = panel;

        HistorialPaneles.Clear();
    }

    public void OpenPanel(GameObject sigPanel)
    {
        Debug.Log("Opening panel: " + sigPanel.name);
        if (PanelActual != null)
        {
            HistorialPaneles.Push(PanelActual);
            PanelActual.SetActive(false);
        }

        sigPanel.SetActive(true);
        PanelActual = sigPanel;
    }

    public void GoBack()
    {
        if (HistorialPaneles.Count == 0)
            return;

        PanelActual.SetActive(false);

        GameObject PanelPrevio = HistorialPaneles.Pop();

        PanelPrevio.SetActive(true);
        PanelActual = PanelPrevio;
    }
}