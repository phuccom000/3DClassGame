using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;
    World world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();

        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if (!world.inUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            HandleSlotClick(CheckForSlot());
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            cursorItemSlot.EmptySlot();
            cursorSlot.UpdateSlot();
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if (clickedSlot == null)
        {
            cursorItemSlot.EmptySlot();
            cursorSlot.UpdateSlot(); // Ensure the UI is updated after clearing the slot
            return;
        }
        // If your hand is EMPTY and you click on an EMPTY slot
        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            return;
        }

        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        // If your hand is EMPTY but you click on an item slot
        if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        // If your hand is holding item slot and you click on an EMPTY slot
        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        // If your hand is holding item slot and you click on an item slot
        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            // If both of them are DIFFERENT objects
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();
                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }
            // If both of them are SAME objects
            // if () {

            // }
            return;

        }

    }

    private UIItemSlot CheckForSlot()
    {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.tag == "UIItemSlot")
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
