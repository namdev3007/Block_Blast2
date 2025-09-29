using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridInput : MonoBehaviour
{
    public GraphicRaycaster raycaster;  
    public EventSystem eventSystem;     

    public bool TryGetCell(Vector2 screenPos, out GridSquareView cell)
    {
        var ped = new PointerEventData(eventSystem) { position = screenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        foreach (var rr in results)
        {
            var gsv = rr.gameObject.GetComponentInParent<GridSquareView>();
            if (gsv != null) { cell = gsv; return true; }
        }
        cell = null;
        return false;
    }
}
