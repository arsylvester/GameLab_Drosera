﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class EntityBase : MonoBehaviour
{
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;
    public UnityEvent OnHeal;

    [SerializeField] protected float _maxHealth;
    protected float _health;
    [SerializeField] protected bool _canAct;
    [SerializeField] protected bool _canStun;
    [SerializeField] protected float _knockbackResistance;
    [SerializeField] protected float _coolDown;
    [SerializeField] protected float _moveSpeed;

    [SerializeField] protected CharacterController _controller;
    [SerializeField] protected Animator _animator;

    protected virtual IEnumerator Knockback(float force, Vector3 direction)
    {
        //apply knockback here
        yield return null;
    }

    protected virtual IEnumerator TakeDamage(float value, float knockbackForce, Vector3 knockbackDir)
    {
        StartCoroutine(Knockback(knockbackForce, knockbackDir));
        TakeDamage(value);
        yield return null;
    }

    protected virtual void TakeDamage(float value)
    {
        OnTakeDamage?.Invoke();
        _health -= value;
        if (_health <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    protected virtual float TakeHealing(float value)
    {
        OnHeal?.Invoke();
        _health += value;
        if (_health > _maxHealth)
        {
            _health = _maxHealth;
        }
        return _health;
    }
}
