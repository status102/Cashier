using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace Cashier.Commons
{
    public unsafe class AgentEventHandlerHook : IDisposable
    {
        public AgentId AgentId { get; }
        private readonly AgentInterface* agentInterface;
        private readonly HookWrapper<AgentEventHandler> hook;
        private Action<EventCall> Action { get; init; }

        public bool Disposed { get; private set; }

        public delegate void* AgentEventHandler(AgentInterface* agentInterface, void* a2, AtkValue* values, ulong atkValueCount, ulong eventType);

        public AgentEventHandlerHook(AgentId agentId, Action<EventCall> action)
        {
            AgentId = agentId;
            agentInterface = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(agentId);
            hook = Hook<AgentEventHandler>(agentInterface->AtkEventInterface.vtbl[0], HandleEvent);
            hook?.Enable();
            Action = action;
        }

        public void* HandleEvent(AgentInterface* agent, void* a2, AtkValue* values, ulong atkValueCount, ulong eventType)
        {
            if (Disposed || agent != agentInterface)
                return hook.Original(agent, a2, values, atkValueCount, eventType);

            try
            {
                var call = new EventCall()
                {
                    EventType = eventType,
                    UnknownPointer = a2,
                    UnknownPointerData = *(ulong*)a2,
                };

                var v = values;
                for (var i = 0UL; i < atkValueCount; i++)
                {
                    call.AtkValues.Add(*v);
                    v++;
                }

                Action(call);

                return hook.Original(agent, a2, values, atkValueCount, eventType);
            }
            catch
            {
                //
            }

            return hook.Original(agent, a2, values, atkValueCount, eventType);
        }

        public class EventCall
        {
            public ulong EventType;
            public List<AtkValue> AtkValues = [];
            public void* UnknownPointer;
            public ulong UnknownPointerData;
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            hook?.Disable();
            hook?.Dispose();
            Disposed = true;
        }

        public static HookWrapper<T> Hook<T>(string signature, T detour, int addressOffset = 0) where T : Delegate
        {
            var addr = Svc.SigScanner.ScanText(signature);
            var h = Svc.GameInteropProvider.HookFromAddress(addr + addressOffset, detour);
            var wh = new HookWrapper<T>(h);
            //HookList.Add(wh);
            return wh;
        }

        public static HookWrapper<T> Hook<T>(void* address, T detour) where T : Delegate
        {
            var h = Svc.GameInteropProvider.HookFromAddress(new nint(address), detour);
            var wh = new HookWrapper<T>(h);
            //HookList.Add(wh);
            return wh;
        }

        public static HookWrapper<T> Hook<T>(nuint address, T detour) where T : Delegate
        {
            var h = Svc.GameInteropProvider.HookFromAddress((nint)address, detour);
            var wh = new HookWrapper<T>(h);
            //HookList.Add(wh);
            return wh;
        }

        public static HookWrapper<T> Hook<T>(nint address, T detour) where T : Delegate
        {
            var h = Svc.GameInteropProvider.HookFromAddress(address, detour);
            var wh = new HookWrapper<T>(h);
            //HookList.Add(wh);
            return wh;
        }
    }

    public interface IHookWrapper : IDisposable
    {
        public void Enable();
        public void Disable();

        public bool IsEnabled { get; }
        public bool IsDisposed { get; }

    }

    public class HookWrapper<T> : IHookWrapper where T : Delegate
    {

        private Hook<T> wrappedHook;

        private bool disposed;

        public HookWrapper(Hook<T> hook)
        {
            wrappedHook = hook;
        }

        public void Enable()
        {
            if (disposed)
                return;
            wrappedHook?.Enable();
        }

        public void Disable()
        {
            if (disposed)
                return;
            wrappedHook?.Disable();
        }

        public void Dispose()
        {
            Disable();
            disposed = true;
            wrappedHook?.Dispose();
        }

        public nint Address => wrappedHook.Address;
        public T Original => wrappedHook.Original;
        public bool IsEnabled => wrappedHook.IsEnabled;
        public bool IsDisposed => wrappedHook.IsDisposed;
    }
}
