using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VariantScript : MonoBehaviour
{
    public void Appearance()
    {
        this.gameObject.SetActive(true);
        this.GetComponent<Animator>().SetTrigger("appearance");
    }
    public void Deactiv()
    {
        this.GetComponent<Button>().interactable = false;
        this.GetComponent<Animator>().SetTrigger("disappearanceS");
    }
    private void Dest()
    {
        Destroy(this.gameObject);
    }
}
