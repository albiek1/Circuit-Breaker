using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractionHandler : MonoBehaviour
{
    InteractionHandler[] allHandlers;
    bool hasFocus;

    public void InitAllHandlers(InteractionHandler[] allHandlers)
    {
        this.allHandlers = allHandlers;
    }

    public abstract void OrderedUpdate();

    protected virtual void FocusLost() { }

    protected virtual bool CanReleaseFocus()
    {
        return true;
    }

    protected virtual void RequestFocus()
    {
        if (!hasFocus)
        {
            bool noHandlersHaveFocus = true;
            foreach(var otherHandler in allHandlers)
            {
                if (otherHandler.hasFocus)
                {
                    noHandlersHaveFocus = false;
                    if (otherHandler.CanReleaseFocus())
                    {
                        otherHandler.hasFocus = false;
                        otherHandler.FocusLost();
                        hasFocus = true;
                        break;
                    }
                }
            }
            if (noHandlersHaveFocus)
            {
                hasFocus = true;
            }
        }
    }

    protected bool HasFocus
    {
        get
        {
            return hasFocus;
        }
    }
}
