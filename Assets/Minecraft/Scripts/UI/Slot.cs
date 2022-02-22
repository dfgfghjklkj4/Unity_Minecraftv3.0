using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public int id;
    public HotbarController hotbarController;
    public Image slotFrameImage;
    public Image itemImage;
    public Sprite slotFrameSprite;
    public Sprite selectedSlotFrameSprite;
    public SlotItem item;

    public void Select() {
        this.slotFrameImage.sprite = selectedSlotFrameSprite;
    }

    public void Deselect() {
        this.slotFrameImage.sprite = slotFrameSprite;
    }

    public void Initialize() {
        this.itemImage.sprite = this.item.Image;
    }
    public void SelectItem()
    {
        HotbarController.instanc.SelectItem(id);
    }
}
