using TMPro;
using UnityEngine;

namespace UI
{
    public class ColorOptionData : TMP_Dropdown.OptionData
    {
        public ColorOptionData(string text, Sprite image, Color color) : base(text, image)
        {
            this.color = color;
        }
        public Color color { get; set; }
    }
}
