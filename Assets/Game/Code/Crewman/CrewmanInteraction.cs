﻿using UnityEngine;
using UnityTK;
using UnityTK.BehaviourModel;

public class CrewmanInteraction : BehaviourModelMechanicComponent<CrewmanInteractionMechanic>
{
    public float interactionDistance = 1;

    public Crewman crewman
    {
        get { return this._crewman.Get(this); }
    }
    private LazyLoadedComponentRef<Crewman> _crewman = new LazyLoadedComponentRef<Crewman>();
    public CrewmanMovementMechanic movement
    {
        get { return this._movement.Get(this); }
    }
    private LazyLoadedComponentRef<CrewmanMovementMechanic> _movement = new LazyLoadedComponentRef<CrewmanMovementMechanic>();

    protected override void BindHandlers()
    {
        this.mechanic.interact.RegisterStartCondition(CanStartInteracting);
        this.mechanic.interact.RegisterStopCondition(CanStopInteracting);
        this.mechanic.interact.onStart += OnStartInteracting;
        this.mechanic.interact.onStop += OnStopInteracting;
        this.mechanic.interact.RegisterActivityGetter(IsInteracting);

        this.mechanic.commandInteract.RegisterStartCondition(CanStartCommandInteraction);
        this.mechanic.commandInteract.RegisterStopCondition(CanStopCommandInteraction);
        this.mechanic.commandInteract.onStart += OnStartCommandInteraction;
        this.mechanic.commandInteract.onStop += OnStopCommandInteraction;
        this.mechanic.commandInteract.RegisterActivityGetter(IsCommandInteractionActive);

        this.movement.move.onStart += OnMoveStart;
        this.movement.move.onStop += OnMoveStop;
    }

    public void FixedUpdate()
    {
        if (!Essentials.UnityIsNull(this.currentInteractable))
            this.mechanic.interactionTick.Fire(this.currentInteractable);
    }

    #region Command Interaction

    private bool justIssuedMovement;
    private IInteractable commandedInteractable;

    private bool IsCommandInteractionActive()
    {
        return !Essentials.UnityIsNull(this.commandedInteractable);
    }

    private void OnMoveStart(MovementParameters p)
    {
        if (!this.justIssuedMovement)
        {
            this.mechanic.interact.ForceStop();
            this.mechanic.commandInteract.TryStop();
            return;
        }

        Debug.Log("Received commanded interaction move command for position " + p.position);
        this.justIssuedMovement = false;
    }

    private void OnMoveStop()
    {
        if (!ReferenceEquals(this.commandedInteractable, null))
        {
            this.mechanic.interact.TryStart(this.commandedInteractable);
            this.commandedInteractable = null;
        }
    }

    private bool CanStartCommandInteraction(IInteractable interactable)
    {
        if (interactable.interact.CanStart(this.crewman))
        {
            // Anyone else on this job yet?
            foreach (var crewman in Game.instance.crewmen)
            {
                if (crewman.GetComponent<CrewmanInteraction>().commandedInteractable == interactable)
                    return false;
            }

            return true;
        }

        return false;
    }

    private bool CanStopCommandInteraction()
    {
        return CanStopInteracting() && !Essentials.UnityIsNull(this.currentInteractable);
    }

    private void OnStartCommandInteraction(IInteractable interactable)
    {
        Debug.Log("Started commanded interacting with " + interactable);

        if (this.mechanic.interact.IsActive())
            this.mechanic.interact.ForceStop();

        if (!this.mechanic.interact.TryStart(interactable))
        {
            Debug.Log("Cannot reach interactable, issued move command!");
            this.commandedInteractable = interactable;
            this.justIssuedMovement = true;
            this.movement.move.ForceStart(new MovementParameters(interactable.interactionPosition, interactable.interactionLookRotation));
        }
    }

    private void OnStopCommandInteraction()
    {
        Debug.Log("Stopped commanded interacting with " + commandedInteractable);
        this.commandedInteractable = null;
        this.justIssuedMovement = false;
        this.mechanic.interact.ForceStop();
    }

    #endregion

    #region Interaction
    private IInteractable currentInteractable;

    private bool CanStartInteracting(IInteractable interactable)
    {
        return ReferenceEquals(this.currentInteractable, null) && interactable.interact.CanStart(this.crewman) &&
            Vector3.Distance(interactable.interactionPosition, this.transform.position) <= this.interactionDistance;
    }

    private bool CanStopInteracting()
    {
        return IsInteracting() && this.currentInteractable.interact.CanStop();
    }

    private void OnStartInteracting(IInteractable interactable)
    {
        Debug.Log("Started interacting with " + interactable);

        if (this.mechanic.commandInteract.IsActive())
            this.mechanic.commandInteract.ForceStop();

        if (this.movement.move.IsActive())
            this.movement.move.ForceStop();

        this.currentInteractable = interactable;
        this.currentInteractable.interact.ForceStart(this.crewman);
    }

    private void OnStopInteracting()
    {
        if (Essentials.UnityIsNull(this.currentInteractable))
            return;

        Debug.Log("Stopped interacting with " + currentInteractable);
        this.currentInteractable.interact.ForceStop();
        this.currentInteractable = null;
    }

    private bool IsInteracting()
    {
        return !Essentials.UnityIsNull(this.currentInteractable);
    }

    #endregion
}