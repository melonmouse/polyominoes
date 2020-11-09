using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IClickableObject {
    void RegisterClick();
}

public class ClickGrid : MonoBehaviour, IPointerDownHandler {
    public GameObject grid;
    public void OnPointerDown(PointerEventData eventData) {
        grid.GetComponent<IClickableObject>().RegisterClick();
    }
}
