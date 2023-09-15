using System;

[AttributeUsage(AttributeTargets.Struct)]
internal class RecvPacketAttribute : Attribute
{
    public int packetID { get; set; }
}