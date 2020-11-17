using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHolsterServer : MonoBehaviour
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
    
}
