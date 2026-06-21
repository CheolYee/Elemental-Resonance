using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Battle.UI
{
    public class CardSelectClickBridge : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
    }
}
