using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ColorDropDown : TMP_Dropdown
{
    private const int imageColorItemIndex = 3;
    private int dataIndex = 0;

    public void ColorChange(Color color)
    {
        captionImage.color = color;
    }

    protected override GameObject CreateDropdownList(GameObject template)
    {
        dataIndex = 0;
        return base.CreateDropdownList(template);
    }
    protected override DropdownItem CreateItem(DropdownItem itemTemplate)
    {
        var item = base.CreateItem(itemTemplate);
        var imageTemplate = item.transform.GetChild(imageColorItemIndex);
        var bgTemplate = item.transform.GetChild(0);
        var image = imageTemplate.GetComponent<Image>();
        
        //var bg = bgTemplate.GetComponent<Image>();
        var data = this.options[dataIndex];
        if (data is ColorOptionData colorOptionData)
        {
            //Debug.Log("C: " + colorOptionData.text);
            //bg.color = colorOptionData.color;
            image.color = colorOptionData.color;
        }
        else
        {
            //bg.color = Color.blue;
        }

        dataIndex++;
        return item;
    }
}
