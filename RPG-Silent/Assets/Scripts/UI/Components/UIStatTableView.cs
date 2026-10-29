using System;
using TMPro;
using UnityEngine;

public sealed class UIStatTableView : MonoBehaviour
{
    [Serializable]
    private struct StatRow { public TMP_Text First, Second, Third; }
    [SerializeField] private TMP_Text firstHeading, secondHeading, thirdHeading;
    [SerializeField] private StatRow[] rows;

    public void Present(string first, string second, string third, int[] a, int[] b, int[] c, bool delta)
    {
        firstHeading.text = first; secondHeading.text = second; thirdHeading.text = third;
        for (int i = 0; i < rows.Length; i++)
        {
            string suffix = i >= 2 ? "%" : "";
            rows[i].First.text = a[i] + suffix;
            rows[i].Second.text = b[i] + suffix;
            rows[i].Third.text = (delta && c[i] > 0 ? "+" : "") + c[i] + suffix;
            rows[i].Third.color = !delta ? new Color(.85f, .78f, .58f) : c[i] > 0 ? new Color(.55f, .83f, .68f) : c[i] < 0 ? new Color(.88f, .48f, .45f) : new Color(.62f, .70f, .67f);
        }
    }
}
