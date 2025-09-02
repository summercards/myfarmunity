// Assets/Scripts/Character/TPSStateMachine.cs
using System;
using UnityEngine;

public abstract class State
{
    protected TPSCharacter owner;
    protected State(TPSCharacter owner) { this.owner = owner; }
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void Tick(float dt) { }
}

public class TPSStateMachine
{
    public State Current { get; private set; }
    public event Action<State> OnStateChanged;

    public void Init(State start)
    {
        Current = start;
        Current?.OnEnter();
        OnStateChanged?.Invoke(Current);
    }

    public void Change(State next)
    {
        if (next == null || next == Current) return;
        Current?.OnExit();
        Current = next;
        Current?.OnEnter();
        OnStateChanged?.Invoke(Current);
    }

    public void Tick(float dt) => Current?.Tick(dt);
}
