using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;

namespace AITSYS.VRCUnity.DiscordRPC
{
    internal static class DiscordRpcNative
    {
        internal delegate void ReadyHandler(ref DiscordUser user);
        internal delegate void DisconnectedHandler(int errorCode, string message);
        internal delegate void ErrorHandler(int errorCode, string message);
        internal delegate void JoinHandler(string secret);
        internal delegate void SpectateHandler(string secret);
        internal delegate void JoinRequestHandler(ref DiscordUser user);

        [StructLayout(LayoutKind.Sequential)]
        internal struct EventHandlers
        {
            internal ReadyHandler ready;
            internal DisconnectedHandler disconnected;
            internal ErrorHandler error;
            internal JoinHandler join;
            internal SpectateHandler spectate;
            internal JoinRequestHandler joinRequest;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DiscordUser
        {
            [MarshalAs(UnmanagedType.LPStr)] internal string userId;
            [MarshalAs(UnmanagedType.LPStr)] internal string username;
            [MarshalAs(UnmanagedType.LPStr)] internal string discriminator;
            [MarshalAs(UnmanagedType.LPStr)] internal string avatar;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RichPresenceData
        {
            internal IntPtr state;
            internal IntPtr details;
            internal long startTimestamp;
            internal long endTimestamp;
            internal IntPtr largeImageKey;
            internal IntPtr largeImageText;
            internal IntPtr smallImageKey;
            internal IntPtr smallImageText;
            internal IntPtr partyId;
            internal int partySize;
            internal int partyMax;
            internal IntPtr matchSecret;
            internal IntPtr joinSecret;
            internal IntPtr spectateSecret;
            [MarshalAs(UnmanagedType.I1)] internal bool instance;
        }

        internal sealed class RichPresence
        {
            internal string state;
            internal string details;
            internal long startTimestamp;
            internal long endTimestamp;
            internal string largeImageKey;
            internal string largeImageText;
            internal string smallImageKey;
            internal string smallImageText;

            private readonly List<IntPtr> allocations = new List<IntPtr>(8);

            internal void Send()
            {
                var data = new RichPresenceData
                {
                    state = ToUtf8(state),
                    details = ToUtf8(details),
                    startTimestamp = startTimestamp,
                    endTimestamp = endTimestamp,
                    largeImageKey = ToUtf8(largeImageKey),
                    largeImageText = ToUtf8(largeImageText),
                    smallImageKey = ToUtf8(smallImageKey),
                    smallImageText = ToUtf8(smallImageText)
                };

                try
                {
                    UpdatePresenceNative(ref data);
                }
                finally
                {
                    FreeAllocations();
                }
            }

            private IntPtr ToUtf8(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return IntPtr.Zero;

                byte[] bytes = Encoding.UTF8.GetBytes(value);
                IntPtr buffer = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                Marshal.WriteByte(buffer, bytes.Length, 0);
                allocations.Add(buffer);
                return buffer;
            }

            private void FreeAllocations()
            {
                for (int i = allocations.Count - 1; i >= 0; i--)
                    Marshal.FreeHGlobal(allocations[i]);

                allocations.Clear();
            }
        }

        private static EventHandlers retainedHandlers;

        [MonoPInvokeCallback(typeof(ReadyHandler))]
        private static void Ready(ref DiscordUser user)
        {
            if (retainedHandlers.ready != null)
                retainedHandlers.ready(ref user);
        }

        [MonoPInvokeCallback(typeof(DisconnectedHandler))]
        private static void Disconnected(int errorCode, string message)
        {
            if (retainedHandlers.disconnected != null)
                retainedHandlers.disconnected(errorCode, message);
        }

        [MonoPInvokeCallback(typeof(ErrorHandler))]
        private static void Error(int errorCode, string message)
        {
            if (retainedHandlers.error != null)
                retainedHandlers.error(errorCode, message);
        }

        [MonoPInvokeCallback(typeof(JoinHandler))]
        private static void Join(string secret)
        {
            if (retainedHandlers.join != null)
                retainedHandlers.join(secret);
        }

        [MonoPInvokeCallback(typeof(SpectateHandler))]
        private static void Spectate(string secret)
        {
            if (retainedHandlers.spectate != null)
                retainedHandlers.spectate(secret);
        }

        [MonoPInvokeCallback(typeof(JoinRequestHandler))]
        private static void JoinRequest(ref DiscordUser user)
        {
            if (retainedHandlers.joinRequest != null)
                retainedHandlers.joinRequest(ref user);
        }

        internal static void Initialize(string applicationId, EventHandlers handlers)
        {
            retainedHandlers = handlers;
            var nativeHandlers = new EventHandlers
            {
                ready = Ready,
                disconnected = Disconnected,
                error = Error,
                join = Join,
                spectate = Spectate,
                joinRequest = JoinRequest
            };

            InitializeNative(applicationId, ref nativeHandlers, false, string.Empty);
        }

        internal static void UpdatePresence(RichPresence presence)
        {
            presence.Send();
        }

        [DllImport("discord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitializeNative(
            string applicationId,
            ref EventHandlers handlers,
            bool autoRegister,
            string optionalSteamId);

        [DllImport("discord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UpdatePresenceNative(ref RichPresenceData presence);

        [DllImport("discord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ClearPresence();

        [DllImport("discord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Shutdown();
    }
}
