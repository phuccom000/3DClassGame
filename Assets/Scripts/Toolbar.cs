using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    // World world;
    public World world;
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
        if (!world.inUI)
        {
            Debug.Log("In UI");
            highlight.gameObject.SetActive(true);
            selectedItemText.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Out UI");
            highlight.gameObject.SetActive(false);
            selectedItemText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (!world.inUI)
        {
            //Debug.Log("In UI");
            highlight.gameObject.SetActive(true);
            selectedItemText.gameObject.SetActive(true);
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
            }
        }
        else
        {
            //Debug.Log("Out UI");
            highlight.gameObject.SetActive(false);
            selectedItemText.gameObject.SetActive(false);
        }
        selectedItemText.text = slots[slotIndex].GetSlotItemName();
    }
}