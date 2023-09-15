using System;

[AttributeUsage(AttributeTargets.Struct)]
internal class EchoPacketAttribute : Attribute
{
    public int packetID { get; set; }
}