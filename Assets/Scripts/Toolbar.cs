using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    // World world;
    public Player player;
    public RectTransform highlight;
    public Text selectedItemText;

    public int slotIndex = 0;
    private void Start()
    {
        // world = GameObject.Find("World").GetComponent<World>();

        byte index = 1;
        foreach (UIItemSlot s in slots)
        {
            ItemStack stack = new ItemStack(index, Random.Range(2, 65));
            ItemSlot slot = new ItemSlot(s, stack);
            index++;
        }
        if (slots[slotIndex].HasItem)
            selectedItemText.text = slots[slotIndex].GetSlotItemName();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (scroll > 0)
                slotIndex--;
            else slotIndex++;

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;
            highlight.position = slots[slotIndex].slotIcon.transform.position;
            if (slots[slotIndex].HasItem)
                selectedItemText.text = slots[slotIndex].GetSlotItemName();
        }
    }
}