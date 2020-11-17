using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHolster : MonoBehaviour
{
    public int selectedWeapon;
    public List<PlayerWeapon> weapons;
    private Text ammoText;

    private void Awake()
    {
        weapons.Add(new PlayerWeapon("Pistol", 20f, 25f));
        weapons.Add(new PlayerWeapon("AK47", 50f, 40f));
        weapons.Add(new PlayerWeapon("Shotgun", 80f, 10f));
        weapons.Add(new PlayerWeapon("SniperRifle", 100f, 200f));
        weapons.Add(new PlayerWeapon("MAC10", 30f, 25f));
    }

    void Start()
    {
        SelectWeapon();
    }

    private void SelectWeapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            if (i == selectedWeapon)
            {
                weapon.gameObject.SetActive(true);
            }
            else
                weapon.gameObject.SetActive(false);
            i++;
        }
    }

    public void SetWeapon(string weaponName)
    {
        int previousSelectedWeapon = selectedWeapon;
        switch (weaponName)
        {
            case "Pistol":
                selectedWeapon = 0;
                break;
            case "AK47":
                selectedWeapon = 1;
                break;
            case "Shotgun":
                selectedWeapon = 2;
                break;
            case "SniperRifle":
                selectedWeapon = 3;
                break;
            case "MAC10":
                selectedWeapon = 4;
                break;
        }
        if (previousSelectedWeapon != selectedWeapon && selectedWeapon < weapons.Count) 
        {
            SelectWeapon();
        }
        else
        {
            selectedWeapon = previousSelectedWeapon;
        }
    }

    public PlayerWeapon GetSelectedWeapon()
    {
        return weapons[selectedWeapon];
    }
    
    public void UpdateAmmoInfo()
    {
        Text ammoText = GameObject.Find("AmmoText").GetComponent<Text>();
        PlayerWeapon currentWeapon = weapons[selectedWeapon];
        if (currentWeapon.name == "Pistol")
        {
            ammoText.text = "\u221E";
        }
        else
        {
            if (currentWeapon.ammo <= 0)
                ammoText.text = "<color=red>" + currentWeapon.ammo.ToString() + "</color>";
            else
                ammoText.text = currentWeapon.clip.ToString() + " / " + currentWeapon.ammo.ToString();
        }
    }

    void Update()
    {
        int previousSelectedWeapon = selectedWeapon;
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            if (selectedWeapon >= transform.childCount - 1)
                selectedWeapon = 0;
            else
                selectedWeapon++;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            if (selectedWeapon <= 0)
                selectedWeapon = transform.childCount - 1;
            else
                selectedWeapon--;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedWeapon = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2)
        {
            selectedWeapon = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3)
        {
            selectedWeapon = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) && transform.childCount >= 4)
        {
            selectedWeapon = 3;
        }

        if (previousSelectedWeapon != selectedWeapon && selectedWeapon < weapons.Count) 
        {
            SelectWeapon();
        }
        else
        {
            selectedWeapon = previousSelectedWeapon;
        }
        UpdateAmmoInfo();
    }

}
