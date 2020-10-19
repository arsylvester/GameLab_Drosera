﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gunner : PlayerBase
{
    GunnerPrimaryFire _primaryFire;
    GunnerAltFire _altFire;
    GunnerGrenade _grenade;
    GunnerDOTGrenade _dotGrenade;

    bool _altAbility = false;

    protected override void Awake()
    {
        base.Awake();
        instance = this;

        _primaryFire = GetComponent<GunnerPrimaryFire>();
        _altFire = GetComponent<GunnerAltFire>();
        _grenade = GetComponent<GunnerGrenade>();
        _dotGrenade = GetComponent<GunnerDOTGrenade>();
    }

    protected override void Update()
    {
        base.Update();

        if (currentState != PlayerState.Dead)
        {
            if (swapAbilityButton)
            {
                if (!_altAbility)
                {
                    _altAbility = true;
                }
                else
                {
                    _altAbility = false;
                }
            }
        }
    }

    protected override void Attacking()
    {
        if (ammo > 0)
        {
            if ((shootButtonKey || shootButtonGamepad == 1) && !altFireButton)
            {
                _primaryFire.Fire(); 
            }
            currentState = PlayerState.Neutral;
        }
        else
        {
            currentState = PlayerState.Reloading;
        }

        if (altFireButton)
        {
            _altFire.Fire();
            currentState = PlayerState.Neutral;
        }
    }

    protected override void Ability()
    {
        if (!_altAbility)
        {
            _grenade.Fire();
        }
        else
        {
            _dotGrenade.Fire();
        }
        currentState = PlayerState.Neutral;
    }
}
