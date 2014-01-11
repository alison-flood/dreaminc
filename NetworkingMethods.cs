using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsyncSSLServer
{
    static class NetworkingMethods
    {
        public static byte[] StartOfMessage = Encoding.ASCII.GetBytes("<QITSOM>");
        public static byte[] EndOfMessage = Encoding.ASCII.GetBytes("<QITEOF>");
        public static int TypeLength = 1;
        public static int MinimumMessageLength = StartOfMessage.Length + EndOfMessage.Length + TypeLength;

        public enum MessageTypes
        {
            Handshake = 1,
            Login = 2
        }

        public static byte[] SpliceByteArray(byte[] a, byte[] b)
        {
            return SpliceByteArray(a, b, b.Length);
        }

        public static byte[] SpliceByteArray(byte[] a, byte[] b, int numRead)
        {
            byte[] newArray = new byte[a.Length + numRead];
            for (int i = 0; i < a.Length; i++)
                newArray[i] = a[i];
            for (int i = 0; i < numRead; i++)
                newArray[i + a.Length] = b[i];
            return newArray;
        }

        public static byte[] GetPartOfArray(byte[] message, int start, int length)
        {
            if (start + length > message.Length)
                return new byte[0];

            byte[] newMessage = new byte[length];
            for (int i = 0; i < length; i++)
                newMessage[i] = message[i + start];
            return newMessage;
        }

        public static int GetTypeFromMessage(byte[] message)
        {
            try
            {
                return Convert.ToInt32(message[StartOfMessage.Length]);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static int ValidateMessageFormat(byte[] message)
        {
            if (message.Length >= MinimumMessageLength && GetPartOfArray(message, 0, StartOfMessage.Length).SequenceEqual(StartOfMessage) &&
                    GetPartOfArray(message, message.Length - EndOfMessage.Length, EndOfMessage.Length).SequenceEqual(EndOfMessage))
            {
                int messageType = GetTypeFromMessage(message);
                if (IsValidMessageType(messageType))
                    return messageType;
            }

            return -1;
        }

        public static bool IsValidMessageType(int type)
        {
            return Enum.IsDefined(typeof(MessageTypes), type);
        }

        public static byte[] GetContentFromMessage(byte[] message)
        {
            return GetPartOfArray(message, StartOfMessage.Length + TypeLength, message.Length - (StartOfMessage.Length + TypeLength + EndOfMessage.Length));
        }
    }
}
